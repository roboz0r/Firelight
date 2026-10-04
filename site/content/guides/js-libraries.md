---
title: Using JavaScript libraries
tagline: npm packages in F# components with Fable interop
description: "Use an npm library from a Firelight component: Fable's Import attributes, minimal typed bindings, Emit, loading on demand with importDynamic, and creating and destroying library objects in the component lifecycle."
section: guides
order: 100
summary: "npm libraries in components: imports, bindings, loading on demand, lifecycle"
lead: "Declare the parts of an npm library you call, with Fable's interop attributes, and call them from F#. In a component, create the library's objects once the component's DOM exists, update them when its properties change, and destroy them when it leaves the page."
links:
  - text: "Fable docs: JavaScript interop"
    href: https://fable.io/docs/javascript/features.html
  - text: "Lit docs: Lifecycle"
    href: https://lit.dev/docs/components/lifecycle/
toc: true
---

A JavaScript library needs no wrapper to be called from F#. [Fable](https://fable.io/) compiles
an F# declaration marked with an import attribute to a JavaScript `import`, and a call to it to a
plain call. A binding is only those declarations: the names and types of what you call, written
once, so that the rest of your code is checked F#.

This site's own example is [Fable.Shiki](https://github.com/roboz0r/Firelight/tree/main/site/Fable.Shiki),
the bindings to [Shiki](https://shiki.style/) that highlight every code block on these pages at
build time.

## Importing

Fable's attributes and functions map onto JavaScript's import forms:

| F# | JavaScript | Example |
|---|---|---|
| `[<Import("name", "pkg")>]` | `import { name } from "pkg"` | `[<Import("createHighlighter", "shiki")>]` |
| `[<ImportMember("pkg")>]` | The same, named after the F# value | `[<ImportMember("shiki")>] let codeToHtml ...` |
| `[<ImportDefault("pkg")>]` | `import x from "pkg"` | `[<ImportDefault("canvas-confetti")>]` |
| `[<ImportAll("pkg")>]` | `import * as x from "pkg"` | `[<ImportAll("shiki")>] let shiki: IShiki = jsNative` |
| `importSideEffects "pkg/x.js"` | `import "pkg/x.js"` | A module that registers a custom element |
| `importDynamic<'T> "pkg"` | `import("pkg")` | [Loading on demand](#loading-on-demand) |

The attribute goes on a `let` whose body is `jsNative`, on a static member, or on a class. Fable
writes the `import` only where the value is used, so an unused binding costs nothing.

```fsharp
open Fable.Core
open Fable.Core.JsInterop

[<AllowNullLiteral>]
type Highlighter =
    abstract codeToHtml: code: string * options: obj -> string

[<Import("createHighlighter", "shiki")>]
let createHighlighter (options: obj) : JS.Promise<Highlighter> = jsNative

// Created on first use, then shared: a highlighter is slow to create and holds its grammars
// in memory until it's disposed.
let private highlighter =
    lazy (createHighlighter {| langs = [| "fsharp" |]; themes = [| "github-light" |] |})

let highlight (code: string) =
    promise {
        let! h = highlighter.Value
        return h.codeToHtml (code, {| lang = "fsharp"; theme = "github-light" |})
    }
```

That is a complete binding for this use: one function, one interface for the object it returns,
and anonymous records for the options. An anonymous record compiles to a plain JavaScript object.
A `JS.Promise` is awaited with `let!` in a `promise { }`, from
[Fable.Promise](https://github.com/fable-compiler/fable-promise) (add it to your project), and
`highlight` returns a `JS.Promise` too.

A `lazy` promise that fails stays failed: every later call gets the same error. That suits a
failure that won't fix itself, such as a missing grammar. To retry after a network error, start a
new promise rather than reusing the failed one.

## Typed bindings

Write bindings for what you call, not for the whole library, and grow them as you use more. Each
JavaScript shape has an F# form:

| JavaScript | F# | In Fable.Shiki |
|---|---|---|
| A function | An imported `let`, or a static member of an `[<Erase>]` type | `Shiki.createHighlighter` |
| An object a function returns | An interface with abstract members | `Highlighter` |
| An options object | A `[<ParamObject>]` constructor, or an anonymous record | `HighlighterOptions(langs, themes)` |
| A class | An imported F# class whose members are `jsNative` | (none) |
| A promise | `JS.Promise<'T>` | `JS.Promise<Highlighter>` |

Fable.Shiki groups its functions as static members, so they read as `Shiki.createHighlighter`,
and gives its options objects constructors with named, optional arguments:

```fsharp fragment
/// Languages and themes to load before using a reusable highlighter.
[<AllowNullLiteral; Global>]
type HighlighterOptions
    [<ParamObject; Emit("$0")>]
    (langs: string[], themes: string[], ?langAlias: LanguageAliasMap, ?warnings: bool) =
    member val langs: string[] = nativeOnly with get, set
    member val themes: string[] = nativeOnly with get, set
    // ...

[<Erase>]
type Shiki =
    [<Import("createHighlighter", "shiki")>]
    static member inline createHighlighter(options: HighlighterOptions) : JS.Promise<Highlighter> =
        nativeOnly
```

`ParamObject` turns the constructor's arguments into an object literal, leaving out the optional
ones you don't pass, so `HighlighterOptions([| "fsharp" |], [| "github-light" |])` compiles to
`{ langs: ["fsharp"], themes: ["github-light"] }`. `Erase` keeps the `Shiki` type itself out of the
JavaScript. The site's renderer then calls it like any F# API:

```fsharp fragment
Shiki.createHighlighter (HighlighterOptions(languages, [| "github-light"; "github-dark" |]))
```

An anonymous record is enough for options you pass once. A `ParamObject` type documents the options
and makes them discoverable, which suits a binding that others will use.

A class binding compiles a constructor call to `new`. The [lifecycle example](#creating-and-destroying)
binds Chart.js's `Chart` class this way.

For JavaScript that F# has no syntax for, `Emit` writes the JavaScript you give it, with `$0`,
`$1` for the arguments. Firelight uses it for `LitSignals.updateEffect`, a method that
`@lit-labs/signals` adds to the element:

```fsharp fragment
[<Emit("$0.updateEffect($1, $2)")>]
static member inline updateEffect (host: LitElement, callback: unit -> unit, ?options: SignalEffectOptions) : unit -> unit =
    nativeOnly
```

Nothing checks the text inside `Emit`, so a mistake there shows up only when the code runs. Prefer
the import attributes and interfaces, and use `Emit` for the few things they can't express.

The dynamic operator is the shortest binding of all: `lib?format(value, 2)` calls `format` on any
object. Its result takes whatever type the surrounding code expects, `string` or `float` alike,
and nothing checks it. Use it to try a library out, then replace it with typed declarations for
code you keep.

## Loading on demand

A static import puts the library in the bundle that loads with the page. `importDynamic` loads it
when the code first runs, as a separate file. Use it for a library that is large and needed only
after the user does something, such as an editor or a chart in a dialog.

```fsharp
open Fable.Core
open Fable.Core.JsInterop

type Highlighter =
    abstract codeToHtml: code: string * options: obj -> string

/// The parts of the "shiki" module this code uses.
type ShikiModule =
    abstract createHighlighter: options: obj -> JS.Promise<Highlighter>

// Created on first use, then shared: Shiki loads once, however many times it's needed.
let highlighter =
    lazy
        (promise {
            let! shiki = importDynamic<ShikiModule> "shiki"
            return! shiki.createHighlighter {| langs = [| "fsharp" |]; themes = [| "github-light" |] |}
        })
```

`importDynamic<ShikiModule>` types the module it loads; the interface describes the exports used.
Code that needs the highlighter awaits `highlighter.Value`.

Write the path as a string literal. The bundler finds dynamic imports by reading the code, and a
literal tells it exactly which module to split out. Vite also handles some relative paths built
from a variable, such as `./langs/${name}.js`, but not a package name computed at run time.

Fable.Shiki's fine-grained entry point, `ShikiCore`, is built for this. It takes each language and
theme as a dynamic import, so only the grammars you list are bundled, each in its own file:

```fsharp fragment
ShikiCore.createHighlighterCore (
    HighlighterCoreOptions(
        ShikiCore.createJavaScriptRegexEngine (),
        langs = [| LanguageInput.ofImport (importDynamic "shiki/langs/fsharp.mjs") |],
        themes = [| ThemeInput.ofImport (importDynamic "shiki/themes/github-light.mjs") |]
    )
)
```

This site now highlights code at build time. When it highlighted in the browser, with two
languages and two themes, Shiki was 97.5 kB of gzipped JavaScript on the homepage, which now
ships 26.4 kB in all. When a library's output doesn't depend on the user, consider running it at
build time instead.

## Creating and destroying

A library that draws into an element, such as a chart, a map or an editor, needs that element to
exist, and it keeps state, listeners and timers of its own until you destroy it. A component's
lifecycle gives each step its place:

| Step | Lifecycle method | Why there |
|---|---|---|
| Create the library's object | `firstUpdated` | The component has rendered, so its elements exist |
| Pass it new property values | `updated` | Runs after each render, with the names of the changed properties |
| Destroy it | `disconnectedCallback` | The component has left the page |
| Create it again | `connectedCallback` | The element was moved, or removed and added back |

The [Lifecycle guide](/guides/lifecycle/) covers each method. Here they wrap
[Chart.js](https://www.chartjs.org/):

```fsharp
open Fable.Core
open Fable.Core.JsInterop
open Browser.Types
open Firelight
open type Firelight.Lit

type ChartDataset =
    abstract data: ResizeArray<float> with get, set

type ChartData =
    abstract labels: ResizeArray<string> with get, set
    abstract datasets: ChartDataset[]

/// The parts of Chart.js this component uses.
[<Import("Chart", "chart.js/auto")>]
type Chart(canvas: HTMLCanvasElement, config: obj) =
    member _.data: ChartData = jsNative
    member _.update() : unit = jsNative
    member _.destroy() : unit = jsNative

[<AttachMembers>]
type SalesChart() =
    inherit LitElement()

    let canvas = createRef<HTMLCanvasElement> ()
    let mutable chart: Chart option = None

    static member properties =
        PropertyDeclarations.create [ "values", PropertyDeclaration<float list>(attribute = false) ]

    static member styles = css $$""":host { display: block; position: relative; }"""

    member val values: float list = [] with get, set

    member this.Labels = ResizeArray [ for i in 1 .. this.values.Length -> string i ]

    member this.CreateChart() =
        canvas.value
        |> Option.iter (fun el ->
            let config =
                {|
                    ``type`` = "line"
                    data =
                        {|
                            labels = this.Labels
                            datasets = [| {| label = "Sales"; data = ResizeArray this.values |} |]
                        |}
                |}

            chart <- Some(Chart(el, config)))

    override this.firstUpdated _ = this.CreateChart()

    override this.updated changed =
        if changed.ContainsKey "values" then
            chart
            |> Option.iter (fun c ->
                c.data.labels <- this.Labels
                c.data.datasets[0].data <- ResizeArray this.values
                c.update ())

    override this.disconnectedCallback() =
        base.disconnectedCallback ()
        chart |> Option.iter (fun c -> c.destroy ())
        chart <- None

    override this.connectedCallback() =
        base.connectedCallback ()

        if this.hasUpdated && chart.IsNone then
            this.CreateChart()

    override _.render() = html $"""<canvas {ref canvas}></canvas>"""

defineElement<SalesChart> "sales-chart"
```

A few details matter:

- A `ref` reaches the canvas. It's inside the component's shadow root, where
  `document.querySelector` doesn't look.
- The template has no holes inside the `<canvas>`, so Lit never touches what Chart.js draws.
  Lit updates only the parts of the DOM that its holes point at.
- The data is a `ResizeArray`, which compiles to a JavaScript array. An F# `float[]` compiles to a
  `Float64Array`, which some libraries don't accept as an array.
- `connectedCallback` and `disconnectedCallback` call `base` first. Lit's own versions let the
  component render and connect or disconnect its controllers.

None of these methods run when Lit SSR prerenders the component, so the chart is created only in
the browser. The module that imports Chart.js still has to load in Node to prerender, though; the
[Prerendering guide](/guides/prerendering/) covers that.

## Wrapping a library as a component

`SalesChart` is already the usual shape for a wrapped library: properties in, events out, and the
library object private to the component. The rest of the app writes
`<sales-chart .values={values}>` and never sees Chart.js.

Before this site highlighted code at build time, its `<fl-code>` component wrapped Shiki the same
way, with three more ideas:

```fsharp fragment
// One highlighter for the page, created by the first <fl-code> that needs it.
let private highlighter =
    lazy (ShikiCore.createHighlighterCore (HighlighterCoreOptions(...)))

[<AttachMembers>]
type CodeBlock() as this =
    inherit LitElement()

    let mutable highlighted: string option = None
    let mutable latest = 0

    let highlight (text: string) =
        latest <- latest + 1
        let request = latest

        promise {
            try
                let! h = highlighter.Value
                let result = h.codeToHtml (text, MultipleThemeOptions(this.lang, themes))

                // Ignore a result that arrives after a newer request.
                if request = latest then
                    highlighted <- Some result
                    this.requestUpdate ()
            with e ->
                // The plain code stays on screen.
                console.error ($"fl-code: could not highlight '{this.lang}'", e)
        }
        |> Promise.start

    // ...

    override _.render() =
        match highlighted with
        | Some result -> html $"{unsafeHTML result}"
        | None -> html $"<slot></slot>"
```

- **Share expensive objects.** A `lazy` at module level creates the highlighter once, for every
  instance on the page, on first use.
- **Show something until it's ready.** Until Shiki has loaded, `<slot>` shows the plain code
  written inside the tag, so the page is readable before the library arrives, and if it fails.
- **Discard stale results.** A slow result for old input mustn't replace a newer one, so each
  request is numbered and only the latest is kept.

`unsafeHTML` renders Shiki's output as HTML. That's safe here because Shiki builds the HTML itself
and escapes the code it's given. Don't pass `unsafeHTML` any HTML that came from users.

## Common mistakes

The compiler catches some, with messages that don't always point at the cause:

| You wrote | The compiler says | Write instead |
|---|---|---|
| `Chart(canvas.value, config)` | The type 'HTMLCanvasElement option' is not compatible with the type 'HTMLCanvasElement' | `canvas.value \|> Option.iter (fun el -> ...)` |
| `override this.firstUpdated() = ...` | This override takes a different number of arguments to the corresponding abstract member | `override this.firstUpdated _ = ...` |

The rest compile:

| You wrote | What happens | Write instead |
|---|---|---|
| `override this.connectedCallback() = setup ()`, without `base` | The component never renders | `base.connectedCallback ()` first |
| The library object created in the constructor or `render` | No element yet, or a new object on every render | Create it in `firstUpdated` |
| `document.querySelector "canvas"` | `null`: the canvas is in the shadow root | A `ref` |
| No `destroy` in `disconnectedCallback` | Listeners and timers outlive the component; Chart.js refuses a new chart on the same canvas | Destroy it, and create it again on reconnect |
| `{items}` inside the element the library draws into | Lit and the library overwrite each other | Leave that element empty in the template |
| `data = [\| 1.0; 2.0 \|]` for a library that checks `Array.isArray` | It's a `Float64Array`, and the library rejects it | `ResizeArray [ 1.0; 2.0 ]` |
| `[<Import("chart", "chart.js")>]`, a wrong export name | The bundler reports that the module has no such export | The name from the library's documentation |
| `importDynamic name`, with a package name computed at run time | The bundler can't resolve it, so the built site requests a module that isn't there | A string literal |
