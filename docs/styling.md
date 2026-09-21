# Styling Components

A component's shadow root is a style boundary. Rules defined in the document - a Tailwind build, a design system, a third-party theme - do not cross it, and rules defined inside it do not leak out. That isolation is the point of shadow DOM, but it raises a practical question for any app that already has a global stylesheet: **how do those global rules reach the components?**

This guide covers the recommended answer - importing CSS as a string at build time and adopting it as a constructed stylesheet - along with the alternatives and the cases where they are the better fit.

---

## The recommended pattern: build-time import + constructed stylesheets

Put the app's stylesheets in one module. Import each file as a *string* using the bundler's inline query, turn it into a `CSSStyleSheet`, and hand the array to `static member styles`.

```fsharp
module Styles

open Fable.Core
open Browser

[<ImportDefault("./App.css?inline")>]
let private appCss: string = jsNative

[<ImportDefault("./public/fonts/material-symbols/style.css?inline")>]
let private symbolsCss: string = jsNative

let private makeSheet (cssText: string) =
    let sheet = CSSStyleSheet.Create()
    sheet.replaceSync cssText
    sheet

let appSheet = makeSheet appCss
let symbolsSheet = makeSheet symbolsCss

let allComponentStyles = [| appSheet; symbolsSheet |]
```

Any component that needs the global rules adopts them:

```fsharp
[<AttachMembers>]
type MyApp() =
    inherit LitElement()

    static member styles = Styles.allComponentStyles

    override this.render() =
        html $"""<div class="flex flex-col min-h-screen">...</div>"""
```

Lit's `styles` accepts a native `CSSStyleSheet` anywhere it accepts a `CSSResult` - the binding type `CSSResultOrNative` is a union of the two - so sheets and `css` literals mix freely:

```fsharp
static member styles =
    cssResultGroup {
        yield! Styles.allComponentStyles
        css $$""":host { display: block; } .card { padding: 1rem; }"""
    }
```

### Why this works well

- **Build time, not runtime.** `?inline` runs the file through the full CSS pipeline - Tailwind generation, `@import` resolution, PostCSS, minification - and hands the finished text to the module as a plain string. Nothing has to be discovered, fetched, or parsed when the component first renders.
- **No timing dependency.** The sheet exists before the first `render()`. There is no window in which the component is live but unstyled.
- **Parsed once, shared everywhere.** A constructed `CSSStyleSheet` is a single object adopted by many shadow roots. Ten instances of a component cost one parse and one copy in memory, not ten `<style>` blocks.
- **Composable in F#.** Stylesheets become ordinary values. They can be grouped per feature, passed around, and combined with `css` literals in a `cssResultGroup`.

### Keep the `<link>` in `index.html` as well

The constructed sheets style shadow roots; they do not style the document. Keep the regular `<link rel="stylesheet">` tags so the page shell, `<body>` background, and any light-DOM content are styled too:

```html
<link id="app-symbols" rel="stylesheet" href="/fonts/material-symbols/font.css">
<link id="app-fonts" rel="stylesheet" href="/fonts/fonts.css">
<link id="app-styles" rel="stylesheet" href="/App.css">
```

The file is parsed once per context - the document sheet and the constructed sheet are separate copies - but both are available immediately, with no fetch triggered at render time.

---

## The `@font-face` rule is document-scoped

`@font-face` is registered on a **document**, not on a shadow root. A font face declared inside a constructed sheet that is only adopted by shadow roots is ignored - the text renders in a fallback font while the `.woff2` sits unused.

Split font files along that line:

- **`font.css`** - only the `@font-face` blocks. Loaded with a `<link>` in `index.html` so the faces are registered on the document.
- **`style.css`** - the classes that *use* the family (`.material-symbols-outlined { font-family: 'Material Symbols Outlined'; ... }`). Inlined into `Styles.fs` so the classes work inside shadow roots.

Once a face is registered on the document, shadow roots can use it: font resolution is global even though the rules that request it are scoped. The same reasoning applies to other document-level at-rules, such as `@property` registrations.

---

## `replaceSync` and `@import`

Constructed stylesheets do not support `@import` - the rules are dropped. This is not a limitation in practice because `?inline` already resolved every `@import` at build time. It does mean that any hand-written CSS string passed to `replaceSync` must be self-contained.

---

## CSS module scripts - the same idea, natively

The platform has a purpose-built form of this. An [import attribute](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Statements/import/with) brings a stylesheet in directly as a `CSSStyleSheet`, with no string round-trip and no `replaceSync` call:

```js
import sheet from './global.css' with { type: 'css' };

class MyElement extends LitElement {
  static styles = [sheet]; // Lit accepts constructed stylesheets directly
}
```

This is the most web-native answer and it is where the ecosystem is heading. Two things currently block it for Firelight:

- **Fable cannot emit import attributes.** `[<ImportDefault>]` and friends produce a plain `import ... from '...'` - there is no way to attach `with { type: 'css' }` to the generated statement, and without the attribute the browser refuses to load a CSS file as a module.
- **Safari does not support the syntax.** Chromium and Firefox do; shipping it today means shipping a broken stylesheet to Safari users. Check the [browser compatibility table on MDN](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Statements/import/with#browser_compatibility) before revisiting this.

The first can be worked around with a one-line JavaScript shim that Fable imports from:

```js
// styles.js - hand-written, sits next to the F# sources
import sheet from './global.css' with { type: 'css' };
export { sheet };
```

```fsharp
[<Import("sheet", "./styles.js")>]
let appSheet: CSSStyleSheet = jsNative
```

The second cannot. Until Safari ships it, `?inline` + `replaceSync` reaches the same end state - a constructed `CSSStyleSheet` in `static member styles` - with the bundler doing the work the browser will eventually do. Both approaches produce the same runtime object, so migrating later is a change to one module.

---

## Alternatives, and when to reach for them

### `css` literals - the default for component-owned styles

Styles that belong to one component belong in that component:

```fsharp
static member styles =
    css $$"""
    :host { display: block; }
    .counter { display: flex; gap: 0.5rem; }
    """
```

The global-stylesheet pattern above is for *shared* rules. It is not a replacement for per-component CSS, and pulling a large utility framework into a small leaf component is usually the wrong trade.

### `LightDomElement` - opt out of the boundary entirely

`LightDomElement` renders into the host element instead of a shadow root, so document styles, `id` references, `<label for>`, and hash anchors all work without any copying:

```fsharp
[<AttachMembers>]
type ArticleBody() =
    inherit LightDomElement()

    override this.render() = html $"""<article class="prose">...</article>"""
```

The cost is that the component has no style encapsulation, no `:host` selector, and no `<slot>` projection. It suits content-heavy components - rendered Markdown, form fragments participating in a document-level form - where global styling is the goal rather than the obstacle.

### Scraping `document.styleSheets` at runtime - avoid

It is tempting to walk `document.head` for `<link rel="stylesheet">` elements and either re-emit them inside the shadow root or copy their `cssRules` into a new sheet:

```fsharp
// Don't do this.
let sheets = document.head.querySelectorAll "link[rel='stylesheet']"
// ...clone each link.sheet.cssRules into a constructed sheet at render time
```

It works, but every property of it is fragile:

- `link.sheet` is `null` until the file has loaded, so the result depends on when the first render happens - a race that usually resolves in your favour locally and against you on a cold connection.
- Cross-origin sheets throw on `cssRules` access, so CDN-hosted CSS silently drops out.
- Rule-by-rule copying re-serializes and re-parses the whole stylesheet on the main thread, once per component that does it.
- Re-emitting `<link>` tags inside a shadow root produces a visible unstyled flash per component instance.
- The component's styling now depends on the contents of `index.html`, which makes it hard to test in isolation and unusable as a published component.

The build-time import removes the entire class of problem: the CSS text becomes a static dependency of the module that needs it.

---

## Summary

| Need | Approach |
|---|---|
| Styles for one component | `css` literal in `static member styles` |
| App-wide stylesheet (Tailwind, design system) inside shadow roots | `?inline` import into `CSSStyleSheet.replaceSync`, then `static member styles` |
| `@font-face`, `@property`, other document-scoped at-rules | `<link>` in `index.html`; keep them out of the inlined file |
| Page shell and light-DOM content | `<link>` in `index.html` |
| Component that should inherit document styles wholesale | `LightDomElement` |
| `import sheet from './x.css' with { type: 'css' }` | Not yet - Fable cannot emit import attributes, and Safari does not support them |
| Discovering stylesheets from the DOM at runtime | Avoid - timing-dependent, CORS-limited, and slow |
