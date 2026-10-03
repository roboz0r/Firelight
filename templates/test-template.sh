#!/usr/bin/env bash
# Smoke test for the Firelight.Templates pack, run by .github/workflows/templates.yml.
# Packs the template and the local src/Firelight, creates an app with `dotnet new firelight`,
# builds it and checks it in Chromium with Playwright. Works on Linux, macOS and Git Bash.
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
import { preview } from "vite";
import { chromium } from "playwright";

const server = await preview({ logLevel: "warn" });
const browser = await chromium.launch();
try {
  const page = await browser.newPage();
  const errors = [];
  page.on("pageerror", (error) => errors.push(error.message));
  page.on("console", (message) => message.type() === "error" && errors.push(message.text()));
  await page.goto(server.resolvedUrls.local[0]);

  // CSS locators pierce shadow DOM, so this finds the button inside <click-counter>.
  const button = page.locator("click-counter button");
  await button.filter({ hasText: "Clicked 0 times" }).waitFor({ timeout: 10_000 });
  const radius = await button.evaluate((b) => getComputedStyle(b).borderRadius);
  if (radius !== "8px") throw new Error(`Component styles not applied: border-radius is ${radius}`);
  await button.click();
  await button.filter({ hasText: "Clicked 1 times" }).waitFor({ timeout: 10_000 });
  if (errors.length > 0) throw new Error(`Errors in the page:\n${errors.join("\n")}`);
  console.log("Smoke test passed: <click-counter> rendered, is styled and counts clicks.");
} finally {
  await browser.close();
  await server.close();
}
EOF
node smoke.mjs
