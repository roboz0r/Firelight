import { defineConfig } from "vite";
import { copyFileSync, existsSync, readdirSync, readFileSync } from "node:fs";
import { extname, resolve } from "node:path";

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
        if (/\.fs$|[\\/]partials[\\/]/.test(file)) server.ws.send({ type: "full-reload" });
      });
    },
  };
}

// One page per package: packages/<name>/index.html.
const packagePages = Object.fromEntries(
  readdirSync(resolve(root, "packages")).map((name) => [`package-${name}`, resolve(root, "packages", name, "index.html")]),
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
  plugins: [includes(), routingPageAs404(), demoSizes(), directoryUrls()],
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
