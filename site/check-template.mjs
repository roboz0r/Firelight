// The Getting started tutorial (content/start.md) walks through the app that
// `dotnet new firelight -n MyApp` creates from templates/firelight-app/. This fails the build when
// the template no longer matches what the tutorial says about it, so a change to the template also
// gets a look at the tutorial:
// - its first step shows and runs Snippets/Start/App.fs, a copy of the template's App.fs. (The
//   site can't show the template's file directly: `::: example` takes files under site/, and the
//   reader's file says `module MyApp.App`, not `module FirelightApp.App`.)
// - it lists the template's files, runs its npm scripts, and edits its index.html.
import { readdirSync, readFileSync } from "node:fs";
import { resolve } from "node:path";

const root = import.meta.dirname;
const templateDir = resolve(root, "../templates/firelight-app");
const read = (file) => readFileSync(resolve(root, file), "utf8").replace(/\r\n/g, "\n");
const problems = [];

// `dotnet new` replaces the template's sourceName, FirelightApp, with the name given by -n.
const template = read("../templates/firelight-app/App.fs").replaceAll("FirelightApp", "MyApp");
if (read("Snippets/Start/App.fs") !== template)
  problems.push(
    "site/Snippets/Start/App.fs no longer matches templates/firelight-app/App.fs. Copy the template's\n" +
      "  App.fs there, replacing FirelightApp with MyApp, and check that the later steps in\n" +
      "  Snippets/Start/ still follow from it.",
  );

// The files in the tutorial's "What's in it" table, and no others.
const described = [".config", ".gitignore", ".template.config", "App.fs", "FirelightApp.fsproj", "README.md", "index.html", "package.json", "vite.config.js"];
const files = readdirSync(templateDir).filter((f) => !["bin", "obj", "build", "dist", "node_modules"].includes(f));
const added = files.filter((f) => !described.includes(f));
const removed = described.filter((f) => !files.includes(f));
if (added.length || removed.length)
  problems.push(`The template's files changed (added: ${added.join(", ") || "none"}; removed: ${removed.join(", ") || "none"}).`);

const scripts = JSON.parse(read("../templates/firelight-app/package.json")).scripts ?? {};
for (const name of ["dev", "build", "preview"])
  if (!scripts[name]) problems.push(`The template's package.json has no "${name}" script.`);
if (!JSON.parse(read("../templates/firelight-app/.config/dotnet-tools.json")).tools?.fable)
  problems.push("The template's .config/dotnet-tools.json no longer installs Fable.");

const html = read("../templates/firelight-app/index.html");
for (const part of ["<h1>FirelightApp</h1>", "<click-counter></click-counter>", 'src="/build/App.js"'])
  if (!html.includes(part)) problems.push(`The template's index.html no longer contains ${part}.`);

if (problems.length) {
  console.error(
    "The Getting started tutorial (site/content/start.md) no longer matches templates/firelight-app/:\n- " +
      problems.join("\n- ") +
      "\nUpdate the tutorial to match, then this check (site/check-template.mjs).",
  );
  process.exit(1);
}
