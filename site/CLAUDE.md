# Writing site pages

Pages are Markdown in `content/`, rendered to static HTML at build time by the F# renderer in
`Renderer/` (design and history: `PLAN.md`). `content/guides/events.md` is served at
`/guides/events/`; an `index.md` at its folder (`content/index.md` is the homepage). Markdown
before the first `##` sits with the heading and lead; each `##` starts a `<section>`.
`## Heading {#id}` gives a heading its own id instead of one made from its text, and
`{.same-section}` keeps an h2 in the section before it.

## Frontmatter

A page starts with YAML between `---` lines. The first four fields are required (except on
`404.md` and the homepage); the rest are optional.

```yaml
---
title: Events                  # the h1, and the page's name in navigation
description: Raise and handle DOM events from F# components.   # meta description
section: guides                # one of Pages.sections in Renderer/Pages.fs (add a new one there)
order: 30                      # position in the section: lists, prev/next
summary: "Raise `CustomEvent`s" # one line for lists of pages (inline Markdown; package pages need it)
lead: The paragraph under the heading (inline Markdown).
tagline: raise DOM events      # <title> becomes "Events: raise DOM events" (default "Events · Firelight")
pageTitle: Whole <title>       # when "title: tagline" reads badly
links: [{ text: Source, href: "https://github.com/roboz0r/Firelight/tree/main/src/Firelight" }]  # pills under the lead
nuget: Firelight               # package pages: adds the NuGet pill at the version in Directory.Build.props
eyebrow: { text: Firelight.Router, href: /packages/router/ }  # link above the h1; defaults to the section
toc: true                      # h2/h3 table of contents
spa: true                      # 404.html serves it under its route (page scripts don't run there)
layout: home                   # the homepage: title, lead and links (as buttons) become the hero
unlisted: true                 # only linked to (the search page): no section, sitemap, search or .md
---
```

## Voice

`content/guides/templates.md` is the model page; the owner approved its voice.

1. Open with what the developer writes, in their terms, then what it gets them. Within a section,
   the reason comes before the details (a short example may lead).
2. Short, plain sentences; imperatives for instructions ("Type a note…", "Use `repeat` when…").
   "You" only for something the reader actually does. No hype, no exclamation marks, no "simply"
   or "just".
3. Say what is Lit's and spend the words on the F# part: explain as much Lit as the F# examples
   need, and link to lit.dev for the rest.
4. Reference material goes in compact tables (thing, what it does, example), but only when the rows
   are parallel; things that don't line up across columns are lists. Keep cells short: tables stop
   at the reading width. Prose covers the judgement calls: when, why not, the caveat.
5. Check every claim: run what you say about behaviour, compile what you say about types, and quote
   the real compiler message. Say so when an API is awkward. Prefer compiled ```` ```fsharp ```` blocks;
   use `fragment` only when a whole module would bury the point.
6. Put a live demo next to any idea that is easier to see than to read. Make the demo show the
   difference, and tell the reader what to try.
7. State security caveats concretely: what runs, with whose permissions, and the safe alternative.
8. Mistakes are "you wrote / what happens / write instead", split into what the compiler catches
   and what compiles silently.

## Links

Write internal links root-relative: `[Events](/guides/events/)`, also in raw HTML `<a href>`. The
base path (`/Firelight/`) is added for you. Link the first mention of Fable on a page to
https://fable.io/, and only that one. The build fails on any link to a page, `#anchor` or
file that doesn't exist, naming the file and line.

## Code and demos

- ```` ```fsharp ```` blocks are compiled by `tests/Docs.Snippets`, so each is a self-contained module.
  ```` ```fsharp fragment ```` (anything after `fsharp`) is shown but not compiled.
- `::: example Snippets/Rating.fs .stacked`, the demo's HTML, then `:::`: the live demo above the
  source file that defines it, with the module loaded on the page. `.class` options style the
  demo box. With no body it shows just the code.
- `::: demo Snippets/Rating.fs` shows the live demo without its code.
- `::: compare` holds two fenced code blocks (Lit TypeScript, then Firelight) side by side, labelled
  "Lit" and "Firelight". Other labels go on the opening line, separated by `|`:
  `::: compare Lit (no decorators) | Firelight`.
- `::: cards` holds Markdown in which each `### ` heading starts a card, in a grid.
- `::: package-table` (no body) is the table of packages, from the package pages' `summary`.
- Add `ssr=false` to `example`/`demo` for a demo that can't prerender (below).

A demo is one file, `Snippets/<Name>.fs`: one module that registers its elements with Firelight's
`defineElement`. `Site.fsproj` picks it up by glob; Fable compiles it to `build/Snippets/<Name>.js`.
`npm run dev` notices new and deleted snippets without a restart (Fable takes 10–20 s to reread the
project; the page reloads when the module is ready).

## Markdown for agents

Every page is also published as Markdown, at `<page>/index.md`, with `llms.txt` (an index) and
`llms-full.txt` (every page) at the root, for coding agents (`Renderer/Agents.fs`). Containers
become plain Markdown there; a new kind of container needs a Markdown form in `Agents.fs` too.
Page scripts are left out.

## Search

`npm run build` ends with Pagefind (`build:search`), which indexes each page's `<main>` except its
navigation and demos (`data-pagefind-ignore`). The header links to `/search/` (`content/search.md`,
`Components/Search.fs`). In dev, search says it's available in the built site.

## Prerender-safe components

Each demo is rendered in Node at build time by Lit SSR (constructor, `willUpdate` and `render`
only), shipped as declarative shadow DOM and hydrated in the browser. Demo modules load through
Vite's SSR module runner, in the build as in dev, so they may import what Vite handles, such as CSS
with `?inline`. To prerender:

- register with `defineElement`, which uses the global `customElements`;
- reach `window`, `document` and other browser APIs in `connectedCallback`, `firstUpdated`,
  `updated` or event handlers, so module load, constructor, `willUpdate` and `render` stay pure;
- render the same thing first in the browser as on the server.
- render `nothing`, not `""` or `None`, in a text hole that starts empty and fills in later: hydration
  writes the later value into Lit's marker comment, where it never shows (see `/guides/prerendering/`).

Otherwise mark the demo `ssr=false` (Router, Virtualizer and Task are). Check both: with JavaScript
off the demo shows its initial state, and with it on the demo appears once, not twice.

## Page scripts

Pages may carry plain `<script type="module">` blocks. On a page with a prerendered demo, a page
script uses the DOM, the demos' events (as `content/packages/firelight.md` does) or `import()`,
never a static import of Lit or a module using it: hydration support must load before
`LitElement`, and a static import puts Lit first, so the demos silently render twice.

## Done means

From `site/`: `npm run build`. From the repository root: `dotnet build tests/Docs.Snippets` and
`dotnet run --project tests/Site.E2E -- --summary` (after the build; the one-time Chromium install
is in the comment at the top of `tests/Site.E2E/Main.fs`). While writing, `npm run dev` re-renders
on save and logs broken links as warnings.
