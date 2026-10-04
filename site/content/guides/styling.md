---
title: Styling
tagline: scoped CSS for components
description: "Style Firelight components with CSS in F#: :host, scoped styles, custom properties for theming, ::part and exportparts, shared stylesheets and unsafeCSS."
section: guides
order: 40
summary: "Scoped CSS with css, :host, custom properties, ::part and shared styles"
lead: "Write a component's CSS in `static member styles`, with `css $$\"\"\"...\"\"\"`. Lit applies it inside the component's shadow root, where it styles that component and nothing else, and the page's rules can't reach in. This guide covers the F# side, and the standard ways a page styles a component from outside."
links:
  - text: "Lit docs: Styles"
    href: https://lit.dev/docs/components/styles/
  - text: "MDN: ::part()"
    href: https://developer.mozilla.org/en-US/docs/Web/CSS/::part
toc: true
---

A component's styles are CSS, written in an F# string. Lit turns each `css` value into one
stylesheet and adopts it into the shadow root of every instance, so a hundred instances share one
parsed sheet. The shadow root is a boundary: a `button` rule in the component styles only its own
buttons, and the page's `button` rule doesn't touch them. [Get started](/start/#style-it) shows it
running.

## Component styles

```fsharp
open Fable.Core
open Fable.Core.JsInterop
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type Card() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [
            "heading", PropertyDeclaration<string>()
            "compact", PropertyDeclaration<bool>(reflect = true)
        ]

    static member styles =
        css
            $$"""
        :host { display: block; padding: 1rem; border: 1px solid #e8e1d8; border-radius: 0.5rem; }
        :host([hidden]) { display: none; }
        :host([compact]) { padding: 0.5rem; }
        h3 { margin: 0 0 0.5rem; }
        """

    member val heading = "" with get, set
    member val compact = false with get, set

    override this.render() =
        html $"""<h3>{this.heading}</h3><slot></slot>"""

defineElement<Card> "my-card"
```

`$$"""` makes single braces plain CSS; the [Templates guide](/guides/templates/#templates-are-interpolated-strings)
explains the delimiters. Lit reads `styles` once, when `defineElement` registers the class,
so styles are fixed for the class. What changes per element belongs in attributes, classes or
custom properties, below.

`:host` selects the element itself, from inside:

- A custom element is `display: inline` until you say otherwise, so give `:host` a `display`.
- `:host { display: block }` beats the browser's own rule for the `hidden` attribute, so
  `<my-card hidden>` stays visible. `:host([hidden]) { display: none; }` puts it back.
- `:host([compact])` and `:host(.wide)` style the element by its attributes and classes.
  `compact` has `reflect = true`, so setting the property from F# adds the attribute too: see
  [Reflect a property](/guides/properties/#reflect-a-property-to-its-attribute).
- A page rule on the element, such as `my-card { padding: 0 }`, beats the component's `:host`
  rule. The component's `:host` styles are defaults; where it sits on the page is the page's call.
  The exception is `!important`: across the boundary, the component's `!important` wins, here and
  for `::slotted` and `::part`.

`<slot>` shows the element's children, which stay in the page's DOM and take the page's styles.
`::slotted(p)` styles them from inside, but only the top-level children, and the page's rules
win.

## What crosses the boundary

The shadow root keeps selectors out in both directions, but not everything stops there:

| From the page | Reaches inside? |
|---|---|
| Inherited properties, such as `color`, `font` and `line-height` | Yes, through the host element |
| CSS custom properties, such as `--accent` | Yes, like inherited properties |
| Selectors, such as `button` or `my-card .title` | No |
| `::part(name)` rules | Only for the elements the component names as parts |

So a component can expose two kinds of styling API: custom properties for values, and parts for
whole elements.

## Theming with custom properties

Read a value from a custom property, with a default for when the page sets none:

```css
.bar { background: var(--progress-color, var(--accent, #c2410c)); }
```

The page, or any element above the component, sets `--progress-color`, and the component picks it
up. The chain falls back to a site-wide `--accent`, and then to a fixed colour. Name the component's
own properties after it, and list them where its users will look, since they are part of its API.

This site works the same way. `site.css` defines a palette on `:root`, with `--accent`, `--border`
and `--muted`, and redefines it for dark mode in a `prefers-color-scheme` media query. Every demo on
these pages reads the palette, so the demos follow the site's theme with no code of their own.

These three progress bars are one component, styled from the page's CSS, which is under the code.
A `my-progress` rule gives them their width. The second sets `--progress-color` in its `style`
attribute, and the third is restyled through its parts, covered next. Switch your system between
light and dark mode: the first and third follow this site's palette, and the second keeps its own
colour.

::: example Snippets/StylingParts.fs .stacked
<my-progress label="Photos" value="40"></my-progress>
<my-progress label="Music" value="65" style="--progress-color: #0b6e4f"></my-progress>
<my-progress label="Videos" value="80" class="striped"></my-progress>
<style>
  my-progress { width: 18rem; }
  my-progress.striped::part(track) { height: 1rem; }
  my-progress.striped::part(bar) {
    background: repeating-linear-gradient(45deg, var(--accent) 0 8px, var(--fg) 8px 16px);
  }
</style>
:::

## Style parts from outside with ::part

`part="bar"` on an element in the template exposes it to the page as `my-progress::part(bar)`. The
page's rule can set any property on that element, and it wins over the component's own rule for it.
It reaches only the part itself, not its children: `::part(track) .bar` matches nothing.

A part inside a component that sits in another component's shadow DOM is two boundaries away, and
the page can't reach it. The outer component forwards it with `exportparts`, renaming it if it
likes:

```fsharp
open Fable.Core
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type UploadPanel() =
    inherit LitElement()

    override _.render() =
        html
            $"""
        <h3 part="heading">Uploads</h3>
        <my-progress label="Photos" value="40" exportparts="bar: upload-bar"></my-progress>"""

defineElement<UploadPanel> "my-upload-panel"
```

The page then styles `my-upload-panel::part(upload-bar)`. Parts it doesn't export stay out of
reach.

Use custom properties for values the page will want to change, such as colours and sizes, and
parts when the page needs control of a whole element. Both are promises to whoever styles the
component, so rename them with care.

## Share styles between components

A `css` value is an ordinary F# value. Put shared rules in a module, and give `styles` an array:

```fsharp
open Fable.Core
open Firelight
open type Firelight.Lit

module SharedStyles =
    let buttons =
        css
            $$"""
        button { font: inherit; padding: 0.3rem 0.8rem; border-radius: 0.375rem; }
        """

[<AttachMembers>]
type Toolbar() =
    inherit LitElement()

    static member styles =
        [| SharedStyles.buttons
           css $$""":host { display: flex; gap: 0.5rem; }""" |]

    override _.render() =
        html $"""<button>Undo</button><button>Redo</button>"""

defineElement<Toolbar> "my-toolbar"
```

Lit makes one stylesheet from `SharedStyles.buttons` and adopts it in every component that lists
it. A `css` value can also go in another `css` value's hole, as in the
[Templates guide](/guides/templates/#templates-are-interpolated-strings).

An app-wide stylesheet, such as Tailwind's output, doesn't reach inside shadow roots from a
`<link>` in the page. Import it as text at build time and make it a `css` value:

```fsharp
open Fable.Core
open Firelight
open type Firelight.Lit

module AppStyles =
    /// Our own build output, so unsafeCSS is safe here.
    [<ImportDefault("./App.css?inline")>]
    let private appCss: string = jsNative

    let app = unsafeCSS appCss

[<AttachMembers>]
type Panel() =
    inherit LitElement()

    static member styles = [| AppStyles.app; css $$""":host { display: block; }""" |]

    override _.render() = html $"""<div class="p-4 rounded-lg">...</div>"""

defineElement<Panel> "my-panel"
```

`?inline` is Vite's: it runs the file through the CSS build and gives the module the result as a
string. [`unsafeCSS`](#unsafecss) wraps it as CSS without checking it, which is
right for your own build output and nothing else.

You can also give `styles` a native `CSSStyleSheet` made with `replaceSync`, as
[Styling components](https://github.com/roboz0r/Firelight/blob/main/docs/styling.md) in the
repository does, which also covers keeping `@font-face` rules in the page. An F# array holds one
type, so mix sheets and `css` values with `cssResultGroup { sheet; css $$"""...""" }`. A sheet
can't be made at module load in Lit SSR, which has no `CSSStyleSheet`, so prefer `unsafeCSS` in
components you prerender.

## Values from F#: unsafeCSS {#unsafecss}

A hole in `css` takes only another `css` value or a number. That keeps arbitrary text out of a
stylesheet: a string in a hole throws when that `css` runs. For text you trust, such as a
colour from your build's configuration, `unsafeCSS` turns a string into CSS:

```fsharp
open Firelight
open type Firelight.Lit

/// From our own build configuration, never from users.
let brandColor = "#d9480f"

let brand = css $$""":host { --brand: {{unsafeCSS brandColor}}; }"""
```

Never pass it text that someone else controls. CSS can't run script, but it can make requests:
a `url(...)` loads from any server, and an attribute selector such as `input[value^="a"]` can test
the values in the page's attributes, such as a pre-filled field, and report each guess to that
server, from your page. For values that come from users, or that change at run time, set a custom
property with `styleMap` instead:

```fsharp
open Firelight
open type Firelight.Lit

let progress (color: string) (value: float) =
    let theme = StyleInfo.create [ "--progress-color", Some color ]
    html $"""<my-progress value={value} style={styleMap theme}></my-progress>"""
```

`styleMap` sets one property's value, so the text can't add selectors or rules. Still check it
against what you expect, such as a colour, since a `url(...)` in a value makes a request too.

## Change styles at run time

Styles are fixed for the class, but which rules apply isn't. Three ways to change the look of one
element:

- Toggle a class or set an inline style from the template with `classMap` and `styleMap`: see
  [Directives](/guides/templates/#directives).
- Reflect a property to an attribute and select it with `:host([...])`: see
  [Reflect a property](/guides/properties/#reflect-a-property-to-its-attribute).
- Set a custom property, from the template or from the page.

## Light DOM components

A component that inherits `LightDomElement` instead of `LitElement` renders into the element itself,
with no shadow root. The page's CSS styles it like any other markup, and ids, `<label for>` and
`#anchor` links work across it. In exchange there's no scoping: `styles`, `:host`, `::part` and
`<slot>` don't apply. Use it for content that should look like the rest of the page, such as
rendered Markdown, and shadow DOM for widgets that should look the same everywhere.

## Common mistakes

The compiler catches one:

| You wrote | The compiler says | Write instead |
|---|---|---|
| `[\| sharedCss; sheet \|]` with a `CSSStyleSheet` | All elements of an array must be implicitly convertible to the type of the first element, which here is 'CSSResult'. This element has type 'CSSStyleSheet'. | `cssResultGroup { sharedCss; sheet }` |

The rest compile, because `styles` is an ordinary static member and a `css` hole takes `obj`:

| You wrote | What happens | Write instead |
|---|---|---|
| `static member style` | No styles, and no error | `static member styles` |
| `static member styles = ":host { ... }"` | Throws: Failed to set the 'adoptedStyleSheets' property on 'ShadowRoot' | `css $$""":host { ... }"""` |
| `{{color}}` in `styles`, with a string | Throws "Value passed to 'css' function must be a 'css' function result", and the element is never defined | `unsafeCSS` for trusted text, or a custom property |
| `:host { display: block; }` alone | `hidden` no longer hides the element | Add `:host([hidden]) { display: none; }` |
| Page CSS such as `my-card h3 { }` | Matches nothing inside | A custom property or a part |
| A `<link>` to the app's CSS in `index.html` | Styles the page, not the components | A constructed sheet, as in [Share styles](#share-styles-between-components) |
| A module `mutable` read in `styles` | Read once, so later changes are ignored | A custom property, `classMap` or `styleMap` |
