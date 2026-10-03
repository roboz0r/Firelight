import { defineConfig, normalizePath } from "vite";
import { copyFileSync, existsSync, readdirSync, readFileSync } from "node:fs";
import { extname, resolve } from "node:path";
import { pathToFileURL } from "node:url";

const root = import.meta.dirname;

const escapeHtml = (text) =>
  text.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");

const readText = (file) => readFileSync(resolve(root, file), "utf8").replace(/\r\n/g, "\n").trimEnd();

// Build-time includes, so every page is complete static HTML before any JavaScript loads:
// - <!-- include partials/header.html --> inserts a shared fragment (header, footer, package list).
// - <pre data-include="Snippets/Counter.fs"></pre> fills in an F# source file, so the code shown
//   is the same code that runs the live demo beside it.
function includes() {
  const partial = /<!-- include (\S+) -->/g;
  const snippet = /<pre data-include="([^"]+)"><\/pre>/g;
  let base;
  return {
    name: "firelight-includes",
    configResolved: (config) => {
      base = config.base;
    },
    transformIndexHtml: {
      order: "pre",
      handler: (html) =>
        html
          .replace(partial, (_, file) => readText(file).replaceAll("%BASE_URL%", base))
          .replace(snippet, (_, file) => `<pre data-include="${file}"><code>${escapeHtml(readText(file))}</code></pre>`),
    },
    configureServer(server) {
      for (const dir of ["Snippets", "Routing", "partials"]) server.watcher.add(resolve(root, dir));
      server.watcher.on("change", (file) => {
        // (Renderer/ sources only matter once Fable has compiled them; markdownPages reloads then.)
        if (/\.fs$|[\\/]partials[\\/]/.test(file) && !/[\\/]Renderer[\\/]/.test(file)) server.ws.send({ type: "full-reload" });
      });
    },
  };
}

// Markdown pages: content/<path>.md becomes <path>/index.html. The renderer is F# (Renderer/,
// compiled by Fable to build/Renderer/) running here in Node: markdown-it, Shiki and Lit SSR, with
// each `::: example` demo prerendered to Declarative Shadow DOM. Vite then processes the result like
// any hand-written page (module scripts, base path, the includes above).
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
  const render = async (source) => (await load(rendererUrl)).renderPage({ root, base, loadModule: load }, source);

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
      // Vite's dependency scan only reads HTML files on disk, so it can't see this import in the
      // rendered pages; without this it's found on the first visit and the page reloads.
      if (command !== "build") return { optimizeDeps: { include: ["@lit-labs/ssr-client/lit-element-hydrate-support.js"] } };
      const { pages } = await import(fileUrl(rendererUrl)).catch((e) => {
        throw new Error(`Can't load the page renderer; run "npm run build:renderer" first. (${e.message})`);
      });
      const input = {};
      const handWritten = userConfig.build?.rollupOptions?.input ?? {};
      for (const page of pages(root)) {
        const output = normalizePath(resolve(root, page.output));
        if (existsSync(output)) throw new Error(`${page.source} and ${page.output} both define the same page.`);
        // Entry names become asset file names: packages/firelight -> assets/packages-firelight-<hash>.js
        const name = page.output.replace(/\/?index\.html$/, "").replaceAll("/", "-") || "index";
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
    configureServer(devServer) {
      server = devServer;
      server.watcher.add(resolve(root, "content"));
      server.watcher.on("all", (_, file) => {
        if (/[\\/]content[\\/].*\.md$|[\\/]build[\\/]Renderer[\\/].*\.js$/.test(file)) reloadSoon();
      });
      server.middlewares.use(async (req, res, next) => {
        const [path, query] = req.url.split("?");
        if (!path.startsWith(base) || extname(path) && !path.endsWith(".html")) return next();
        try {
          const relative = path.slice(base.length);
          const { pages } = await load(rendererUrl);
          const page = pages(root).find((p) => [p.output, p.output.replace(/index\.html$/, "")].includes(relative));
          if (!page) {
            const withSlash = pages(root).find((p) => p.output === relative + "/index.html");
            if (!withSlash) return next();
            res.writeHead(301, { Location: path + "/" + (query ? "?" + query : "") });
            return res.end();
          }
          const html = await server.transformIndexHtml("/" + page.output, await render(page.source), req.originalUrl);
          res.setHeader("Content-Type", "text/html");
          res.end(html);
        } catch (e) {
          next(e);
        }
      });
    },
  };
}

// One page per package: packages/<name>/index.html. (Packages written in Markdown are added by markdownPages.)
const packagePages = Object.fromEntries(
  readdirSync(resolve(root, "packages"))
    .filter((name) => existsSync(resolve(root, "packages", name, "index.html")))
    .map((name) => [`package-${name}`, resolve(root, "packages", name, "index.html")]),
);

// GitHub Pages serves 404.html for any unknown path. Making it the routing page lets deep links
// like /Firelight/client-side-routing/users/42 load, and unknown URLs render the router's NotFound.
function routingPageAs404() {
  let outDir;
  return {
    name: "firelight-routing-404",
    apply: "build",
    configResolved: (config) => {
      outDir = resolve(config.root, config.build.outDir);
    },
    writeBundle: () =>
      copyFileSync(resolve(outDir, "client-side-routing/index.html"), resolve(outDir, "404.html")),
  };
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
// - Deep links into the client-side routing demo serve the routing page, like 404.html on GitHub Pages.
function directoryUrls() {
  const base = "/Firelight";
  const publicDir = resolve(root, "public");
  const dirs = [root, publicDir, resolve(root, "dist")];
  const relative = (path) => path.slice(base.length + 1);
  const routingPage = base + "/client-side-routing/";
  const handle = (req, res, next) => {
    const [path, query] = req.url.split("?");
    if (path !== base && !path.startsWith(base + "/")) return next();

    if (path === base || (!path.endsWith("/") && !extname(path) && dirs.some((d) => existsSync(resolve(d, relative(path), "index.html"))))) {
      res.writeHead(301, { Location: path + "/" + (query ? "?" + query : "") });
      return res.end();
    }
    if (path.endsWith("/") && path !== base + "/" && existsSync(resolve(publicDir, relative(path), "index.html")))
      req.url = path + "index.html" + (query ? "?" + query : "");
    // Deep links into the client-side routing demo get the routing page, as 404.html does on GitHub Pages.
    else if (path.startsWith(routingPage) && path !== routingPage && !extname(path))
      req.url = routingPage + "index.html" + (query ? "?" + query : "");
    next();
  };
  return {
    name: "firelight-directory-urls",
    configureServer: (server) => void server.middlewares.use(handle),
    configurePreviewServer: (server) => void server.middlewares.use(handle),
  };
}

export default defineConfig({
  base: "/Firelight/",
  // Unknown URLs get a 404, as on GitHub Pages, rather than index.html (Vite's SPA default), so
  // Site.E2E sees missing files.
  appType: "mpa",
  plugins: [markdownPages(), includes(), routingPageAs404(), demoSizes(), directoryUrls()],
  build: {
    rollupOptions: {
      input: {
        home: resolve(root, "index.html"),
        routing: resolve(root, "client-side-routing/index.html"),
        ...packagePages,
      },
    },
  },
  server: {
    host: false,
  },
});
