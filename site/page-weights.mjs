// Reports the JavaScript each page loads: its scripts and every chunk they import, statically or
// dynamically, gzipped (the same measure as build-demos.mjs). Writes build/page-weights.json,
// keyed by the page's path in dist/, and prints a table. Site.E2E checks the report against what
// the browser actually loads. The demo apps in dist/demos/ come from their own builds, so they are
// left out here; build-demos.mjs measures them and holds their budgets.
import { mkdirSync, readdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join, posix, resolve } from "node:path";
import { gzipSync } from "node:zlib";

export function pageWeights({ reportFile }) {
  let outDir;
  let base;
  const graph = new Map(); // chunk file name -> the chunks it imports

  // An attribute's value, quoted either way or not at all.
  const attribute = (attributes, name) => {
    const match = new RegExp(`(?:^|\\s)${name}\\s*=\\s*(?:"([^"]*)"|'([^']*)'|([^\\s"'>]+))`, "i").exec(attributes);
    return match && (match[1] ?? match[2] ?? match[3]);
  };

  // <script src="..."> and inline scripts, leaving out comments and scripts that aren't code
  // (JSON, import maps). The pages are Vite's output, so a full HTML parser isn't needed.
  const scripts = (html) => {
    const found = { files: [], inline: [] };
    const withoutComments = html.replace(/<!--[\s\S]*?-->/g, "");
    for (const [, attributes, body] of withoutComments.matchAll(/<script\b([^>]*)>([\s\S]*?)<\/script\s*>/gi)) {
      const type = (attribute(attributes, "type") ?? "").trim().toLowerCase();
      if (type && type !== "module" && !type.includes("javascript")) continue;
      const src = attribute(attributes, "src");
      if (src) found.files.push(src);
      else if (body.trim()) found.inline.push(body);
    }
    return found;
  };

  const weigh = (page, html) => {
    const { files, inline } = scripts(html);
    const pageDir = posix.dirname(page);
    const seen = new Set();
    const visit = (file) => {
      if (seen.has(file)) return;
      seen.add(file);
      for (const imported of graph.get(file) ?? []) visit(imported);
    };
    for (const src of files) {
      if (/^[a-z]+:\/\//i.test(src)) throw new Error(`${page} loads ${src} from another site; page weights can't measure it.`);
      visit(src.startsWith(base) ? src.slice(base.length) : posix.normalize(posix.join(pageDir, src)));
    }
    const chunks = [...seen].sort();
    const measure = (contents) => ({
      js: contents.reduce((n, c) => n + c.length, 0),
      jsGzip: contents.reduce((n, c) => n + gzipSync(c).length, 0),
    });
    const fromFiles = measure(chunks.map((f) => readFileSync(join(outDir, f))));
    const fromInline = measure(inline.map((s) => Buffer.from(s)));
    return {
      js: fromFiles.js + fromInline.js,
      jsGzip: fromFiles.jsGzip + fromInline.jsGzip,
      files: chunks,
      inline: { scripts: inline.length, ...fromInline },
    };
  };

  return {
    name: "firelight-page-weights",
    apply: "build",
    configResolved: (config) => {
      outDir = resolve(config.root, config.build.outDir);
      base = config.base;
    },
    writeBundle: (_, bundle) => {
      for (const chunk of Object.values(bundle))
        if (chunk.type === "chunk") graph.set(chunk.fileName, [...chunk.imports, ...chunk.dynamicImports]);
    },
    // After writeBundle, so pages that other plugins copy there (404.html) are included.
    closeBundle: {
      order: "post",
      handler: () => {
        const pages = readdirSync(outDir, { recursive: true })
          .map((f) => f.replaceAll("\\", "/"))
          .filter((f) => f.endsWith(".html") && !f.startsWith("demos/"))
          .sort();
        const report = Object.fromEntries(pages.map((page) => [page, weigh(page, readFileSync(join(outDir, page), "utf8"))]));
        mkdirSync(dirname(reportFile), { recursive: true });
        writeFileSync(reportFile, JSON.stringify(report, null, 2));

        const kB = (bytes) => `${(bytes / 1000).toFixed(1)} kB`;
        console.log("\nJavaScript per page (gzipped, with everything it imports):");
        console.table(Object.fromEntries(pages.map((p) => [p, { js: kB(report[p].jsGzip), chunks: report[p].files.length }])));
      },
    },
  };
}
