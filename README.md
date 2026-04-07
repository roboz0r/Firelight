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

## Documentation

- [Official Lit Documentation](https://lit.dev/docs/) - for a complete API reference
- [Components and Templates](docs/components-and-templates.md) - architectural guide covering component patterns, template composition, and communication strategies
- [Elmish DevTools](docs/elmish-devtools.md) - persisting Elmish state to `localStorage` for better HMR development experience
- [Sample Projects](sample/README.md) - annotated examples from a basic tutorial to a full drag-and-drop Kanban board and multi-page routing

## Getting Started

See the [GettingStarted sample](sample/GettingStarted/) for a guided walkthrough covering reactive properties, styles, events, controllers, context, and Elmish - each concept in its own focused module.

---

## License

MIT
