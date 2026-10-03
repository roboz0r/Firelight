#!/usr/bin/env bash
# Smoke test for the Firelight.Templates pack, run by .github/workflows/templates.yml.
# Packs the template and the local src/Firelight, creates an app with `dotnet new firelight`,
# builds it and checks it in Chromium with Playwright, served by `vite preview` and by
# `npm run dev`. Works on Linux, macOS and Git Bash.
#
# Usage: templates/test-template.sh        KEEP_WORK=1 keeps the temp directory for inspection.
set -euo pipefail

repo=$(cd "$(dirname "$0")/.." && pwd)
version=$(sed -n 's:.*<PackageVersion>\(.*\)</PackageVersion>.*:\1:p' "$repo/Directory.Build.props")
# Same Playwright as the .NET E2E tests, so they share downloaded browsers.
playwright=$(sed -n 's:.*"Microsoft.Playwright" Version="\([^"]*\)".*:\1:p' "$repo/Directory.Packages.props")

# dotnet and NuGet on Windows need C:/... paths rather than Git Bash's /c/... paths.
native() { if command -v cygpath > /dev/null; then cygpath -m "$1"; else echo "$1"; fi; }
step() { echo; echo "==> $*"; }

work=$(mktemp -d)
cleanup() {
  if [ "${KEEP_WORK:-}" = 1 ]; then echo "Kept $work"; else rm -rf "$work" || true; fi
}
trap cleanup EXIT

feed="$work/feed"
hive="$(native "$work/hive")"
app="$work/SmokeApp"

step "Packing Firelight.Templates and Firelight $version into $feed"
dotnet pack "$repo/templates/Firelight.Templates.csproj" -c Release -o "$(native "$feed")"
dotnet pack "$repo/src/Firelight/Firelight.fsproj" -c Release -o "$(native "$feed")"

# Firelight comes only from the local feed, everything else from nuget.org. A private package
# folder stops NuGet reusing a cached Firelight of the same version downloaded from nuget.org,
# and a private CLI home keeps the user's local tool cache from pointing into it after cleanup.
export NUGET_PACKAGES="$(native "$work/packages")"
export DOTNET_CLI_HOME="$(native "$work/cli-home")"
export DOTNET_NOLOGO=1
cat > "$work/nuget.config" << EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$(native "$feed")" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="local">
      <package pattern="Firelight" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
EOF

step "Creating SmokeApp from the template"
# A private template hive leaves the user's installed templates alone.
dotnet new install "$(native "$feed/Firelight.Templates.$version.nupkg")" --debug:custom-hive "$hive"
dotnet new firelight -n SmokeApp -o "$(native "$app")" --debug:custom-hive "$hive"
grep -q "<PackageReference Include=\"Firelight\" Version=\"$version\" />" "$app/SmokeApp.fsproj" ||
  { echo "SmokeApp.fsproj does not reference Firelight $version:"; cat "$app/SmokeApp.fsproj"; exit 1; }
grep -q "^module SmokeApp.App$" "$app/App.fs" || { echo "App.fs module was not renamed"; exit 1; }

cd "$app"
step "Building SmokeApp"
dotnet tool restore
npm install --no-audit --no-fund
npm ls lit
npm run build

step "Smoke testing SmokeApp with Playwright $playwright"
npm install --no-save --no-audit --no-fund "playwright@$playwright"
npx playwright install ${CI:+--with-deps} chromium
cat > smoke.mjs << 'EOF'
// Checks the app in Chromium, served by `vite preview` (the build) or by `npm run dev`.
// Usage: node smoke.mjs preview|dev
import { spawn, spawnSync } from "node:child_process";
import net from "node:net";
import { preview } from "vite";
import { chromium } from "playwright";

const delay = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
// Whether anything answers HTTP on url, giving up on a request after 2 s.
const answers = (url) => fetch(url, { signal: AbortSignal.timeout(2_000) }).then(() => true, () => false);
// Polls until `done()`, for up to `ms`. Returns whether it got there.
async function waitFor(done, ms) {
  const deadline = Date.now() + ms;
  while (!(await done())) {
    if (Date.now() > deadline) return false;
    await delay(250);
  }
  return true;
}

// The first port from `start` that nothing listens on, over IPv4 or IPv6. (Vite's --strictPort
// fails rather than moving on if something takes it in the meantime.)
async function freePort(start) {
  const bindable = (port, host) =>
    new Promise((resolve) => {
      const probe = net.createServer();
      probe.once("error", (error) => resolve(error.code === "EADDRNOTAVAIL" || error.code === "EAFNOSUPPORT"));
      probe.listen(port, host, () => probe.close(() => resolve(true)));
    });
  for (let port = start; port < start + 100; port++)
    if ((await bindable(port, "127.0.0.1")) && (await bindable(port, "::1"))) return port;
  throw new Error(`No free port from ${start} to ${start + 99}`);
}

async function check(url) {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    const errors = [];
    page.on("pageerror", (error) => errors.push(error.message));
    page.on("console", (message) => message.type() === "error" && errors.push(message.text()));
    await page.goto(url);

    // CSS locators pierce shadow DOM, so this finds the button inside <click-counter>.
    const button = page.locator("click-counter button");
    await button.filter({ hasText: "Clicked 0 times" }).waitFor({ timeout: 10_000 });
    const radius = await button.evaluate((b) => getComputedStyle(b).borderRadius);
    if (radius !== "8px") throw new Error(`Component styles not applied: border-radius is ${radius}`);
    await button.click();
    await button.filter({ hasText: "Clicked 1 times" }).waitFor({ timeout: 10_000 });
    if (errors.length > 0) throw new Error(`Errors in the page:\n${errors.join("\n")}`);
    console.log(`Smoke test passed on ${url}: <click-counter> rendered, is styled and counts clicks.`);
  } finally {
    await browser.close();
  }
}

// `npm run dev` is a tree of processes: npm, `dotnet fable watch` and the Vite it starts. Stopping
// npm alone leaves Vite running, so stop the tree: with taskkill on Windows (Git Bash's kill can't
// reach it), and elsewhere by starting it as its own process group and signalling the group.
async function checkDev() {
  const windows = process.platform === "win32";
  const port = await freePort(5240);
  const url = `http://localhost:${port}/`;
  // Everything after Fable's --run goes to Vite.
  const dev = spawn("npm", ["run", "dev", "--", "--port", String(port), "--strictPort"], {
    stdio: ["ignore", "inherit", "inherit"],
    shell: windows, // npm is npm.cmd there
    detached: !windows,
  });
  let exited = false;
  dev.once("exit", () => (exited = true));
  const stop = (signal) => {
    if (windows) spawnSync("taskkill", ["/pid", String(dev.pid), "/t", "/f"], { stdio: "ignore" });
    else {
      try {
        process.kill(-dev.pid, signal);
      } catch {} // the group has already gone
    }
  };
  // Whether any process of the tree is left. taskkill /t /f ends the whole tree, so on Windows the
  // process started here is enough; elsewhere signal 0 checks the group without affecting it.
  const running = () => {
    if (windows) return !exited;
    try {
      process.kill(-dev.pid, 0);
      return true;
    } catch {
      return false;
    }
  };
  // However this script ends: an error, Ctrl+C or a CI cancellation.
  const onExit = () => stop("SIGKILL");
  process.once("exit", onExit);
  for (const signal of ["SIGINT", "SIGTERM", "SIGHUP"]) process.once(signal, () => process.exit(1));

  let failure = null;
  try {
    // Fable compiles the app first, then starts Vite.
    if (!(await waitFor(async () => exited || (await answers(url)), 180_000)))
      throw new Error(`npm run dev didn't serve ${url} within 3 minutes`);
    if (exited) throw new Error(`npm run dev exited before serving ${url}`);
    await check(url);
  } catch (error) {
    failure = error;
  }

  // Stop it whether or not the check passed: ask, then insist, then make sure.
  stop("SIGTERM");
  if (!(await waitFor(() => !running(), 10_000))) stop("SIGKILL");
  const stopped =
    (await waitFor(() => !running(), 10_000)) && (await waitFor(async () => !(await answers(url)), 10_000));
  // Stopped, so don't signal its process ID again, which the system may since have reused.
  if (stopped) process.off("exit", onExit);
  const notStopped = stopped ? null : new Error(`npm run dev's processes are still running, or ${url} still answers`);
  if (failure) {
    if (notStopped) console.error(notStopped.message);
    throw failure;
  }
  if (notStopped) throw notStopped;
  console.log(`Stopped npm run dev: none of its processes are left and nothing answers on ${url}.`);
}

const mode = process.argv[2];
if (mode === "preview") {
  const server = await preview({ logLevel: "warn", preview: { port: await freePort(4240), strictPort: true } });
  try {
    await check(server.resolvedUrls.local[0]);
  } finally {
    await server.close();
  }
} else if (mode === "dev") {
  await checkDev();
} else {
  throw new Error(`Usage: node smoke.mjs preview|dev (not ${mode})`);
}
EOF
node smoke.mjs preview

step "Smoke testing SmokeApp under npm run dev"
node smoke.mjs dev
