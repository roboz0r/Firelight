# Site overhaul plan

Goal: turn the site from a set of promo pages into the place people learn Firelight, without
losing what already works.

Keep:

- **Code shown = code that runs.** Snippets are real `.fs` files, compiled by Fable and included verbatim.
- **Static HTML first.** Every page is complete before JavaScript loads; components enhance it.
- **Measured numbers.** Bundle sizes come from the build, not from prose.
- **Built with Firelight.** The page layout and interactive parts of the site are Firelight components.

SSR boundary: Lit SSR runs **at build time only**, rendering each component's initial state to
static HTML (Declarative Shadow DOM). Nothing on a server mirrors client runtime state or DOM
behaviour, and there is no server runtime at all.

Selling point to state on the site: templates are plain HTML and CSS in F# strings, so what people
(and their coding agents) already know about the web platform and Lit carries over directly. The
docs spend their words on the thin Firelight-specific layer instead.

## Decisions

| # | Decision | Recommendation | Why |
|---|----------|----------------|-----|
| D1 | Content pipeline | A build-time page renderer **written in F#**, compiled by Fable and run in Node from a Vite plugin: Fable.MarkdownIt parses the Markdown, Firelight templates provide the layout, and `@lit-labs/ssr` renders the result to HTML | Keeps static-first, and the whole site (pages and components) is built with Firelight. It also shows a sensible use of SSR: prerendering for a static site. Fallback if the spike fails: the same pipeline in plain JS (markdown-it string output in the Vite plugin). Rejected: Starlight/VitePress (excellent, but the site would no longer be built with Firelight) |
| D2 | Reuse `PersonalWebsite/src/Website/MarkdownRenderer.fs` | Reuse the Fable.MarkdownIt setup, the heading ids, the h2/h3 table of contents, the asset-link handling and the rewriter hook (for `::: example` / `::: compare`). Drop the hljs and Tailwind parts | Its output (Lit templates) is exactly what Lit SSR consumes, so the code transfers almost directly. Whether to walk tokens (as it does now) or insert markdown-it's HTML string with `unsafeHTML` is a choice for the spike. Token walking is only worth it if rewriters need to produce real templates |
| D3 | Syntax highlighting | Shiki **at build time** through the existing `Fable.Shiki` bindings, with the same themes as now | Code blocks no longer need JS, and `Fable.Shiki` moves from the browser into the build. `fl-code` stays only if something renders code at runtime. `Fable.Shiki` will eventually be its own package, and the site is where it gets proven. Keep it in `site/Fable.Shiki/` for now, but hold its API to package standard: doc comments, no site-specific types, and both browser and Node use |
| D7 | Prerendered demos | Render each `::: example` demo with Lit SSR (Declarative Shadow DOM) and load `@lit-labs/ssr-client` hydration support on pages that have demos. A demo that can't be prerendered (Virtualizer, Observers, Motion, Router) opts out with `ssr=false` | Demos show their initial state before JS loads, with no layout shift, and stay visible with JS disabled. Making components prerender safely becomes its own guide topic |
| D4 | Navigation source of truth | Page frontmatter (`section`, `order`, `title`, `summary`) | Pages register themselves, so pages written in parallel don't conflict over a shared nav file. Header, sidebar, prev/next, homepage package table, sitemap and `llms.txt` are all generated from frontmatter |
| D5 | Snippet registration | `<Compile Include="Snippets/**/*.fs" />` in `Site.fsproj` | Same reason: avoids a merge hotspot. Snippets are independent modules, so the alphabetical order doesn't matter |
| D6 | Hosting | Stay on GitHub Pages | Works already |

## Information architecture

```
/                         Home (hand-written HTML landing page; package table generated)
/start/                   Getting started: zero to a running component
/why/                     Firelight vs Fable.Lit, Feliz, Sutil, Lit in TypeScript
/guides/<topic>/          Templates, properties & attributes, events, styling, lifecycle,
                          controllers, component communication, context, using web component
                          libraries, using JS libraries, app architecture & loading
/packages/<name>/         Existing nine pages, migrated
/from-lit/<topic>/        Lit TS and Firelight side by side, following lit.dev's topic order
/cookbook/<recipe>/       Small single-purpose examples
/demos/                   Todo, Kanban, client-side routing
/llms.txt, /llms-full.txt, <page>/index.md
```

Source material for the guides: `brainstorming/2..8`, `.claude/skills/firelight/SKILL.md`,
`sample/GettingStarted` (one module per concept), README.

## Authoring format

```md
---
title: Events
description: Raise and handle DOM events from F# components.
section: guides
order: 30
---

Prose…

::: example Snippets/Rating.fs
<my-rating value="3"></my-rating>
:::

::: compare
```ts
// Lit
```
```fsharp fragment
// Firelight
```
:::

```fsharp
// Self-contained: compiled by tests/Docs.Snippets
```

```fsharp fragment
// Not compiled
```
```

- `::: example <file>` produces the existing `.example` layout (source on the left, live demo on
  the right) and adds `<script type="module" src="/build/<file>.js">` to the page automatically.
  With no body, it shows the code only.
- `::: compare` lays out two code blocks side by side using CSS only (no JS tabs).
- Plain ```` ```fsharp ```` blocks must be self-contained, because they get compiled.
  ```` ```fsharp fragment ```` opts out (`tests/Docs.Snippets` skips any block with more after `fsharp` in its info string).
- Internal links are written as `/guides/events/` and the base path is added at build time.

---

## Phase 1: Markdown pipeline, migrating existing pages unchanged (M)

1. **Spike (time-boxed) to answer three questions on one package page:**
   - *Lit SSR of Fable output:* in Node, import `build/Snippets/Counter.js` and `Rating.js`,
     render them with `@lit-labs/ssr`, then hydrate them in the browser. Lit SSR runs only the
     constructor, `willUpdate` and `render` (not `connectedCallback` or `firstUpdated`), so check
     that Fable's module-level code doesn't touch `window` or `document`.
   - *Dev loop:* with `dotnet fable watch` recompiling the F# renderer, the Vite plugin must
     re-import it (cache-busting the import) and reload the page, with no restart needed.
   - *Page output:* in dev, pages are rendered on request through `server.transformIndexHtml`.
     In build, Rollup gets virtual `.html` inputs through `resolveId`/`load`. If virtual inputs
     misbehave, generate the HTML into gitignored folders under `site/` before Vite starts.

   If either of the first two fails, fall back to the JS pipeline (D1) and prerender only demos.
2. Put the renderer in `site/Renderer/` (a Fable project run in Node, separate from the browser
   code in `Site.fsproj`): `Markdown.fs` (Fable.MarkdownIt, Shiki, containers, heading ids, TOC),
   `Layout.fs` (head metadata, header, sidebar, TOC, prev/next, footer as Firelight templates),
   `Pages.fs` (scans frontmatter, builds the nav model) and `Prerender.fs` (Lit SSR to string).
   `vite.config.js` keeps only a thin plugin that calls the renderer.
3. Head metadata on every page: title, description, canonical URL, Open Graph and Twitter tags,
   favicon.
4. Migrate the nine package pages and the routing page to Markdown, with no wording changes.
   Generate the homepage package table and `package-nav` from frontmatter.
5. **Real 404 page.** `404.html` becomes a "page not found" page; an inline script loads the
   routing demo only for paths under `/client-side-routing/`.
6. `site/CLAUDE.md`: authoring conventions (the format above, where snippets go, the
   definition of done). Short.

**Done when:** `npm run build` produces the same set of pages, the rendered text matches the
current site, pages without demos ship no JavaScript, and prerendered demos are visible with
JavaScript disabled.

## Phase 2: Automated checks (M)

Everything after this phase relies on these checks instead of line-by-line review.

1. **`tests/Docs.Snippets`.** Done: `tests/Readme.Snippets` was generalised to compile plain
   ```` ```fsharp ```` blocks in `README.md` and `site/content/**/*.md`. Errors still point at
   Markdown line numbers.
2. **Internal link check inside the build.** The Markdown plugin already knows every page, so an
   unknown internal `href` or `#anchor` fails the build.
3. **`tests/Site.E2E`** (Expecto + Playwright, the same setup as `sample/Kanban.E2E`), run against
   `vite preview`. For every page in the generated sitemap:
   - no console errors and no failed requests (this also catches Lit hydration mismatch warnings);
   - every custom element tag in the DOM is registered (`customElements.get`);
   - no serious or critical axe violations (`Deque.AxeCore.Playwright`);
   - each `::: example` demo renders something.
   Plus a few interaction tests for the flagship demos.
4. **Size budgets.** `build-demos.mjs` entries get `budgetKB`, and the build fails if a demo is over.
   Also report the JS each page ships.
5. **CI.** Build and test the site on pull requests (no deploy). The external link check (lychee)
   runs on a weekly schedule and doesn't block.

**Done when:** deliberately breaking a snippet, a link, a demo and a budget each fails CI.

## Phase 3: Getting started (M; can run alongside phases 1–2)

1. **`dotnet new` template** in `templates/firelight-app/`: fsproj, `App.fs` with one component,
   `index.html`, `vite.config.js`, `package.json` (`npm run dev` / `npm run build`), and tool manifest.
   Published as a `Firelight.Templates` NuGet package (approved).
2. **CI check for the template:** create a project from it, `npm install`, build, and run a
   Playwright smoke test.
3. **`/start/` tutorial:** install the template, run it, change the component, add a property, an
   event, styles. Every step's code is a compiled snippet.
4. Point every "Get started" link at `/start/` instead of GitHub.

**Done when:** someone with only the .NET SDK and Node can follow `/start/` to a running component
without leaving the site.

## Phase 4: Content at scale (L, agents in parallel)

Each page is one self-contained task: a Markdown file plus any snippet files. Thanks to D4 and D5,
no shared files need editing. Agents work in separate worktrees, and a page is done when
`npm run build`, `Docs.Snippets` and `Site.E2E` all pass.

1. **Guides** (about 10). Each one starts from the matching brainstorming document or skill section.
   Add "Prerendering components at build time": what Lit SSR runs, how to keep components
   prerender-safe, and the build-time-only boundary. Use this site as the worked example.
   Update `brainstorming/8-no-ssr.md` to match.
2. **`/why/` comparison.** Fable.Lit is the closest alternative and must be addressed directly.
   Then Feliz/React, Sutil, and plain Lit in TS. Be factual and use measured sizes where possible.
3. **From Lit.** For each lit.dev docs topic, a Lit TS example next to the Firelight one.
   Write our own prose and examples; check the licence of lit.dev's content before quoting any of it.
4. **Cookbook.** Start with about 15 recipes chosen from questions people actually ask: forms and
   validation, fetching data, dialogs, slots, drag and drop, theming with CSS custom properties,
   wrapping a JS library, using Web Awesome components, and so on.

Order: guides → `/why/` → From Lit → cookbook. Review the prose. The checks cover the code.

## Phase 5: Demos and polish (S–M)

1. Add Kanban to `build-demos.mjs` (with a budget) and give it a demo card with its size.
   Run `Kanban.E2E` against the deployed build path.
2. **Brand: a warm campfire.** The opposite of Lit's angular flame: rounded flames, logs or
   embers, a glow. The site's palette is already warm (`--accent: #d9480f`), so this is mostly the
   mark and the hero. This can start at any time; it doesn't depend on other phases.
   - Have an agent produce 4–6 SVG concepts and put them on a contact sheet
     (`site/brand/concepts.html`). A Playwright script renders each one at 16, 32 and 180 px and
     at hero size, on light and dark backgrounds, so they can be compared at a glance.
   - Requirements: recognisable at 16 px, a one-colour version, works on both themes, and no
     resemblance to Lit's mark.
   - From the chosen mark: favicon (SVG plus PNG fallbacks), header mark, hero treatment and a
     default Open Graph image. Use the same mark for the NuGet package icon and the GitHub
     social preview.
3. Version (`PackageVersion` from `Directory.Build.props`, currently 0.2.0) and NuGet links on
   package pages; changelog page; a clear "pre-1.0" note.
4. **Agent-readable output.** `llms.txt` (index generated from frontmatter), `llms-full.txt`, and
   `<page>/index.md` with `::: example` includes expanded into the code itself. Add a short
   "Using Firelight with coding agents" page linking these and the skill.
5. **Search** with Pagefind, run over `dist/` after the build. The search box is a small Firelight
   component.
6. Mobile navigation (CSS first).

## Launch: 0.3.0

The new site and the 0.3.0 release go out together, so every install command on the site works
on day one.

1. The owner signs off on the site (the deploy preview from `npm run build && npm run preview`).
2. Check that the `NUGET_TOKEN` secret's API key can push **new** packages (scope "Push new
   packages and package versions", glob covering `Firelight.*`). Firelight.Signals, Task, Motion,
   Observers, Virtualizer and Templates have never been published.
3. Bump `PackageVersion` in `Directory.Build.props` to 0.3.0 on the release branch. The
   template's default Firelight version follows automatically.
4. Publish **before** the site goes live: tag that commit `v0.3.0` and push only the tag.
   `publish.yml` pushes all nine library packages and `Firelight.Templates`. Wait until all ten
   show 0.3.0 on nuget.org (indexing takes a few minutes).
5. Merge to `main`. `pages.yml` deploys the site. Check the live site, and that every package
   page's NuGet link resolves.

## Later

- API reference generated from XML docs (fsdocs, or a small generator) once the public API settles.
- Visual regression screenshots of every page in `Site.E2E`.
- Extract `Fable.Shiki` into its own package once the site has used it in both the browser and
  Node without API changes for a while. The site's usage becomes its sample and its tests.
