# Components and Templates

Firelight apps are built from two distinct building blocks: **components** and **templates**. Understanding the difference shapes how you structure state, lifecycle, and composition.

---

## Components

A component is a class that inherits `LitElement` and is registered as a custom HTML element via `defineElement`. The browser tracks it as a live node in the DOM; Lit manages its shadow root, reactive properties, and update cycle.

```fsharp
[<AttachMembers>]
type MyCounter() =
    inherit LitElement()

    let mutable count = 0

    override this.render() =
        html $"""
        <button @click={fun _ -> count <- count + 1; this.requestUpdate()}>
            Clicked {count} times
        </button>"""

// Registered once at startup:
defineElement<MyCounter> "my-counter"
```

Components are the right choice when the piece of UI:

- Manages its own internal state
- Needs lifecycle hooks (`connectedCallback`, `disconnectedCallback`, etc.)
- Wraps a third-party library that requires a DOM node to attach to
- Should be opaque to its parent — exposing a clean property/event API
- Communicates outward via DOM events and inward via attributes or properties

### Components with an Elmish loop

When a component's internal state is non-trivial, pair it with a local Elmish loop. The `Model`, `Msg`, and `update` function live in the component's module; the component class owns the loop.

```fsharp
// TodoModel.fs — pure domain logic, no Lit dependency
type TodoState = { Items: TodoItem list }
type TodoMsg = AddTodo of string | ToggleTodo of int * bool | RemoveTodo of int

module TodoMsg =
    let update (msg: TodoMsg) (state: TodoState) = ...

// App.fs — the component owns the loop
[<AttachMembers>]
type TodoApp() as this =
    inherit LitElement()

    let mutable _state: TodoState = { Items = [] }

    member this.Dispatch(msg: TodoMsg) =
        _state <- TodoMsg.update msg _state
        this.requestUpdate()

    override this.render() =
        html $"""..."""
```

The `update` function is a pure transformation — it has no knowledge of Lit, the DOM, or rendering. The component class is the impure shell that drives the loop and triggers re-renders.

---

## Templates

A template is a plain function that returns an `HTMLTemplateResult`. It has no class, no lifecycle, and no state of its own. It receives everything it needs as arguments and produces a fragment to be rendered inside a component's shadow DOM.

```fsharp
// A template that renders a single todo item
let todoItem (item: TodoItem) (dispatch: TodoMsg -> unit) =
    html $"""
    <div class="item">
        <input type="checkbox" ?checked={item.Done}
            @click={fun _ -> dispatch (ToggleTodo(item.Id, not item.Done))} />
        <span>{item.Text}</span>
    </div>"""
```

Used inside a component's `render`:

```fsharp
override this.render() =
    html $"""
    <ul>
        {_state.Items |> List.map (fun item -> todoItem item this.Dispatch)}
    </ul>"""
```

Templates are the right choice when the piece of UI:

- Is a pure view of data passed to it
- Has no lifecycle requirements
- Does not need to maintain state between renders
- Is an internal implementation detail of a component

Templates correspond directly to the `view` function in classic Elmish. Breaking a large `render` method into named template functions is encouraged — it keeps render logic readable without the overhead of additional custom elements.

---

## Choosing between the two

| | Component | Template |
|---|---|---|
| State | Own internal state | Stateless — receives state as arguments |
| Lifecycle | `connectedCallback`, `disconnectedCallback`, etc. | None |
| DOM | Registered custom element with shadow root | Fragment rendered inside a parent's shadow root |
| Elmish | Gets its own `Model`/`Msg`/`update` loop | Pure `model -> dispatch -> HTMLTemplateResult` |
| Reuse | Usable from anywhere in the DOM | Used within one component's `render` |
| API surface | Properties in, events out | Arguments in, `HTMLTemplateResult` out |

When in doubt, start with a template. Promote to a component only when you need state, lifecycle, or a clean boundary that hides internal complexity from the rest of the program.

---

## Architectural patterns

### Pure Elmish — one component, templates all the way down

A single root component owns the entire application state. Every other piece of UI is a template function. This is the classic Elm Architecture applied to a web component shell.

```text
TodoApp (Component — owns state, Elmish loop)
  └── todoItem     (template function)
  └── todoInput    (template function)
  └── statusBar    (template function)
```

**Strengths:** Simple mental model, single source of truth, easy to test the `update` function in isolation.  
**Fits well for:** Apps where the UI surface is one cohesive unit, such as widgets, forms, or focused tools.

### Composite — multiple components, each with their own loop

Complex subsystems are encapsulated in their own components. Each component has its own Elmish loop and exposes a minimal property/event API. The parent treats child components as black boxes.

```text
AppShell (Component — routing, top-level auth state)
  └── TodoApp (Component — todo Elmish loop)
        └── TodoItemComponent (Component — subscribes to context, handles one item)
  └── SettingsPanel (Component — settings Elmish loop)
```

Communication between components uses DOM events (outward) and properties/attributes (inward), keeping the Elmish types of one component from leaking into another.

**Strengths:** Each component is independently testable and replaceable. Complexity is contained.  
**Fits well for:** Larger apps composed of distinct subsystems, or reusable components published for use outside a single application.

### Mixed — pragmatic middle ground

Most apps fall here. A small number of components own meaningful state and lifecycle; most UI is templates. The decision is driven by practical need, not a top-down rule.

```text
AppShell (Component)
  └── TodoApp (Component — complex enough to warrant its own loop)
        └── todoItem (template — simple enough to stay a function)
        └── todoEmpty (template — static, no state needed)
  └── ClockWidget (Component — needs setInterval lifecycle)
  └── navLink (template — pure view of route state)
```

---

## Communication between components

When components need to talk to each other, use the patterns the web platform provides.

### Parent to child — properties and attributes

The parent sets properties on the child element. The child declares them with `PropertyDeclarations` and reacts when they change.

```fsharp
// Parent render:
html $"<todo-item todo-id={item.Id}></todo-item>"

// Child declaration:
static member properties =
    PropertyDeclarations.create [
        "todoId", PropertyDeclaration<int>(attribute = !^"todo-id")
    ]
```

### Child to parent — DOM events

The child dispatches a `CustomEvent`; the parent listens with `@event-name`.

```fsharp
// Child:
this.dispatchEvent(CustomEvent.create "todo-completed" {| detail = {| id = _todoId |} |})

// Parent render:
html $"<todo-item @todo-completed={fun e -> this.Dispatch (Complete e.detail.id)}></todo-item>"
```

### Across the tree — context

Use `@lit/context` (via `Firelight.Context`) when data needs to be available to deeply nested components without threading it through every level. The Todo sample demonstrates this: `TodoApp` provides both `state` and `dispatch` via context, and `TodoItemComponent` consumes them directly.

```fsharp
// Provider (ancestor component):
let stateProvider = ContextProvider(jsThis, ContextProvider.Options(Context.stateCtx, _state))

// Consumer (descendant component — no props required):
let stateConsumer =
    ContextConsumer(jsThis, ContextConsumer.Options(Context.stateCtx, subscribe = true))
```

---

## Summary

- **Component** = `LitElement` subclass + `defineElement`. Owns state, lifecycle, and a shadow root. In Elmish usage, owns a `Model`/`Msg`/`update` loop.
- **Template** = plain function returning `HTMLTemplateResult`. Stateless, lifecycle-free. Pure view of its arguments.
- Prefer templates for simplicity. Promote to a component when you need state, lifecycle, or encapsulation.
- Components communicate via properties (in), events (out), and context (across the tree).
