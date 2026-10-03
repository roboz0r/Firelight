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

import { createRequire } from "node:module";
import { existsSync, readFileSync } from "node:fs";
import { delimiter, join, resolve } from "node:path";
import { pathToFileURL } from "node:url";

const images = import.meta.dirname;
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

const outputs = [
  { file: "icon-128.png", width: 128, height: 128, html: icon("logo.svg", 128), transparent: true },
  { file: "social-preview.png", width: 1280, height: 640, html: card(1280, 640) },
  { file: "og-default.png", width: 1200, height: 630, html: card(1200, 630) },
];

const browser = await launch();
try {
  for (const { file, width, height, html, transparent = false } of outputs) {
    const page = await browser.newPage({ viewport: { width, height }, deviceScaleFactor: 1 });
    await page.setContent(html);
    await page.evaluate(() => document.fonts.ready);
    await page.screenshot({ path: resolve(images, file), omitBackground: transparent });
    await page.close();
    console.log(`${file} (${width}×${height})`);
  }
} finally {
  await browser.close();
}
