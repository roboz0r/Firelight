<p align="center"><img src="images/logo.svg" width="96" alt="Firelight logo: a flame between angle brackets"></p>

# Firelight

**Web Components for F#.** Build reactive, standards-based UI components using [Lit](https://lit.dev/) and [Fable](https://fable.io/).

Firelight gives you idiomatic F# bindings to Lit's lightweight Web Components platform - type-safe reactive properties, composable templates, the Elmish MVU loop, and cross-component context - all compiling to lean, standards-compliant JavaScript.

---

## Packages

| Package | Description |
|---|---|
| `Firelight` | Core bindings: `LitElement`, `html`/`css` templates, directives, reactive properties |
| `Firelight.Context` | Context protocol for sharing state across component trees without prop drilling |
| `Firelight.Elmish` | Elmish (MVU) integration via reactive controllers |
| `Firelight.Observers` | Reactive controllers for browser mutation, intersection, resize, and performance observers |
| `Firelight.Router` | Client-side routing via the [URL Pattern API](https://developer.mozilla.org/en-US/docs/Web/API/URLPattern) |
| `Firelight.Signals` | Bindings to Lit Labs signals for shared reactive state and targeted template updates |
| `Firelight.Motion` | Bindings to Lit Labs animations and spring controllers |
| `Firelight.Task` | Bindings to Lit's `@lit/task` reactive controller for async work |
| `Firelight.Virtualizer` | Bindings to Lit Labs viewport virtualization for large lists |

## Quick Start

Define a component by inheriting `LitElement`, declare reactive properties, and implement `render`:

```fsharp
open Fable.Core
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type Counter() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [ "count", PropertyDeclaration<int>() ]

    member val count = 0 with get, set

    override this.render() =
        html $"""
            <p>Count: {this.count}</p>
            <button @click={fun _ -> this.count <- this.count + 1}>
                Increment
            </button>
        """

defineElement<Counter> "my-counter"
```

Use it anywhere in HTML:

```html
<my-counter></my-counter>
```

## Elmish Integration

For components with non-trivial state, `Firelight.Elmish` wires an Elmish `Program` directly to the component lifecycle:

```fsharp
open Fable.Core
open Firelight
open Firelight.Elmish
open type Firelight.Lit

type Msg = Increment | Decrement

let init () = 0

let update msg model =
    match msg with
    | Increment -> model + 1
    | Decrement -> model - 1

[<AttachMembers>]
type Counter() as this =
    inherit LitElement()

    let elmish = ElmishController.simple this init update

    override _.render() =
        let model = elmish.model
        html $"""
            <button @click={fun _ -> elmish.dispatch Decrement}>-</button>
            <span>{model}</span>
            <button @click={fun _ -> elmish.dispatch Increment}>+</button>
        """

defineElement<Counter> "my-counter"
```

## Context

`Firelight.Context` lets you broadcast state (like an Elmish dispatch function) to any descendant component, regardless of nesting depth:

```fsharp
open Fable.Core
open Fable.Core.JsInterop
open Firelight
open Firelight.Context
open type Firelight.Lit

type Msg = Increment | Decrement

// Define a typed context: a symbol branded with the type of value it carries
type DispatchContext =
    inherit Context<Msg -> unit>
    inherit symbol

let dispatchContext: DispatchContext = LitContext.createContext (JS.Symbol())

// Provide it from a parent component
[<AttachMembers>]
type App() =
    inherit LitElement()

    let dispatch (msg: Msg) = JS.console.log msg
    let provider = ContextProvider(jsThis, ContextProvider.Options(dispatchContext, dispatch))

    override _.render() = html $"<increment-button></increment-button>"

// Consume it in any descendant
[<AttachMembers>]
type IncrementButton() =
    inherit LitElement()

    let consumer = ContextConsumer(jsThis, ContextConsumer.Options(dispatchContext))

    override _.render() =
        html $"""<button @click={fun _ -> consumer.value |> Option.iter (fun dispatch -> dispatch Increment)}>+</button>"""
```

## Router

`Firelight.Router` provides client-side routing built on the browser's [URL Pattern API](https://developer.mozilla.org/en-US/docs/Web/API/URLPattern). Define routes as URL patterns with typed extractors, and use `RouterController` to wire routing into Lit's reactive lifecycle:

```fsharp
open Fable.Core
open Browser.Types.URLPattern
open Firelight
open Firelight.Router
open type Firelight.Lit

type Page = Home | About | User of id: string | NotFound

let matchUser (result: URLPatternResult) =
    match result.pathname.groups.["id"] with
    | Some id -> User id
    | None -> NotFound

[<AttachMembers>]
type MyApp() as this =
    inherit LitElement()

    let router =
        [ "/", (fun _ -> Home)
          "/about", (fun _ -> About)
          "/users/:id", matchUser ]
        |> createRouter NotFound

    let routing = RouterController(this, router)

    override _.render() =
        match routing.route with
        | Home -> html $"<h1>Home</h1>"
        | About -> html $"<h1>About</h1>"
        | User id -> html $"<h1>User {id}</h1>"
        | NotFound -> html $"<h1>Not Found</h1>"
```

The `RouterController` handles `popstate` events, intercepts clicks on links that match one of its routes (plus hash links, with smooth scrolling), and manages `history.pushState` navigation automatically. A `urlpattern-polyfill` npm dependency is included for browsers without native support.

## Async Tasks

`Firelight.Task` binds Lit's [`@lit/task`](https://lit.dev/docs/data/task/) controller. The F# type is named `LitTask` to keep it distinct from `System.Threading.Tasks.Task`. Pass an argument array to run automatically when its values change, or set `autoRun = !^false` and call `run()` yourself.

```fsharp
open Fable.Core
open Fable.Core.JsInterop
open Firelight
open Firelight.Task

// Stands in for a real request, e.g. a fetch to your API. promise { } is Fable.Promise's.
let fetchProduct (id: string) : JS.Promise<string> =
    promise {
        do! Promise.sleep 500
        return $"Product {id}"
    }

[<AttachMembers>]
type ProductView() as this =
    inherit LitElement()

    let mutable productId = "123"

    let loadProduct (args: string[]) (_: TaskFunctionOptions) : TaskResult<string> =
        match args with
        | [| id |] -> !^(fetchProduct id)
        | _ -> initialState // args always holds one id

    let product =
        LitTask(
            this,
            TaskConfig(
                TaskFunction(loadProduct),
                args = (fun () -> [| productId |])
            )
        )

    override _.render() =
        product.render(
            StatusRenderer(
                pending = (fun () -> Lit.html $"<p>Loading...</p>"),
                complete = (fun name -> Lit.html $"<p>{name}</p>"),
                error = (fun error -> Lit.html $"<p>{error}</p>")
            )
        )
        |> unbox
```

The task exposes `status`, `value`, `error`, and `taskComplete`. Its task function receives an `AbortSignal` through `TaskFunctionOptions`, aborted when a newer run supersedes the current one. Its type is [Fable.Fetch](https://github.com/fable-compiler/fable-fetch)'s, which Firelight.Task depends on, so it passes straight to Fable.Fetch's `fetch`: `fetch url [ Signal options.signal ]`.

## Signals

`Firelight.Signals` binds [`@lit-labs/signals`](https://lit.dev/docs/data/signals/). Use `LitSignals.defineElement` to register a component with `SignalWatcher`. Reading a signal with `get()` during a render then schedules an update when its value changes. `watch` updates only its template part; the signal-aware `html` and `svg` tags apply `watch` to interpolated signals automatically.

```fsharp
open Firelight
open Firelight.Signals
open type LitSignals

let count = signal 0
let doubled = computed (fun () -> count.get() * 2)

type SignalCounter() =
    inherit LitElement()

    override _.render() =
        html $"""
            <p>Double: {doubled}</p>
            <button @click={fun _ -> count.set(count.get() + 1)}>Increment</button>
        """

defineElement<SignalCounter> "signal-counter"
```

`open type LitSignals` brings its signal-aware `html` and `svg` tags into scope. If you also use `open type Firelight.Lit`, open `LitSignals` afterward: its `html` and `svg` shadow Lit's tags. Call `Lit.html` explicitly when you want the regular tag, and use `watch count` inside it for a targeted update. `updateEffect` attaches an effect to an element registered with `defineElement` and returns a dispose function. This Lit Labs package and its signal polyfill are experimental; pin compatible versions when deploying it.

## Motion

`Firelight.Motion` binds [`@lit-labs/motion`](https://github.com/lit/lit/tree/main/packages/labs/motion). Use `Motion.animate()` in an element expression to animate layout changes between renders. `MotionOptions` supports timing, entry and exit keyframes, guards, IDs for transitions between elements, and callbacks. The package also exposes `AnimateController`, `SpringController`, and `SpringController2D`.

```fsharp
open Fable.Core
open Fable.Core.JsInterop
open Firelight
open Firelight.Motion

[<AttachMembers>]
type MovingBox() =
    inherit LitElement()

    let mutable shifted = false

    override this.render() =
        Lit.html $"""
            <button @click={fun _ -> shifted <- not shifted; this.requestUpdate()}>Move</button>
            <div class={if shifted then "shifted" else ""}
                 {Motion.animate(MotionOptions(keyframeOptions = MotionKeyframeOptions(duration = !^300.0), ``in`` = Motion.fade))}>
            </div>
        """
```

`MotionKeyframe.create` accepts arbitrary CSS properties for custom keyframes. The `animate` directive needs a Lit element expression, and its host must be a `LitElement`.

## Documentation

- [Official Lit Documentation](https://lit.dev/docs/) - for a complete API reference
- [Components and Templates](docs/components-and-templates.md) - architectural guide covering component patterns, template composition, and communication strategies
- [Styling Components](docs/styling.md) - getting global CSS (Tailwind, design systems, icon fonts) into shadow roots via constructed stylesheets
- [Elmish DevTools](docs/elmish-devtools.md) - persisting Elmish state to `localStorage` for better HMR development experience
- [Sample Projects](sample/README.md) - annotated examples from a basic tutorial to a full drag-and-drop Kanban board and multi-page routing
- [Observers and Virtualizer sample](sample/ObserversAndVirtualizer/) - resize observation and two ways to virtualize a large list

## Getting Started

See the [GettingStarted sample](sample/GettingStarted/) for a guided walkthrough covering reactive properties, styles, events, controllers, context, and Elmish - each concept in its own focused module.

---

## License

MIT
