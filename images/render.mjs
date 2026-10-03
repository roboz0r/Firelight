// Renders the PNG brand assets from the SVGs in this folder. Run it after editing a logo:
//
//   npx -y -p playwright@1.58.0 node images/render.mjs
//
// from the repository root (or anywhere: paths are relative to this file). Playwright isn't a
// dependency of the repo; npx fetches it for this one run. 1.58.0 is the version Site.E2E uses
// (Microsoft.Playwright in Directory.Packages.props), so its Chromium is usually downloaded already;
// if not, run `npx -y playwright@1.58.0 install chromium`, or this script falls back to the
// installed Microsoft Edge.
//
// The cards' text is set in Segoe UI (falling back to the system UI font), the site's font on
// Windows, so render on Windows to reproduce the committed images exactly.
//
// Writes:
//   images/icon-128.png         NuGet package icon: logo.svg, transparent background.
//   images/social-preview.png   1280×640 GitHub social preview. Not used by anything in the repo:
//                               upload it in the repository's Settings > General > Social preview.
//   images/og-default.png       1200×630 default Open Graph image for the site.
// and the site's icons, which Vite copies from site/public/ to the root of the site:
//   site/public/favicon.svg           a copy of logo-small.svg.
//   site/public/favicon-32.png        logo-small.svg at 32 px, transparent, for browsers without SVG favicons.
//   site/public/apple-touch-icon.png  180×180 on a solid background (iOS doesn't do transparency).
//   site/public/og-default.png        a copy of images/og-default.png.

import { createRequire } from "node:module";
import { copyFileSync, existsSync, mkdirSync, readFileSync } from "node:fs";
import { delimiter, join, relative, resolve } from "node:path";
import { pathToFileURL } from "node:url";

const images = import.meta.dirname;
const repo = resolve(images, "..");
const site = resolve(repo, "site/public");
const svg = (name) => readFileSync(resolve(images, name), "utf8");
const dataUrl = (text) => `data:image/svg+xml;base64,${Buffer.from(text).toString("base64")}`;

// `npx -p playwright` puts the package's .bin on PATH but doesn't make it importable from here,
// so find it next to that .bin folder.
async function loadPlaywright() {
  try {
    return await import("playwright");
  } catch {}
  for (const bin of process.env.PATH.split(delimiter)) {
    const pkg = join(bin, "..", "playwright", "package.json");
    if (bin.endsWith(join("node_modules", ".bin")) && existsSync(pkg))
      return await import(pathToFileURL(createRequire(pkg).resolve("playwright")).href);
  }
  throw new Error("Playwright not found. Run this with: npx -y -p playwright@1.58.0 node images/render.mjs");
}

async function launch() {
  const playwright = await loadPlaywright();
  const chromium = playwright.chromium ?? playwright.default.chromium;
  try {
    return await chromium.launch();
  } catch (e) {
    console.warn(`Playwright's Chromium isn't available, using Microsoft Edge. (${e.message.split("\n")[0]})`);
    return await chromium.launch({ channel: "msedge" });
  }
}

const palette = {
  bg: "#17151a", // the site's dark background
  fg: "#ece8e3",
  muted: "#a39b92",
  glow: "rgb(247 103 7 / 0.2)", // flame #f76707
};

// The logo on its own, filling a transparent square.
const icon = (file, size) => `<!doctype html>
<style>html, body { margin: 0; background: transparent; } img { display: block; }</style>
<img src="${dataUrl(svg(file))}" width="${size}" height="${size}" alt="">`;

// Mark, wordmark and tagline, centred on the dark background with a soft glow behind the flame.
// Sizes are in units of the card's height, so both cards share one layout.
const card = (width, height) => {
  const u = height / 630;
  return `<!doctype html>
<style>
  html, body { margin: 0; }
  body {
    width: ${width}px; height: ${height}px;
    display: flex; align-items: center; justify-content: center; gap: ${56 * u}px;
    background: ${palette.bg};
    font-family: "Segoe UI", system-ui, sans-serif;
  }
  .mark { position: relative; }
  .mark::before { content: ""; position: absolute; inset: ${-110 * u}px; background: radial-gradient(closest-side, ${palette.glow}, transparent); }
  img { position: relative; display: block; width: ${260 * u}px; height: ${260 * u}px; }
  h1 { margin: 0; color: ${palette.fg}; font-size: ${132 * u}px; font-weight: 900; letter-spacing: -0.04em; line-height: 1; }
  p { margin: ${22 * u}px 0 0 ${6 * u}px; color: ${palette.muted}; font-size: ${46 * u}px; font-weight: 600; }
</style>
<div class="mark"><img src="${dataUrl(svg("logo.svg"))}" alt=""></div>
<div><h1>Firelight</h1><p>Web Components for F#</p></div>`;
};

// The logo on the cards' background and glow, for home-screen icons. The mark keeps clear of the
// corners that iOS rounds off.
const tile = (size) => `<!doctype html>
<style>
  html, body { margin: 0; }
  body {
    width: ${size}px; height: ${size}px; display: grid; place-items: center;
    background: radial-gradient(closest-side, ${palette.glow}, transparent), ${palette.bg};
  }
  img { display: block; width: ${size * 0.72}px; height: ${size * 0.72}px; }
</style>
<img src="${dataUrl(svg("logo.svg"))}" alt="">`;

const outputs = [
  { path: resolve(images, "icon-128.png"), width: 128, height: 128, html: icon("logo.svg", 128), transparent: true },
  { path: resolve(images, "social-preview.png"), width: 1280, height: 640, html: card(1280, 640) },
  { path: resolve(images, "og-default.png"), width: 1200, height: 630, html: card(1200, 630) },
  { path: resolve(site, "favicon-32.png"), width: 32, height: 32, html: icon("logo-small.svg", 32), transparent: true },
  { path: resolve(site, "apple-touch-icon.png"), width: 180, height: 180, html: tile(180) },
];
const copies = [
  [resolve(images, "logo-small.svg"), resolve(site, "favicon.svg")],
  [resolve(images, "og-default.png"), resolve(site, "og-default.png")],
];

const browser = await launch();
try {
  mkdirSync(site, { recursive: true });
  for (const { path, width, height, html, transparent = false } of outputs) {
    const page = await browser.newPage({ viewport: { width, height }, deviceScaleFactor: 1 });
    await page.setContent(html);
    await page.evaluate(() => document.fonts.ready);
    await page.screenshot({ path, omitBackground: transparent });
    await page.close();
    console.log(`${relative(repo, path)} (${width}×${height})`);
  }
} finally {
  await browser.close();
}
for (const [from, to] of copies) {
  copyFileSync(from, to);
  console.log(`${relative(repo, to)} (copy of ${relative(repo, from)})`);
}
