---
title: Prerendering components at build time
tagline: Lit SSR in a static build
description: "Prerender Firelight components to HTML at build time with Lit SSR, ship them as declarative shadow DOM and hydrate them in the browser: what runs on the server, the rules for prerender-safe components, the double-render trap, and a recipe for Vite."
section: guides
order: 70
summary: "Lit SSR at build time: declarative shadow DOM, hydration and prerender-safe components"
lead: "Prerender your components to HTML when you build your site, so the page shows them before any JavaScript loads. Lit SSR does the rendering, and in the browser Lit hydrates each component: it takes over the HTML that's there instead of rendering it again. This guide covers what runs at build time, the rules a component must follow, and how to set it up with Vite."
links:
  - text: "Lit docs: Server rendering"
    href: https://lit.dev/docs/ssr/overview/
  - text: "Lit docs: Authoring for SSR"
    href: https://lit.dev/docs/ssr/authoring/
toc: true
---

Firelight components are Lit components, so Lit SSR (`@lit-labs/ssr`, which runs in Node) can
render them. This site is built that way. Its pages are Markdown, turned into HTML at build time by
a renderer written in F#, and every demo on them is in the page's HTML before its script loads.

Prerendering here means at build time only. No server renders pages on request or keeps track of
what a component does in the browser, and as Lit SSR runs in Node, a .NET server can't run it. For
an application, rendering in the browser is the default. Prerender the pages people should see
before the JavaScript arrives, such as documentation, a landing page or an application's loading
screen.

## What runs at build time

Lit SSR renders a component in Node by creating it and asking it for its template. It runs:

- the code at the top level of each module it imports, such as `defineElement` calls;
- the constructor, including `let` bindings and the controllers they create;
- `willUpdate` and `render`, with the properties that attributes and the parent's template set.

It doesn't run `connectedCallback`, `shouldUpdate`, `update`, `firstUpdated`, `updated`, event
handlers or any controller method. Node has no `window` or `document`. Lit provides a small
stand-in with a global `customElements` registry, which Firelight's `defineElement` uses, and
little else.

The result is the component's HTML inside a `<template shadowrootmode="open">`, which is
declarative shadow DOM: the browser's HTML parser attaches it to the element as its shadow root,
styles included, without JavaScript. For the counter from `dotnet new firelight`:

```html
<click-counter><template shadowroot="open" shadowrootmode="open"><style>
    button { font: inherit; padding: 0.5rem 1rem; border-radius: 0.5rem; cursor: pointer; }
    </style><!--lit-part ShslJ/USeUY=-->
    <!--lit-node 0--><button >
        Clicked <!--lit-part-->0<!--/lit-part--> times
    </button><!--/lit-part--></template></click-counter>
```

The comments mark the template and its holes, so Lit can find them again in the browser.
(`shadowroot` is the attribute's older name, for older browsers.)

## Hydration

In the browser, Lit needs hydration support, from `@lit-labs/ssr-client`, to use those markers.
It changes `LitElement` so that a component that arrives with a shadow root doesn't render on its
first update. Instead, Lit matches the template `render` returns against the HTML that's there,
and connects each hole to its place in the DOM. Event listeners are attached then; until then, the
component is visible but doesn't respond.

This component shows which side rendered it. Turn JavaScript off in your browser and reload to see
what the build wrote; with JavaScript on, the first two lines change once the component has
hydrated, and the button works:

::: example Snippets/PrerenderProbe.fs
<my-prerender-probe></my-prerender-probe>
:::

Hydration trusts that the first render in the browser produces what the build did. The component
renders the same first: `hydrated` starts `false` on both sides. It reads `window` only in
`connectedCallback` (and cleans up as in [Lifecycle](/guides/lifecycle/#connected-and-disconnected)),
and only changes what it shows after `updateComplete`, once the hydrating update has finished.
Changing it in `firstUpdated` would work too, but Lit's development build warns about an update
started there.

## Rules for prerender-safe components

- Register with `defineElement`. It uses the global `customElements`, which exists in Node.
- Use `window`, `document` and other browser APIs only in `connectedCallback`, `firstUpdated`,
  `updated`, event handlers and controllers' `hostConnected` and later methods. Module-level code,
  the constructor, `willUpdate` and `render` run in Node. Where code that runs in both places must
  differ, test Lit's `isServer` (from `open type Firelight.Lit`): it's `true` in Node and `false`
  in the browser. Keep the first render the same either way.
- Render the same first in the browser as at build time. State that only the browser knows, such
  as the window's size or a saved preference, changes after the first update, as above.
- Do first-time work in `willUpdate` under `not this.hasUpdated`, not only when `changed` lists a
  property. In the browser, the first update lists every property that has a value; at build time,
  it leaves out a property that still has the value its `member val` gave it.
- Have the data ready before rendering. Lit SSR doesn't wait for promises: `until` renders its
  placeholder, and a task renders its initial state. Pass data in as properties at build time,
  and give the browser the same data for its first render.
- Put `nothing`, not `""` or `None`, in a hole that starts empty and gets text later. Lit SSR
  writes no text for an empty value, so after hydration Lit has no text node to update, and writes
  the text into one of its comments instead, where it's never seen.
- Bind boolean attributes such as `checked`, `disabled` and `hidden` with `?`, not as properties.
  For `.checked={false}`, Lit SSR writes `checked="false"`, and any value means ticked. For a
  checkbox, `?checked` only sets the starting state: once someone has clicked the box, the
  attribute no longer changes it. If your code changes the box after that, also set its
  `checked` property in `updated`, through a `ref`.
- Inherit `LitElement`, not `LightDomElement`. Lit SSR always renders into a shadow root, and in
  the browser a `LightDomElement` renders its children next to it, where the shadow root hides
  them: the prerendered copy stays on screen, and never changes.

For text that may be missing, it's simplest to leave out the whole element, so the empty case is
`nothing`:

```fsharp
open Firelight
open type Firelight.Lit

let errorLine (error: string option) =
    match error with
    | Some message -> html $"""<p class="error">{message}</p>"""
    | None -> nothing
```

What happens when the first render differs depends on the difference. A hole whose text or
attribute differs keeps what the build wrote, while Lit records the new value, so the page shows
the old value until the next change. A different template in a hole throws "Hydration value
mismatch: Unexpected TemplateResult rendered to part" in the browser's console, and the component
stops there.

## The double render

Hydration support must run before `LitElement` is defined. If Lit loads first, nothing fails and
nothing is logged: the component renders a second copy of itself into its prerendered shadow root,
and the page shows both.

Import order alone doesn't guarantee that once a bundler is involved. When this site had a
`<script type="module">` for hydration support followed by one for each demo, Vite merged them into
one entry, and with several pages sharing Lit, it moved `LitElement` into a shared chunk that
loaded first. The rating demo showed ten stars instead of five.

Load hydration support with a static import, then the components with `import()`, which can't
start until the module that calls it has run:

```html
<script type="module">
  import "@lit-labs/ssr-client/lit-element-hydrate-support.js";
  import("/build/App.js");
</script>
```

For the same reason, a script on a page with prerendered components must not import Lit, or a
module that uses Lit, statically.

## Opting a component out

Some components can't render the same at build time as in the browser. Leave them out of the
prerendered HTML: an element that isn't registered when Lit SSR runs is written out as it is, and
renders in the browser like any other. Three of this site's package pages do this, with
`ssr=false` on their demos:

- **Router**: `RouterController` reads `window.location` when it's created, and a page rendered at
  build time has no address, while the routing demo serves many from one page.
- **Virtualizer**: which rows it renders depends on the size of the scrolling element, which only
  the browser can measure.
- **Task**: `LitTask` starts its task in `hostUpdate`, which Lit SSR doesn't call, so the browser's
  first render shows the pending state where the build rendered the initial one.

On this site, leaving a demo's module out of one page isn't enough, because the build renders
every page in one Node process: once any page has registered an element, Lit SSR prerenders it
everywhere. So the renderer replaces an `ssr=false` demo's HTML with a placeholder comment before
rendering, and puts it back afterwards.

## How this site does it

The site follows the same steps. A Vite plugin in `vite.config.js` gives Rollup one virtual
`.html` input per Markdown page, and renders each when Rollup loads it:

```js
resolveId: (id) => (inputs.has(id) ? id : undefined),
load: (id) => (inputs.has(id) ? render(inputs.get(id)) : undefined),
```

`render` calls the renderer, an F# project in `site/Renderer` that
[Fable](https://fable.io/) compiles to JavaScript. It imports each demo's module, so the demo's
elements are registered, then renders the whole page with Lit SSR. In `Prerender.fs`:

```fsharp fragment
// Registering a demo's custom elements is what makes Lit SSR prerender them.
for demo in body.Demos do
    if demo.Prerender then
        do! host.loadModule demo.Module
```

`LitSsr.fs` binds the two functions it needs from `@lit-labs/ssr`:

```fsharp fragment
[<Import("render", "@lit-labs/ssr")>]
let private render (value: obj) : RenderResult = jsNative

[<Import("collectResult", "@lit-labs/ssr/lib/render-result.js")>]
let private collectResult (result: RenderResult) : JS.Promise<string> = jsNative

/// Renders a template, including the shadow roots of any registered custom elements, to a string.
let renderToString (template: TemplateResult) : JS.Promise<string> = collectResult (render template)
```

The page around the demos is a server-only template (the `html` from `@lit-labs/ssr`), so only
the demos carry hydration markers. `Layout.fs` then writes the module script shown under
[The double render](#the-double-render), with an `import()` for each demo on the page.

## Prerender your own Vite project

Start from the project `dotnet new firelight` creates (see [Get started](/start/)), with its
`<click-counter>`. Install Lit SSR for the build, and hydration support for the browser:

```sh
npm install @lit-labs/ssr-client
npm install --save-dev @lit-labs/ssr
```

In `index.html`, replace the `<script>` that loads `/build/App.js`:

```html
<script type="module">
  // Hydration support first: it must run before Lit defines LitElement.
  import "@lit-labs/ssr-client/lit-element-hydrate-support.js";
  // Then the app. Fable compiles App.fs to build/App.js.
  import("/build/App.js");
</script>
```

Add a plugin to `vite.config.js` that renders the component into the page when you build:

```js
import { defineConfig } from "vite";
import { resolve } from "node:path";
import { pathToFileURL } from "node:url";

// Renders <click-counter> with Lit SSR when building, so the page shows it before any
// JavaScript has loaded. index.html then hydrates it in the browser.
function prerender() {
  return {
    name: "prerender",
    apply: "build",
    async transformIndexHtml(page) {
      const { render, html } = await import("@lit-labs/ssr");
      const { collectResult } = await import("@lit-labs/ssr/lib/render-result.js");
      // Importing the app registers its elements. Fable has compiled it before `vite build` runs.
      await import(pathToFileURL(resolve(import.meta.dirname, "build/App.js")).href);
      const counter = await collectResult(render(html`<click-counter></click-counter>`));
      return page.replace("<click-counter></click-counter>", counter);
    },
  };
}

// https://vite.dev/config/
export default defineConfig({
  // `npm run dev` starts Vite from `dotnet fable watch`, so keep Fable's compiler output on screen.
  clearScreen: false,
  plugins: [prerender()],
});
```

Run `npm run build`, then `npm run preview`. The built `dist/index.html` has the counter's
declarative shadow DOM, and with JavaScript off the page still shows "Clicked 0 times". `npm run dev`
doesn't prerender, and needs nothing else: hydration support leaves components without a
prerendered shadow root to render as usual.

The import uses a file URL built from `import.meta.dirname`, rather than `import("./build/App.js")`,
so that Vite, which bundles `vite.config.js` before running it, leaves the import to Node. To
prerender more elements, render each the same way, or render a larger piece of the page.

## Common mistakes

Some mistakes stop the build or show an error in the browser:

| You wrote | What you see | Write instead |
|---|---|---|
| `window`, `document` or `localStorage` at a module's top level, in a constructor, `willUpdate` or `render` | The build fails: "ReferenceError: window is not defined" | Use them in `connectedCallback` or later |
| A different template in the browser's first render, from an `if` on browser state | The console: "Hydration value mismatch: Unexpected TemplateResult rendered to part" | Render the same first, and change after `updateComplete` |

Others fail silently:

| You wrote | What happens | Write instead |
|---|---|---|
| A static import of Lit or your components before hydration support | Each component renders a second copy of itself | Hydration support, then `import()` |
| `{message}`, which is `""` or `None` at first | Text set later never appears: Lit writes it into a comment | `nothing` until there's text: `{if message <> "" then html $"{message}" else nothing}` |
| `.checked={false}`, `.disabled={false}` or `.hidden={false}` | Lit SSR writes `checked="false"`, so the box is ticked until the component hydrates, and for good without JavaScript | `?checked={flag}`, and see the rules above |
| Browser state, such as `window.innerWidth`, in the first render | The prerendered text stays, though Lit thinks it has changed | Show it after `updateComplete` |
| A `LightDomElement` | The prerendered copy stays on screen and the live one is hidden | `LitElement`, or leave it out |
| `if changed.ContainsKey "items" then ...` in `willUpdate` | Not run at build time while `items` has its `member val` value, so the prerendered HTML lacks what it computes | `if not this.hasUpdated \|\| changed.ContainsKey "items"` |
