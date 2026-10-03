// Builds sample apps into public/demos/<name>/ and measures their JavaScript.
// Vite serves public/ in dev and copies it into dist/; the site build writes the sizes into the pages.
import { execSync } from "node:child_process";
import { existsSync, mkdirSync, readdirSync, readFileSync, writeFileSync } from "node:fs";
import { join, resolve } from "node:path";
import { gzipSync } from "node:zlib";

const base = "/Firelight/";
const siteRoot = import.meta.dirname;
const ci = process.env.CI === "true";

// budgetKB: the most gzipped JavaScript the demo may ship, in kB (1000 bytes, as the pages show
// sizes). Each is about 10% over the size when it was set, so growth is a decision: going over
// fails the build. Raise a budget on purpose, in the same change that needs it.
const demos = [{ name: "todo", sample: "Todo", budgetKB: 26.5 }];

const run = (command, cwd) => {
  console.log(`\n> ${command}  (${cwd})`);
  execSync(command, { cwd, stdio: "inherit" });
};

// Every .js file the demo ships, raw and gzipped (the same measure Vite reports).
const measureJs = (dir) => {
  const files = readdirSync(dir, { recursive: true }).filter((f) => f.endsWith(".js"));
  const contents = files.map((f) => readFileSync(join(dir, f)));
  return {
    js: contents.reduce((n, c) => n + c.length, 0),
    jsGzip: contents.reduce((n, c) => n + gzipSync(c).length, 0),
  };
};

const sizes = {};

for (const { name, sample } of demos) {
  const dir = resolve(siteRoot, "..", "sample", sample);
  const outDir = resolve(siteRoot, "public", "demos", name);

  if (ci || !existsSync(resolve(dir, "node_modules"))) run("npm ci", dir);
  run("dotnet fable ./ -o ./build/", dir);
  run(`npx vite build --base ${base}demos/${name}/ --outDir "${outDir}" --emptyOutDir`, dir);

  sizes[name] = measureJs(outDir);
  console.log(`${name}: ${sizes[name].js} bytes of JS, ${sizes[name].jsGzip} gzipped`);
}

mkdirSync(resolve(siteRoot, "build"), { recursive: true });
writeFileSync(resolve(siteRoot, "build", "demo-sizes.json"), JSON.stringify(sizes, null, 2));

const kB = (bytes) => `${(bytes / 1000).toFixed(1)} kB`;
console.log("\nDemo sizes (gzipped JavaScript):");
console.table(
  Object.fromEntries(demos.map(({ name, budgetKB }) => [name, { size: kB(sizes[name].jsGzip), budget: kB(budgetKB * 1000) }])),
);

const over = demos.filter(({ name, budgetKB }) => sizes[name].jsGzip > budgetKB * 1000);
for (const { name, budgetKB } of over)
  console.error(
    `\nDemo "${name}" ships ${kB(sizes[name].jsGzip)} of gzipped JavaScript, over its ${kB(budgetKB * 1000)} budget.` +
      ` Make it smaller, or raise budgetKB in site/build-demos.mjs if the growth is intended.`,
  );
if (over.length > 0) process.exit(1);
