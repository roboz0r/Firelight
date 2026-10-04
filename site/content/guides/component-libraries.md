---
title: Using web component libraries
tagline: Web Awesome, Shoelace, Fluent UI and Carbon tags in F# templates
description: "Use the tags of a web component library such as Web Awesome, Shoelace, Fluent UI or Carbon in Firelight templates: loading, properties and attributes, events, styling with parts and custom properties, and typing elements in F#."
section: guides
order: 90
summary: "Web Awesome, Shoelace, Fluent UI or Carbon tags in F# templates"
lead: "Write a library's tags in a template, such as `<wa-switch>`, and bind their properties and events as you would for any element. A web component library's tags are ordinary custom elements, so they need no wrapper and no F# bindings. This guide uses Web Awesome; the others work the same way."
links:
  - text: Web Awesome
    href: https://webawesome.com/docs/
  - text: "Lit docs: Expressions"
    href: https://lit.dev/docs/templates/expressions/
toc: true
---

A library built on web components ships custom elements: classes that register a tag with the
browser. Once a tag is registered, the browser creates the library's element wherever the tag
appears, in plain HTML or in a Lit template. A Firelight template is HTML, so the library works
there without any glue.

## Libraries

| Library | Built on | A switch | It reports changes with |
|---|---|---|---|
| [Web Awesome](https://webawesome.com/docs/) | Lit | `<wa-switch>` | `change` |
| [Shoelace](https://shoelace.style/) | Lit | `<sl-switch>` | `sl-change` |
| [Fluent UI](https://github.com/microsoft/fluentui/tree/master/packages/web-components) | FAST | `<fluent-switch>` | `change` |
| [Carbon](https://web-components.carbondesignsystem.com/) | Lit | `<cds-toggle>` | `cds-toggle-changed` |

Web Awesome is the successor to Shoelace, from the same team. The libraries differ in their tag
prefixes, their event names and how they load their theme, and each documents its components'
properties, attributes, events, slots, parts and custom properties. This guide shows how each of
those maps to a Firelight template.

## Loading and registering

Install the package, then import each component you use. The import runs the module that
registers the tag, so it's an import for its side effect:

```sh
npm install @awesome.me/webawesome
```

```fsharp
open Fable.Core.JsInterop

importSideEffects "@awesome.me/webawesome/dist/components/switch/switch.js"
```

Import components in the module whose templates use them, so they're registered before those
templates render. Your bundler only includes the components you import.

The other libraries register a tag the same way, with their own paths:

- Shoelace: `@shoelace-style/shoelace/dist/components/switch/switch.js`
- Fluent UI: `@fluentui/web-components/switch.js`
- Carbon: `@carbon/web-components/es/components/toggle/index.js`

The components draw their colours, spacing and fonts from the library's design tokens, which are
CSS custom properties. Load the library's stylesheet once, for the whole document. Custom
properties inherit into shadow roots, so every component on the page, inside yours or not, picks
the tokens up. With Vite, import the stylesheet from your entry module:

```fsharp
open Fable.Core.JsInterop

importSideEffects "@awesome.me/webawesome/dist/styles/themes/default.css"
```

That file holds the default theme's tokens. It also sets `color`, `font-family` and
`color-scheme` on the root element, inside a cascade layer, so any rule of your own for those
wins. The tokens are the light theme's; the `wa-dark` class switches an element and everything in
it to the dark ones, so a dark mode puts it on `<html>`. `dist/styles/webawesome.css` adds utility classes and styles for native elements: it sets
the page's background, the `body`'s font, the margins of headings, paragraphs and lists, and more. Use
it for a page built entirely with Web Awesome, and the theme alone for a page with styles of its
own.

Until its module has run, a tag is an unknown element: the browser shows its text content without
the library's styling. Hide such elements until they're registered with
`wa-switch:not(:defined) { visibility: hidden; }` if the flash matters.

## Properties and attributes

Bind a library element exactly as you would a native one. The rules in
[Binding values](/guides/templates/#binding-values) apply unchanged: an attribute for what you
would write in HTML, a property for live state and for values that aren't strings.

The library's reference lists both names, and they don't always mean the same thing. On
`<wa-switch>`, the `checked` attribute sets `defaultChecked`, the state the switch returns to
when its form is reset. The `checked` property is whether it's on now. Native checkboxes work
the same way, and so do Web Awesome's other form controls and Fluent UI's.

Both switches below follow the same F# value. The first binds the attribute, the second the
property. Turn the first switch on, which also turns the second on, then press Turn both off:

::: example Snippets/LibrarySwitches.fs
<my-switch-bindings></my-switch-bindings>
:::

The second switch turns off. The first stays on: it now has no `checked` attribute, but once
the user has toggled a switch, its default no longer changes what it shows. Bind `.checked`, as
the second does, to control a switch from your state.

Some properties have no attribute at all, because their values can't be strings. Bind them with
a dot. A slider's `valueFormatter`, for instance, is a function, written as an F# lambda:

```fsharp
open Firelight
open type Firelight.Lit

let volume (level: float) =
    html $"""<wa-slider label="Volume" .value={level} .valueFormatter={fun (v: float) -> string v + " %"}></wa-slider>"""
```

## Events

A library's events are DOM events, so `@name` listens for them. Web Awesome's form controls raise
the native `change` and `input` events, and its own events have a `wa-` prefix, such as
`wa-show` and `wa-after-hide` on a dialog. Shoelace prefixes them all with `sl-`.

The element that raised a form control's event holds its new state, as with a native input.
`Ev.checked'` and `Ev.value` read `checked` and `value` from the element the listener is on, so
they work on library controls that have those properties:

```fsharp
open Firelight
open type Firelight.Lit

let notifications (on: bool) (setOn: bool -> unit) =
    html $"""<wa-switch .checked={on} @change={Ev.checked' setOn}>Notifications</wa-switch>"""
```

`Ev.value` types the value as a `string`, so use it only where the property is one. A
`<wa-slider>`'s `value` is a number: `Ev.valueAs<float>` reads it as one, as in
[Typing elements](#typing-elements).

Some events carry their data in `detail`. A dialog's `wa-hide` says which element closed it, in
`detail.source`. Declare the detail's shape as an interface, and handle the event with
`Ev.custom<'T>`:

```fsharp
open Browser.Types
open Firelight
open type Firelight.Lit

type HideDetail =
    abstract source: Element

let dialog (onHide: Element -> unit) =
    html
        $"""
    <wa-dialog label="Settings"
        @wa-hide={Ev.custom<HideDetail> (fun e -> e.detail |> Option.iter (fun d -> onHide d.source))}>
    </wa-dialog>"""
```

The [Events guide](/guides/events/) covers handlers in full.

## Styling

A library component renders inside its own shadow root, so your selectors can't reach its inner
elements. It offers two ways in, custom properties and parts, and its reference lists both. The
theme's design tokens are custom properties too, read by every component:

| Hook | Use it to | Example |
|---|---|---|
| Custom properties | Set values the component reads | `wa-switch { --width: 2.75rem; }` |
| Design tokens | Change the theme for everything inside an element | `--wa-form-control-activated-color: var(--accent);` |
| Parts | Style an inner element the component exposes | `wa-switch::part(thumb) { box-shadow: none; }` |

The demo above uses all three: a wider switch, the site's own colours (its ember for "on"), and a
shadow on the thumb. Because its tokens come from this site's palette, the demo follows the site's
theme button, light or dark. They go in your component's `styles`, or in the page's stylesheet for elements
outside any component.

A part's rules apply only to the element the component marked with `part`. Selectors can't go
further in: `wa-switch::part(control) span` isn't a valid selector, so the browser drops the rule.

## Typing elements

To read or call members of a library element from F#, through a [ref](/guides/templates/#directives)
or an event's target, declare an interface with the members you use:

```fsharp
open Fable.Core
open Browser.Types
open Firelight
open type Firelight.Lit

/// The members of <wa-dialog> this module uses.
[<AllowNullLiteral>]
type WaDialog =
    inherit HTMLElement
    abstract ``open``: bool with get, set

let dialog = createRef<WaDialog> ()

let showDialog () =
    dialog.value |> Option.iter (fun d -> d.``open`` <- true)

let view () =
    html $"""<wa-dialog {ref dialog} label="Saved">Your changes are saved.</wa-dialog>"""

// <wa-slider>'s value is a number.
let volume (level: float) (setLevel: float -> unit) =
    html
        $"""
    <wa-slider label="Volume" .value={level} @input={Ev.valueAs<float> setLevel}>
    </wa-slider>"""
```

`Ev.valueAs<float>` reads `value` from the element the listener is on, `currentTarget`, as
`Ev.value` does, and hands it over as a `float`. It converts nothing: it's only right because the
slider's `value` really is a number.

An interface over a JavaScript object costs nothing at run time: [Fable](https://fable.io/)
reads and writes the members directly. Declare only what you use; the interface doesn't have to
describe the whole element. It is optional, too. Templates don't need it, since a template's tags
and bindings are text that only Lit reads, and F# doesn't check them against any type.

`open` is an F# keyword and `checked` is reserved, so members with those names need double
backticks. A cast to an interface, such as `e.target :?> WaDialog`, isn't checked at run time:
the element is whatever the event's target is.

## Prerendering

Web Awesome supports Lit SSR, so its components can be rendered at build time along with yours.
A few of its attributes matter only then, such as `with-label` on a component whose label goes in
a slot: it tells the prerendered markup to include the label before the component hydrates.
Check a library's documentation before prerendering its components, since being built on Lit
doesn't make a component safe to render in Node.

The module that prerenders must load in Node, too. The demo on this page imports the theme's CSS
file with `?inline`, which Vite understands and Node doesn't, so this site's build loads its demos
through Vite's SSR module runner, as `vite dev` does. A build that imports them with Node's own
`import()` can't load such a module: there, keep CSS imports in the module that starts the app,
out of the modules you prerender. The [Prerendering guide](/guides/prerendering/) covers what Lit
SSR runs.

## Common mistakes

The compiler catches some:

| You wrote | The compiler says | Write instead |
|---|---|---|
| `abstract checked: bool with get, set` | The identifier 'checked' is reserved for future use by F# | ```abstract ``checked``: bool with get, set``` |
| `@change={fun e -> setOn e.target...}` | The type of this expression could not be inferred before accessing its members | `@change={Ev.checked' setOn}` |

The rest compile, and fail quietly in the browser:

| You wrote | What happens | Write instead |
|---|---|---|
| No import for a tag you use | The tag renders as its plain text, with no error | `importSideEffects` for each component |
| `checked={on}`, without `?` or `.` | `false` becomes the text "false", and the attribute's presence turns the default on | `.checked={on}` |
| `?checked={on}` to control the switch | It stops following `on` once the user has toggled it | `.checked={on}` |
| `@input={Ev.value setVolume}` on `<wa-slider>` | `setVolume` gets a number, though F# calls it a `string` | `Ev.valueAs<float> setVolume` |
| No theme stylesheet | Components lose their colours and spacing | Load it once for the document |
| `wa-switch .thumb { ... }` | Matches nothing inside the shadow root | `wa-switch::part(thumb)` |
| A `<form>` around your component, and library controls inside its shadow root | The controls aren't part of the form, and their values aren't submitted | The `<form>` in the same template as its controls |
| The library loaded twice, from a CDN and from npm | `customElements.define` throws: the tag is already defined | One copy, from npm |
