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

## Documentation

- [Official Lit Documentation](https://lit.dev/docs/) - for a complete API reference
- [Components and Templates](docs/components-and-templates.md) - architectural guide covering component patterns, template composition, and communication strategies
- [Elmish DevTools](docs/elmish-devtools.md) - persisting Elmish state to `localStorage` for better HMR development experience
- [Sample Projects](sample/README.md) - annotated examples from a basic tutorial to a full drag-and-drop Kanban board

## Getting Started

See the [GettingStarted sample](sample/GettingStarted/) for a guided walkthrough covering reactive properties, styles, events, controllers, context, and Elmish - each concept in its own focused module.

---

## License

MIT
