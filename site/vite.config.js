import { defineConfig, normalizePath } from "vite";
import { existsSync, readdirSync, readFileSync } from "node:fs";
import { extname, relative as relativePath, resolve } from "node:path";
import { pathToFileURL } from "node:url";
import { pageWeights } from "./page-weights.mjs";

const root = import.meta.dirname;

const escapeHtml = (text) =>
  text.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");

const readText = (file) => readFileSync(resolve(root, file), "utf8").replace(/\r\n/g, "\n").trimEnd();

// <pre data-include="Snippets/Counter.fs"></pre> fills in an F# source file at build time, so the
// code shown on a hand-written page is the same code that runs the live demo beside it, and the
// page is complete before any JavaScript loads. (Markdown pages use `::: example` instead.)
function snippetIncludes() {
  const snippet = /<pre data-include="([^"]+)"><\/pre>/g;
  return {
    name: "firelight-snippet-includes",
    transformIndexHtml: {
      order: "pre",
      handler: (html) =>
        html.replace(snippet, (_, file) => `<pre data-include="${file}"><code>${escapeHtml(readText(file))}</code></pre>`),
    },
    configureServer(server) {
      for (const dir of ["Snippets", "Routing"]) server.watcher.add(resolve(root, dir));
      server.watcher.on("change", (file) => {
        // (Renderer/ sources only matter once Fable has compiled them; markdownPages reloads then.)
        if (/\.fs$/.test(file) && !/[\\/]Renderer[\\/]/.test(file)) server.ws.send({ type: "full-reload" });
      });
    },
  };
}

// Markdown pages: content/<path>.md becomes <path>/index.html. The renderer is F# (Renderer/,
// compiled by Fable to build/Renderer/) running here in Node: markdown-it, Shiki and Lit SSR, with
// each `::: example` demo prerendered to Declarative Shadow DOM. Vite then processes the result like
// any hand-written page (module scripts, base path, the includes above).
// Hand-written pages share the generated layout through placeholders such as <!-- firelight:header -->,
// and the plugin also writes sitemap.xml.
// - Dev: pages are rendered on request. The renderer and the demo modules are loaded through Vite's
//   SSR module runner, which re-runs whatever Fable recompiles, so edits need no restart.
// - Build: each page is a virtual .html input that this plugin renders when Rollup loads it.
function markdownPages() {
  const rendererUrl = "/build/Renderer/Prerender.js";
  const fileUrl = (url) => pathToFileURL(resolve(root, url.slice(1))).href;
  let base;
  let server;
  const inputs = new Map(); // build: absolute output path -> content/...md

  const load = (url) => (server ? server.environments.ssr.runner.import(url) : import(fileUrl(url)));
  const host = () => ({ root, base, loadModule: load });
  const render = async (source) => (await load(rendererUrl)).renderPage(host(), source);
  // The sitemap lists the Markdown pages, plus the hand-written pages and the demo apps in public/demos/.
  let handWrittenRoutes = [];
  const demoRoutes = () => {
    const demos = resolve(root, "public", "demos");
    if (!existsSync(demos)) return [];
    return readdirSync(demos).filter((name) => existsSync(resolve(demos, name, "index.html"))).map((name) => `demos/${name}/`);
  };
  const sitemap = async () => (await load(rendererUrl)).sitemap(host(), [...handWrittenRoutes, ...demoRoutes()]);

  const reload = () => server.ws.send({ type: "full-reload" });
  let timer;
  const reloadSoon = () => {
    clearTimeout(timer);
    timer = setTimeout(reload, 150); // one reload per save, though Fable writes several files and Windows reports twice
  };

  return {
    name: "firelight-markdown-pages",
    enforce: "pre",
    async config(userConfig, { command }) {
      handWrittenRoutes = Object.values(userConfig.build?.rollupOptions?.input ?? {}).map((file) =>
        normalizePath(relativePath(root, file)).replace(/index\.html$/, ""),
      );
      // Vite's dependency scan only reads HTML files on disk, so it can't see the rendered pages'
      // imports (hydration support, then each demo module). Without scanning them up front, each is
      // found on first visit, and Vite re-optimizes and reloads, briefly loading two copies of Lit.
      if (command !== "build")
        return {
          optimizeDeps: {
            entries: ["index.html", "build/Snippets/**/*.js", "build/Routing/**/*.js", "build/Components/**/*.js"],
            include: ["@lit-labs/ssr-client/lit-element-hydrate-support.js"],
          },
        };
      const { pages } = await import(fileUrl(rendererUrl)).catch((e) => {
        throw new Error(`Can't load the page renderer; run "npm run build:renderer" first. (${e.message})`);
      });
      const input = {};
      const handWritten = userConfig.build?.rollupOptions?.input ?? {};
      for (const page of pages(root)) {
        const output = normalizePath(resolve(root, page.output));
        if (existsSync(output)) throw new Error(`${page.source} and ${page.output} both define the same page.`);
        // Entry names become asset file names: packages/firelight -> assets/packages-firelight-<hash>.js
        const name = page.output.replace(/\/?(index)?\.html$/, "").replaceAll("/", "-") || "index";
        if (name in input || name in handWritten) throw new Error(`${page.source} has the same entry name as another page ("${name}").`);
        inputs.set(output, page.source);
        input[name] = output;
      }
      return { build: { rollupOptions: { input } } };
    },
    configResolved: (config) => {
      base = config.base;
    },
    resolveId: (id) => (inputs.has(id) ? id : undefined),
    load: (id) => (inputs.has(id) ? render(inputs.get(id)) : undefined),
    transformIndexHtml: {
      order: "pre",
      handler: async (html, { path }) =>
        html.includes("<!-- firelight:") ? (await load(rendererUrl)).renderIncludes(host(), path.slice(1), html) : html,
    },
    async generateBundle() {
      this.emitFile({ type: "asset", fileName: "sitemap.xml", source: await sitemap() });
    },
    configureServer(devServer) {
      server = devServer;
      server.watcher.add(resolve(root, "content"));
      server.watcher.on("all", (_, file) => {
        if (/[\\/]content[\\/].*\.md$|[\\/]build[\\/]Renderer[\\/].*\.js$/.test(file)) reloadSoon();
      });
      const send = async (res, page, req, status = 200) => {
        const html = await server.transformIndexHtml("/" + page.output, await render(page.source), req.originalUrl);
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
            await send(res, notFound, req, 404);
          } catch (e) {
            next(e);
          }
        });
    },
    configurePreviewServer(previewServer) {
      const notFound = resolve(previewServer.config.root, previewServer.config.build.outDir, "404.html");
      const outDir = resolve(previewServer.config.root, previewServer.config.build.outDir);
      return () =>
        previewServer.middlewares.use((req, res, next) => {
          if (!wantsMissingPage(req, outDir) || !existsSync(notFound)) return next();
          res.statusCode = 404;
          res.setHeader("Content-Type", "text/html");
          res.end(readFileSync(notFound));
        });
    },
  };
}

// A page request (GET, accepting HTML) that Vite's middlewares didn't resolve to an .html file in
// `dir`. Vite has already taken the base path off `req.url` at this point.
function wantsMissingPage(req, dir) {
  if (req.method !== "GET" && req.method !== "HEAD") return false;
  if (!(req.headers.accept ?? "").includes("text/html")) return false;
  let path;
  try {
    path = decodeURIComponent(req.url.split("?")[0]);
  } catch {
    return true;
  }
  return !(path.endsWith(".html") && existsSync(resolve(dir, "." + path)));
}

// Writes the sizes measured by build-demos.mjs into <span data-demo-size="todo"></span>,
// so the numbers on the page always match what ships. (The demos themselves are in public/demos/.)
function demoSizes() {
  const sizesFile = resolve(root, "build/demo-sizes.json");
  const kB = (bytes) => `${(bytes / 1000).toFixed(1)} kB`;
  const pattern = /<span data-demo-size="([^"]+)">[^<]*<\/span>/g;
  let isBuild;
  return {
    name: "firelight-demo-sizes",
    configResolved: (config) => {
      isBuild = config.command === "build";
    },
    transformIndexHtml: (html) => {
      const sizes = existsSync(sizesFile) ? JSON.parse(readFileSync(sizesFile, "utf8")) : {};
      return html.replace(pattern, (placeholder, name) => {
        const size = sizes[name];
        if (size) return `<span data-demo-size="${name}">${kB(size.jsGzip)}</span>`;
        if (isBuild) throw new Error(`No size measured for demo "${name}". Run "npm run build:demos" first.`);
        return placeholder;
      });
    },
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
    snippetIncludes(),
    demoSizes(),
    pageWeights({ reportFile: resolve(root, "build/page-weights.json") }),
  ],
  build: {
    rollupOptions: {
      input: {
        home: resolve(root, "index.html"),
      },
    },
  },
  server: {
    host: false,
  },
});
