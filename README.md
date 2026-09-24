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
| `Firelight.Router` | Client-side routing via the [URL Pattern API](https://developer.mozilla.org/en-US/docs/Web/API/URLPattern) |
| `Firelight.Signals` | Bindings to Lit Labs signals for shared reactive state and targeted template updates |
| `Firelight.Task` | Bindings to Lit's `@lit/task` reactive controller for async work |

## Quick Start

Define a component by inheriting `LitElement`, declare reactive properties, and implement `render`:

```fsharp
open Firelight
open Fable.Core.JsInterop

type Counter() =
    inherit LitElement()

    let mutable count = 0

    override _.render() =
        html $"""
            <p>Count: {count}</p>
            <button @click={fun _ -> count <- count + 1; base.requestUpdate()}>
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
open Firelight
open Firelight.Elmish

type Msg = Increment | Decrement

type Counter() =
    inherit LitElement()

    let elmish =
        ElmishController.simple(
            base,
            init = fun () -> 0,
            update = fun msg model ->
                match msg with
                | Increment -> model + 1
                | Decrement -> model - 1
        )

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
open Firelight.Context

// Define a typed context
let dispatchContext = LitContext.createContext<Symbol, Msg -> unit>()

// Provide it from a parent component
let provider = ContextProvider(host, dispatchContext, dispatch)

// Consume it in any descendant
let consumer = ContextConsumer(host, dispatchContext)
```

## Router

`Firelight.Router` provides client-side routing built on the browser's [URL Pattern API](https://developer.mozilla.org/en-US/docs/Web/API/URLPattern). Define routes as URL patterns with typed extractors, and use `RouterController` to wire routing into Lit's reactive lifecycle:

```fsharp
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

The `RouterController` handles `popstate` events, intercepts internal link clicks (including hash links with smooth scrolling), and manages `history.pushState` navigation automatically. A `urlpattern-polyfill` npm dependency is included for browsers without native support.

## Async Tasks

`Firelight.Task` binds Lit's [`@lit/task`](https://lit.dev/docs/data/task/) controller. The F# type is named `LitTask` to keep it distinct from `System.Threading.Tasks.Task`. Pass an argument array to run automatically when its values change, or set `autoRun = U2.Case1 false` and call `run()` yourself.

The example assumes `fetchProduct : string -> JS.Promise<string>`.

```fsharp
open Firelight
open Firelight.Task
open Fable.Core

[<AttachMembers>]
type ProductView() as this =
    inherit LitElement()

    let mutable productId = "123"

    let loadProduct (args: string[]) (_: TaskFunctionOptions) =
        U2.Case2 (fetchProduct args.[0])

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
            TaskRenderer(
                pending = (fun () -> Lit.html $"<p>Loading...</p>"),
                complete = (fun name -> Lit.html $"<p>{name}</p>"),
                error = (fun error -> Lit.html $"<p>{error}</p>")
            )
        )
        |> unbox
```

The task exposes `status`, `value`, `error`, and `taskComplete`. Its task function receives an `AbortSignal` through `TaskFunctionOptions`; pass that signal to cancellable work when a newer run supersedes the current one.

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

## Documentation

- [Official Lit Documentation](https://lit.dev/docs/) - for a complete API reference
- [Components and Templates](docs/components-and-templates.md) - architectural guide covering component patterns, template composition, and communication strategies
- [Styling Components](docs/styling.md) - getting global CSS (Tailwind, design systems, icon fonts) into shadow roots via constructed stylesheets
- [Elmish DevTools](docs/elmish-devtools.md) - persisting Elmish state to `localStorage` for better HMR development experience
- [Sample Projects](sample/README.md) - annotated examples from a basic tutorial to a full drag-and-drop Kanban board and multi-page routing

## Getting Started

See the [GettingStarted sample](sample/GettingStarted/) for a guided walkthrough covering reactive properties, styles, events, controllers, context, and Elmish - each concept in its own focused module.

---

## License

MIT
