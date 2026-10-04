import { createServer, defineConfig, normalizePath } from "vite";
import { existsSync, readdirSync, readFileSync, rmSync, statSync, utimesSync } from "node:fs";
import { extname, relative as relativePath, resolve } from "node:path";
import { pathToFileURL } from "node:url";
import { pageWeights } from "./page-weights.mjs";

const root = import.meta.dirname;

// Markdown pages: content/<path>.md becomes <path>/index.html. The renderer is F# (Renderer/,
// compiled by Fable to build/Renderer/) running here in Node: markdown-it, Shiki and Lit SSR, with
// each `::: example` demo prerendered to Declarative Shadow DOM. Vite then processes the result like
// any HTML page (module scripts, base path). The plugin also writes sitemap.xml, and the site as
// Markdown for agents (Renderer/Agents.fs).
// - Dev: pages are rendered on request. The renderer and the demo modules are loaded through Vite's
//   SSR module runner, which re-runs whatever Fable recompiles, so edits need no restart.
// - Build: each page is a virtual .html input that this plugin renders when Rollup loads it. The
//   renderer and the demo modules load through the SSR module runner of a Vite server in middleware
//   mode, started for the build, so they load as in dev: a demo may import what Vite understands
//   and Node doesn't, such as CSS with `?inline`.
function markdownPages() {
  const rendererUrl = "/build/Renderer/Prerender.js";
  const fileUrl = (url) => pathToFileURL(resolve(root, url.slice(1))).href;
  let base;
  let mode;
  let server;
  let buildServer; // build: the Vite server whose SSR runner loads modules
  const inputs = new Map(); // build: absolute output path -> content/...md

  // In dev, a module that isn't there yet (a new snippet Fable hasn't compiled) is reported without
  // asking Vite for it: Vite would remember the failed lookup and keep failing after the file
  // appears. The page reloads once it does (see configureServer).
  const load = (url) => {
    if (buildServer) return buildServer.environments.ssr.runner.import(url);
    if (!server) return import(fileUrl(url));
    if (!existsSync(resolve(root, url.slice(1))))
      return Promise.reject(new Error(`${url} doesn't exist yet. If Fable is still compiling it, the page reloads when it's done.`));
    return server.environments.ssr.runner.import(url);
  };
  // Only Vite's own transforms are needed to load modules, not this config's plugins, so the server
  // has no config file. It doesn't listen, watch or pre-bundle anything.
  const startBuildServer = async () => {
    buildServer = await createServer({
      root,
      base,
      mode,
      configFile: false,
      logLevel: "warn",
      appType: "custom",
      server: { middlewareMode: true, hmr: false, ws: false, watch: null, preTransformRequests: false },
      optimizeDeps: { noDiscovery: true, include: [] },
    });
  };
  const stopBuildServer = async () => {
    const stopping = buildServer;
    buildServer = undefined;
    await stopping?.close();
  };
  const host = () => ({ root, base, loadModule: load });
  const render = async (source) => (await load(rendererUrl)).renderPage(host(), source);
  // The sitemap lists the Markdown pages, plus the demo apps in public/demos/.
  const demoRoutes = () => {
    const demos = resolve(root, "public", "demos");
    if (!existsSync(demos)) return [];
    return readdirSync(demos).filter((name) => existsSync(resolve(demos, name, "index.html"))).map((name) => `demos/${name}/`);
  };
  const sitemap = async () => (await load(rendererUrl)).sitemap(host(), demoRoutes());
  const agentFiles = async () => (await load(rendererUrl)).agentFiles(host());

  const reload = () => server.ws.send({ type: "full-reload" });
  let timer;
  const reloadSoon = () => {
    clearTimeout(timer);
    timer = setTimeout(reload, 150); // one reload per save, though Fable writes several files and Windows reports twice
  };

  return {
    name: "firelight-markdown-pages",
    enforce: "pre",
    async config(_, { command }) {
      // Vite's dependency scan only reads HTML files on disk, so it can't see the rendered pages'
      // imports (hydration support, then each demo module). Without scanning them up front, each is
      // found on first visit, and Vite re-optimizes and reloads, briefly loading two copies of Lit.
      if (command !== "build")
        return {
          optimizeDeps: {
            entries: ["build/Snippets/**/*.js", "build/Routing/**/*.js", "build/Components/**/*.js"],
            include: ["@lit-labs/ssr-client/lit-element-hydrate-support.js"],
          },
        };
      const { pages } = await import(fileUrl(rendererUrl)).catch((e) => {
        throw new Error(`Can't load the page renderer; run "npm run build:renderer" first. (${e.message})`);
      });
      const input = {};
      for (const page of pages(root)) {
        const output = normalizePath(resolve(root, page.output));
        if (existsSync(output)) throw new Error(`${page.source} and ${page.output} both define the same page.`);
        // Entry names become asset file names: packages/firelight -> assets/packages-firelight-<hash>.js
        const name = page.output.replace(/\/?(index)?\.html$/, "").replaceAll("/", "-") || "index";
        if (name in input) throw new Error(`${page.source} has the same entry name as another page ("${name}").`);
        inputs.set(output, page.source);
        input[name] = output;
      }
      return { build: { rollupOptions: { input } } };
    },
    configResolved: (config) => {
      base = config.base;
      mode = config.mode;
    },
    // (buildStart also runs in dev, where the dev server itself loads the modules.)
    async buildStart() {
      if (inputs.size > 0) await startBuildServer();
    },
    async buildEnd(error) {
      if (error) await stopBuildServer();
    },
    closeBundle: stopBuildServer,
    resolveId: (id) => (inputs.has(id) ? id : undefined),
    load: (id) => (inputs.has(id) ? render(inputs.get(id)) : undefined),
    async generateBundle() {
      this.emitFile({ type: "asset", fileName: "sitemap.xml", source: await sitemap() });
      // The site as Markdown, for agents: <page>/index.md, llms.txt and llms-full.txt.
      for (const { file, text } of await agentFiles())
        this.emitFile({ type: "asset", fileName: file, source: fillDemoSizes(text, { strict: true, plain: true }) });
    },
    // The internal link check, over every page as built: any link to a page, #anchor or file that
    // doesn't exist fails the build, with the file and line it came from.
    async writeBundle(options, bundle) {
      const renderer = await load(rendererUrl);
      const sources = new Map(renderer.pages(root).map((p) => [p.output, p.source]));
      const built = Object.values(bundle)
        .filter((file) => file.type === "asset" && file.fileName.endsWith(".html"))
        .map((file) => ({ output: file.fileName, source: sources.get(file.fileName) ?? file.fileName, html: String(file.source) }));
      const { errors, warnings } = renderer.checkLinks(host(), true, ["public", options.dir], built);
      for (const warning of warnings) this.warn(warning);
      if (errors.length) this.error(`Broken internal links:\n  ${errors.join("\n  ")}`);
    },
    configureServer(devServer) {
      server = devServer;
      // Directory.Build.props: the version in the footer and the NuGet links.
      const props = resolve(root, "../Directory.Build.props");
      for (const path of ["content", "Routing", props]) server.watcher.add(resolve(root, path));
      server.watcher.on("all", (event, file) => {
        if (/[\\/]content[\\/].*\.md$|[\\/]build[\\/]Renderer[\\/].*\.js$/.test(file) || resolve(file) === props) reloadSoon();
        // An edited snippet's code shows at once, its demo once Fable has compiled it. (Renderer/
        // sources only matter once Fable has compiled them.)
        else if (event === "change" && /\.fs$/.test(file) && !/[\\/]Renderer[\\/]/.test(file)) reloadSoon();
        // A new snippet's module, which a page may be waiting for.
        else if (event === "add" && /[\\/]build[\\/]Snippets[\\/].*\.js$/.test(file)) reloadSoon();
      });
      watchSnippetGlob(server);
      // In dev, the link check only warns, and covers the page being served: other pages are
      // targets, but their anchors aren't known until they're rendered.
      const checkLinks = async (page, html) => {
        const renderer = await load(rendererUrl);
        const targets = renderer.pages(root).map((p) => ({ ...p, html: null }));
        const built = targets.map((t) => (t.output === page.output ? { ...t, html } : t));
        const { errors, warnings } = renderer.checkLinks(host(), false, ["public"], built);
        for (const problem of [...errors, ...warnings]) server.config.logger.warn(`[links] ${problem}`, { timestamp: true });
      };
      const send = async (res, page, req, status = 200) => {
        const html = await server.transformIndexHtml("/" + page.output, await render(page.source), req.originalUrl);
        await checkLinks(page, html);
        res.statusCode = status;
        res.setHeader("Content-Type", "text/html");
        res.end(html);
      };
      server.middlewares.use(async (req, res, next) => {
        const [path, query] = req.url.split("?");
        try {
          if (path === base + "sitemap.xml") {
            const xml = await sitemap();
            res.setHeader("Content-Type", "application/xml");
            return res.end(xml);
          }
          if (path.startsWith(base) && /(\/index\.md|^\/llms(-full)?\.txt)$/.test("/" + path.slice(base.length))) {
            const found = (await agentFiles()).find((f) => f.file === path.slice(base.length));
            if (!found) return next();
            res.setHeader("Content-Type", `${path.endsWith(".md") ? "text/markdown" : "text/plain"}; charset=utf-8`);
            return res.end(fillDemoSizes(found.text, { strict: false, plain: true }));
          }
          if (!path.startsWith(base) || extname(path) && !path.endsWith(".html")) return next();
          const relative = path.slice(base.length);
          const { pages } = await load(rendererUrl);
          const page = pages(root).find((p) => [p.output, p.output.replace(/index\.html$/, "")].includes(relative));
          if (!page) {
            const withSlash = pages(root).find((p) => p.output === relative + "/index.html");
            if (!withSlash) return next();
            res.writeHead(301, { Location: path + "/" + (query ? "?" + query : "") });
            return res.end();
          }
          await send(res, page, req);
        } catch (e) {
          next(e);
        }
      });
      // Anything nothing else served gets 404.html, as on GitHub Pages: "page not found", or a
      // single-page app's page for addresses under its route. (Runs after Vite's own middlewares.)
      return () =>
        server.middlewares.use(async (req, res, next) => {
          if (!wantsMissingPage(req, root)) return next();
          try {
            const { pages } = await load(rendererUrl);
            const notFound = pages(root).find((p) => p.output === "404.html");
            if (!notFound) return next();
            await send(res, notFound, req, missingPageStatus(req, pages(root)));
          } catch (e) {
            next(e);
          }
        });
    },
    configurePreviewServer(previewServer) {
      const outDir = resolve(previewServer.config.root, previewServer.config.build.outDir);
      const notFound = resolve(outDir, "404.html");
      return () =>
        previewServer.middlewares.use(async (req, res, next) => {
          if (!wantsMissingPage(req, outDir) || !existsSync(notFound)) return next();
          const { pages } = await import(fileUrl(rendererUrl));
          res.statusCode = missingPageStatus(req, pages(root));
          res.setHeader("Content-Type", "text/html");
          res.end(readFileSync(notFound));
        });
    },
  };
}

// Dev: `fable watch` reads Site.fsproj's Snippets/**/*.fs glob only when the project file changes,
// so a snippet added or deleted while it runs would go unnoticed. Touching the project makes it
// read the glob again, but a touch while it's already reading the project (10-20 s) is lost. So the
// project is touched when a snippet is added or deleted, then again every 20 s until Fable has
// caught up: until a new snippet's module exists and is newer than its source, and for a deletion,
// until a touch 20 s after it (when any reading under way at the time has finished).
// A deleted snippet's module is removed, so nothing loads the stale copy, and so is any module
// Fable writes later for a snippet that no longer exists (from a compilation under way).
function watchSnippetGlob(server) {
  const snippets = normalizePath(resolve(root, "Snippets")) + "/";
  const modules = normalizePath(resolve(root, "build", "Snippets")) + "/";
  const moduleOf = (file) => resolve(root, "build", relativePath(root, file).replace(/\.fs$/, ".js"));
  const sourceOf = (js) => resolve(root, relativePath(resolve(root, "build"), js).replace(/\.js$/, ".fs"));
  const retryMs = 20000;
  const pending = new Map(); // snippet file -> { event: "add" | "unlink", at }
  let lastTouch = 0;
  let timer;

  const removeModule = (js) => {
    // Forgotten by the SSR module runner first, or Vite's reload after the deletion re-imports it.
    const evaluated = server.environments.ssr.runner.evaluatedModules;
    for (const mod of evaluated.getModulesByFile(normalizePath(js)) ?? []) {
      evaluated.idToModuleMap.delete(mod.id);
      evaluated.urlToIdModuleMap.delete(mod.url);
      evaluated.fileToModulesMap.delete(mod.file);
    }
    for (const file of [js, js + ".map"]) rmSync(file, { force: true });
  };
  const caughtUp = (file, { at }) => {
    if (!existsSync(file)) return lastTouch - at >= retryMs;
    const js = moduleOf(file);
    return existsSync(js) && statSync(js).mtimeMs >= statSync(file).mtimeMs;
  };
  const touch = () => {
    lastTouch = Date.now();
    utimesSync(resolve(root, "Site.fsproj"), new Date(lastTouch), new Date(lastTouch));
  };
  const check = () => {
    clearTimeout(timer);
    if (Date.now() - lastTouch >= retryMs) touch();
    for (const [file, change] of pending) if (caughtUp(file, change)) pending.delete(file);
    if (pending.size > 0) timer = setTimeout(check, 2000);
  };

  server.watcher.add(snippets);
  server.watcher.on("all", (event, file) => {
    const path = normalizePath(file);
    const isModule = (event === "add" || event === "change") && path.startsWith(modules) && path.endsWith(".js");
    if (isModule && !existsSync(sourceOf(file))) return removeModule(file);
    if (!(event === "add" || event === "unlink") || !path.startsWith(snippets) || !path.endsWith(".fs")) return;
    if (event === "unlink") removeModule(moduleOf(file));
    pending.set(file, { event, at: Date.now() });
    clearTimeout(timer);
    // One touch for a batch of files, then the checks.
    timer = setTimeout(() => {
      touch();
      check();
    }, 150);
  });
}

// GitHub Pages answers every address it has no file for with 404.html and status 404, including a
// single-page app's deep links (which 404.html then shows). Locally those deep links get 200, so
// the browser checks (Site.E2E) can take any 404 as a broken link. `req.url` is without the base.
function missingPageStatus(req, pages) {
  const path = req.url.split("?")[0];
  return pages.some((p) => p.spa && path.startsWith("/" + p.route)) ? 200 : 404;
}

// A request for a page (GET, an address with no extension or .html, and an Accept header that
// allows HTML, as Vite's own HTML fallback decides) that Vite's middlewares didn't resolve to an
// .html file in `dir`. Vite has already taken the base path off `req.url` at this point.
function wantsMissingPage(req, dir) {
  if (req.method !== "GET" && req.method !== "HEAD") return false;
  const accept = req.headers.accept ?? "";
  if (accept && !accept.includes("text/html") && !accept.includes("*/*")) return false;
  let path;
  try {
    path = decodeURIComponent(req.url.split("?")[0]);
  } catch {
    return true;
  }
  if (!accept.includes("text/html") && extname(path) && !path.endsWith(".html")) return false;
  return !(path.endsWith(".html") && existsSync(resolve(dir, "." + path)));
}

// Writes the sizes measured by build-demos.mjs into <span data-demo-size="todo"></span>,
// so the numbers on the page always match what ships. (The demos themselves are in public/demos/.)
// In the Markdown for agents (`plain`), the span becomes just the size.
function fillDemoSizes(text, { strict, plain = false }) {
  const sizesFile = resolve(root, "build/demo-sizes.json");
  const kB = (bytes) => `${(bytes / 1000).toFixed(1)} kB`;
  const sizes = existsSync(sizesFile) ? JSON.parse(readFileSync(sizesFile, "utf8")) : {};
  return text.replace(/<span data-demo-size="([^"]+)">[^<]*<\/span>/g, (placeholder, name) => {
    const size = sizes[name];
    if (size) return plain ? kB(size.jsGzip) : `<span data-demo-size="${name}">${kB(size.jsGzip)}</span>`;
    if (strict) throw new Error(`No size measured for demo "${name}". Run "npm run build:demos" first.`);
    return placeholder;
  });
}

function demoSizes() {
  let isBuild;
  return {
    name: "firelight-demo-sizes",
    configResolved: (config) => {
      isBuild = config.command === "build";
    },
    transformIndexHtml: (html) => fillDemoSizes(html, { strict: isBuild }),
  };
}

// Make local directory URLs behave like GitHub Pages:
// - /Firelight and /Firelight/demos/todo redirect to the trailing-slash form.
// - In dev, /Firelight/demos/todo/ serves public/demos/todo/index.html. (Vite's dev server only
//   maps directory URLs to index.html for pages in the project root, not for files in public/.)
function directoryUrls() {
  const base = "/Firelight";
  const publicDir = resolve(root, "public");
  const dirs = [root, publicDir, resolve(root, "dist")];
  const relative = (path) => path.slice(base.length + 1);
  const handle = (req, res, next) => {
    const [path, query] = req.url.split("?");
    if (path !== base && !path.startsWith(base + "/")) return next();

    if (path === base || (!path.endsWith("/") && !extname(path) && dirs.some((d) => existsSync(resolve(d, relative(path), "index.html"))))) {
      res.writeHead(301, { Location: path + "/" + (query ? "?" + query : "") });
      return res.end();
    }
    if (path.endsWith("/") && path !== base + "/" && existsSync(resolve(publicDir, relative(path), "index.html")))
      req.url = path + "index.html" + (query ? "?" + query : "");
    next();
  };
  return {
    name: "firelight-directory-urls",
    enforce: "pre",
    configureServer: (server) => void server.middlewares.use(handle),
    configurePreviewServer: (server) => void server.middlewares.use(handle),
  };
}

export default defineConfig({
  base: "/Firelight/",
  // Unknown addresses get 404.html with a 404 status, as on GitHub Pages (see markdownPages), rather
  // than index.html (Vite's SPA default), so Site.E2E sees missing files.
  appType: "mpa",
  plugins: [
    directoryUrls(),
    markdownPages(),
    demoSizes(),
    pageWeights({ reportFile: resolve(root, "build/page-weights.json") }),
  ],
  server: {
    host: false,
  },
});
