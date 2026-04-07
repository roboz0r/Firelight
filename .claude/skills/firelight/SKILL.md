---
name: firelight
description: Guide for building web UIs with the Firelight F# Lit bindings library. Covers components, templates, Elmish integration, context, routing, communication patterns, and architectural decisions.
trigger: When the user asks to build, modify, or architect UI using Firelight, Lit, F# web components, Fable with Lit bindings, or client-side routing.
---

# Firelight Development Guide

Firelight provides F# bindings for [Lit](https://lit.dev/) 3.x web components via [Fable](https://fable.io/). It is organized as three NuGet packages:

| Package | Purpose |
|---|---|
| **Firelight** | Core Lit bindings: `html`, `css`, `LitElement`, directives, reactive properties |
| **Firelight.Context** | `@lit/context` bindings: `ContextProvider`, `ContextConsumer` |
| **Firelight.Elmish** | Elmish integration: `ElmishController`, `DevTools` |
| **Firelight.Router** | Client-side routing via the URL Pattern API: `Router`, `RouterController` |

NPM peer dependencies: `lit` (3.x), `@lit/context` (1.x), `urlpattern-polyfill` (10.x, for `Firelight.Router`).

All F# code compiles to JavaScript via Fable. The output runs in the browser as standard Web Components.

---

## Core Concept: Components vs Templates

Every piece of Firelight UI is either a **Component** or a **Template**. This is the most important architectural decision.

### Component

A class inheriting `LitElement`, registered as a custom HTML element. Has its own shadow DOM, lifecycle, and state.

```fsharp
open Fable.Core
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type SimpleGreeting() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [ "name", PropertyDeclaration<string>() ]

    static member styles =
        css $""":host {{ color: red; font-weight: bold; }}"""

    member val name = "World" with get, set

    override this.render() =
        html $"""<div><h1>Hello, {this.name}!</h1></div>"""

// Register once at startup:
defineElement<SimpleGreeting> "simple-greeting"
```

Use a Component when the UI:
- Manages its own internal state
- Needs lifecycle hooks (`connectedCallback`, `disconnectedCallback`, `firstUpdated`, `updated`)
- Wraps a third-party library that needs a DOM node
- Should be opaque to its parent with a clean property/event API
- Needs style isolation via Shadow DOM

### Template

A plain function returning `HTMLTemplateResult`. Stateless, no lifecycle, no shadow DOM.

```fsharp
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

Use a Template when the UI:
- Is a pure view of data passed as arguments
- Has no lifecycle or state requirements
- Is an internal implementation detail of a component

**Default to templates. Promote to a component only when you need state, lifecycle, or encapsulation.**

---

## Defining a Component

Every component requires these elements:

### 1. `[<AttachMembers>]` attribute
Required by Fable so instance members are attached to the JS prototype, which Lit's property system expects.

### 2. `inherit LitElement()`
Base class providing the Lit reactive update cycle and shadow DOM.

### 3. `defineElement<'T> "tag-name"`
Registers the component in the browser's Custom Element Registry. Call once at startup.

### 4. Reactive Properties (optional)
Declare with `static member properties`. Use `PropertyDeclaration<'T>()` for type-safe property options.

```fsharp
static member properties =
    PropertyDeclarations.create [
        "name", PropertyDeclaration<string>()
        "todoId", PropertyDeclaration<int>(attribute = !^"todo-id")
        "myData", PropertyDeclaration<string>(attribute = !^false)  // property-only, no attribute
    ]
```

Property options:
- `attribute` — `!^"attr-name"` for custom attribute name, `!^false` to disable attribute reflection
- `reflect` — `true` to reflect property value back to the attribute
- `state` — `true` for internal reactive state (not exposed as attribute)
- `hasChanged` — custom change detection function `('T -> 'T -> bool)`
- `useDefault` — `true` to use the default value during initialization

Define the backing member explicitly for type-checked access and default values:

```fsharp
member val name = "World" with get, set
```

### 5. Styles (optional)
Use `static member styles` with the `css` tagged template. Shadow DOM isolates these styles.

```fsharp
// Use $$""" (interpolated verbatim with $$ prefix) so { and } are literal CSS braces.
// Only {{}}, i.e. doubled braces, trigger F# interpolation.
static member styles =
    [| css $$"""
    :host { display: block; }
    .counter { display: flex; gap: 0.5rem; }
    button { border: 1px solid #d4d4d8; border-radius: 0.375rem; }
    button:hover { background: #f4f4f5; }
    """ |]
```

CSS composition — interpolating a `CSSResult` into another `css` tag:

```fsharp
module Color =
    let red = css $"red"

static member styles =
    css $""":host {{ color: {Color.red}; }}"""
```

### 6. `render()` override
Return an `HTMLTemplateResult` using the `html` tagged template.

```fsharp
override this.render() =
    html $"""<div>{this.name}</div>"""
```

### 7. Lifecycle (optional)
Available overrides: `connectedCallback()`, `disconnectedCallback()`, `firstUpdated(changedProperties)`, `updated(changedProperties)`, `willUpdate(changedProperties)`.

---

## Event Handling

### Basic event binding

Use `@event-name={handler}` in templates:

```fsharp
html $"""<button @click={fun _ -> count <- count + 1; this.requestUpdate()}>Click</button>"""
```

### LitEventListener with options

```fsharp
let listener =
    LitEventListener(
        (fun (ev: Event) -> console.log ($"Clicked: {ev.``type``}")),
        once = false
    )

override this.render() =
    html $"""<button @click={listener}>Advanced</button>"""
```

### Custom events (child to parent)

Firelight provides an inline helper `Event.customEvent` that sets `bubbles = true` and `composed = true` by default (the right defaults for Web Components crossing shadow DOM). Uses `CustomEvent<'T>` from `Fable.Browser.Event` where `detail` is `'T option`.

```fsharp
// In the child component — dispatch with the helper:
this.dispatchEvent(Event.customEvent("todo-completed", _todoId))

// Parent listens — detail is 'T option:
html $"""<todo-item @todo-completed={fun (e: CustomEvent<int>) ->
    e.detail |> Option.iter (fun id -> this.Dispatch (Complete id))}>
</todo-item>"""
```

Helper signature:
```fsharp
Event.customEvent(typeName, detail, ?bubbles (* default true *), ?composed (* default true *))
```

---

## Elmish Integration

`Firelight.Elmish` provides `ElmishController` — a `ReactiveController` that wires an Elmish dispatch loop to a `LitElement` host.

### Simple loop (no Cmds)

```fsharp
open Elmish
open Firelight.Elmish

type CounterModel = { Count: int }
type CounterMsg = Increment | Decrement | Reset | SetStart of int

module Counter =
    let init () = { Count = 0 }
    let update msg model =
        match msg with
        | Increment  -> { model with Count = model.Count + 1 }
        | Decrement  -> { model with Count = model.Count - 1 }
        | Reset      -> { Count = 0 }
        | SetStart n -> { Count = n }

[<AttachMembers>]
type Counter() as this =
    inherit LitElement()

    let elmish = ElmishController.simple this Counter.init Counter.update

    override _.render() =
        let model = elmish.model
        html $"""
        <div class="counter">
            <button @click={fun _ -> elmish.dispatch Decrement}>-</button>
            <span>{model.Count}</span>
            <button @click={fun _ -> elmish.dispatch Increment}>+</button>
        </div>"""
```

### Loop with Cmds

```fsharp
let elmish = ElmishController.withCmds this MyModule.init MyModule.update
```

Where `init : unit -> 'Model * Cmd<'Msg>` and `update : 'Msg -> 'Model -> 'Model * Cmd<'Msg>`.

### Advanced — full Elmish Program pipeline

```fsharp
let elmish =
    Program.mkSimple Counter.init Counter.update (fun _ _ -> ())
    |> Program.withConsoleTrace
    |> DevTools.withLocalStorage DevTools.DefaultDelay "my-key-v1" encode decode
    |> fun p -> ElmishController(this, p)
```

### Key behaviors
- `init()` fires synchronously in the constructor — `elmish.model` is always valid before `render()`
- Properties set externally should dispatch messages (the loop is already running by the time setters fire)
- While disconnected from DOM, state updates are applied silently; `requestUpdate()` fires on reconnection
- Lit batches `requestUpdate()` calls, so multiple rapid dispatches still cause only one render

### External properties feeding into Elmish

```fsharp
let mutable _start = 0

static member properties =
    PropertyDeclarations.create [ "start", PropertyDeclaration<int>() ]

member _.start
    with get () = _start
    and set v =
        _start <- v
        elmish.dispatch (SetStart v)
```

### DevTools — localStorage persistence for HMR

```fsharp
open Thoth.Json

let encode (model: MyModel) : string =
    Encode.Auto.toString (0, model)

let decode (json: string) : MyModel option =
    match Decode.Auto.fromString<MyModel> (json) with
    | Ok model -> Some model
    | Error _ -> None

let elmish =
    Program.mkSimple init update (fun _ _ -> ())
    |> DevTools.withLocalStorage DevTools.DefaultDelay "my-component-v1" encode decode
    |> fun p -> ElmishController(this, p)
```

**Always use Thoth.Json (or another proper JSON library) for encode/decode.** Do not use `JSON.stringify`/`JSON.parse :?>` — Fable DUs, Maps, and nested records do not survive a naive JSON round-trip. `Thoth.Json` handles these correctly via its Auto API.

Use `#if DEBUG` / `#endif` to strip in production. Bump the key suffix (`-v1` to `-v2`) when the model shape changes.

---

## Communication Patterns

### Parent to Child — Properties and Attributes

The parent sets properties on the child element. The child declares them with `PropertyDeclarations`.

```fsharp
// Parent render:
html $"<todo-item todo-id={item.Id}></todo-item>"

// Child declaration:
static member properties =
    PropertyDeclarations.create [
        "todoId", PropertyDeclaration<int>(attribute = !^"todo-id")
    ]
```

### Child to Parent — DOM Events

The child dispatches a custom event; the parent listens with `@event-name`.

Use Firelight's `Event.customEvent` helper, which defaults `bubbles = true` and `composed = true` so the event automatically pierces shadow DOM boundaries. Uses `CustomEvent<'T>` from `Fable.Browser.Event` where `detail` is `'T option`.

```fsharp
// Child dispatches:
this.dispatchEvent(Event.customEvent("card-deleted", detail = 42))

// Parent listens:
html $"""
<my-card @card-deleted={fun (e: CustomEvent<int>) ->
    e.detail |> Option.iter (fun id -> elmish.dispatch (DeleteCard id))}>
</my-card>"""
```

### Across the Tree — Lit Context

Use `Firelight.Context` for data that must reach deeply nested components without prop-drilling (themes, auth state, dispatch functions).

**Define the context type and key:**

The context type must inherit both `Context<'ValueType>` and `symbol`:

```fsharp
open Fable.Core
open Firelight.Context

// The context type brands a symbol key with its value type.
// Must inherit both Context<'T> and symbol.
type MyStateContext =
    inherit Context<MyState>
    inherit symbol

module Context =
    let stateCtx: MyStateContext = LitContext.createContext (JS.Symbol())
```

A convenience alias can reduce boilerplate when defining multiple contexts:

```fsharp
// Optional helper (not part of the library):
type LitContext<'T> =
    inherit Context<'T>
    inherit symbol
```

**Provider (ancestor component):**

```fsharp
let stateProvider =
    ContextProvider(jsThis, ContextProvider.Options(Context.stateCtx, initialState))

// Update when state changes:
stateProvider.setValue newState
```

**Consumer (descendant component):**

```fsharp
let stateConsumer =
    ContextConsumer(
        jsThis,
        ContextConsumer.Options(
            Context.stateCtx,
            (fun state _ -> _localState <- state),  // optional callback
            subscribe = true                          // re-render on changes
        )
    )

// Access in render:
let currentState = stateConsumer.value
```


---

## Reactive Controllers

Controllers share stateful logic across components without inheritance. Implement `ReactiveController` and attach to a host.

```fsharp
type ClockController(host: ClockHost) as this =
    let mutable nonce = 1
    let mutable value = DateTime.Now
    do host.addController this

    member _.Value = value

    interface ReactiveController with
        member _.hostConnected() =
            let i = nonce
            async {
                while i = nonce do
                    do! Async.Sleep host.TickRate
                    value <- DateTime.Now
                    host.requestUpdate ()
            } |> Async.StartImmediate
        member _.hostDisconnected() = nonce <- nonce + 1
        member _.hostUpdate() = ()
        member _.hostUpdated() = ()
```

The nonce pattern ensures the async loop stops cleanly when the host disconnects.

---

## Client-Side Routing

`Firelight.Router` provides client-side routing built on the browser's [URL Pattern API](https://developer.mozilla.org/en-US/docs/Web/API/URLPattern). It consists of three files:

- **`URLPattern.fs`** — F# bindings for the `URLPattern` Web API (types in the `Browser.Types.URLPattern` namespace)
- **`Router.fs`** — `Router<'Route>` type for matching URLs to routes, plus `createRouter` / `createRouterWithBaseUrl` helpers, click/popstate event handlers, and hash-link smooth scrolling
- **`RouterController.fs`** — `RouterController<'Route>`, a `ReactiveController` that wires routing into Lit's lifecycle

### Defining routes

Routes are defined as a list of `(pattern, extractor)` tuples. The pattern is a [URL Pattern string](https://developer.mozilla.org/en-US/docs/Web/API/URL_Pattern_API). The extractor receives a `URLPatternResult` and returns your route union:

```fsharp
open Browser.Types.URLPattern
open Firelight.Router

type Page = Home | About | User of id: string | NotFound

let matchUser (result: URLPatternResult) : Page =
    match result.pathname.groups.["id"] with
    | Some id -> User id
    | None -> NotFound

let router =
    [ "/", (fun _ -> Home)
      "/:page?", (fun r ->
          match r.pathname.groups.["page"] with
          | Some "about" -> About
          | _ -> NotFound)
      "/users/:id", matchUser ]
    |> createRouter NotFound
```

`createRouter` uses `window.location.origin` as the base URL. Use `createRouterWithBaseUrl` for a custom base.

### RouterController

`RouterController` is a `ReactiveController` that:
- Initializes `route` from the current URL on construction
- Listens for `popstate` events (browser back/forward)
- Intercepts internal link clicks, calls `history.pushState`, and updates the route
- Handles hash links (`#section`) with smooth scrolling, piercing shadow DOM boundaries
- Cleans up event listeners on disconnect

```fsharp
open Firelight
open Firelight.Router
open type Firelight.Lit

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

### Domain model separation

Follow the same pattern as Elmish — define the route union and match functions in a separate model file:

```fsharp
// MultiPageModel.fs
namespace MultiPage

open Browser.Types.URLPattern

type Page = Home | About | User of id: string | NotFound

module MultiPageModel =
    let matchRoute (result: URLPatternResult) : Page =
        match result.pathname.groups.["page"] with
        | Some "about" -> About
        | _ -> NotFound

    let matchUser (result: URLPatternResult) : Page =
        match result.pathname.groups.["id"] with
        | Some id -> User id
        | None -> NotFound
```

### URLPattern polyfill

The `Firelight.Router` package includes an npm dependency on `urlpattern-polyfill` for browsers without native `URLPattern` support. Import the polyfill at app startup:

```fsharp
open Browser.Types.URLPattern

// Call once at the top level of your app module:
importPolyfill ()
```

### Key behaviors

- The `Router.Match` method always returns a value (falls back to the `notFound` route). Use `Router.TryMatch` for `Option<'Route>`
- Internal link detection skips: links with `target`, `download`, `rel="external"`, different origins, `mailto:`, `javascript:`, and `tel:` URLs
- Hash links trigger smooth scrolling and `history.replaceState` (no route change)
- `RouterController` calls `host.requestUpdate()` after route changes, integrating with Lit's batched rendering

---

## Directives Quick Reference

All accessed via `open type Firelight.Lit`:

| Directive | Purpose | Example |
|---|---|---|
| `classMap` | Dynamic CSS classes | `classMap (ClassInfo.create ["active", isActive; "hidden", isHidden])` — **each key must be a single class name, no spaces** |
| `styleMap` | Dynamic inline styles | `styleMap (StyleInfo.create ["color", color; "font-size", size])` |
| `repeat` | Keyed list rendering | `repeat (items, (fun item _ -> item.Id), fun item i -> renderItem item i)` |
| `ifDefined` | Conditional attribute | `ifDefined (someOption)` |
| `cache` | Cache template DOM | `cache (Some templateResult)` |
| `keyed` | Force re-render on key change | `keyed (Some key, Some value)` |
| `guard` | Re-render only on dependency change | `guard (deps, fun () -> expensiveRender())` |
| `ref` / `createRef` | DOM element reference | `{ref myRef}` in template, `createRef<HTMLInputElement>()` |
| `join` | Interleave items with separator | `join (items, html $"<hr/>")` |

### classMap — single class names only

**`classMap` keys must each be a single CSS class name with no spaces.** This is a direct wrapper over the Lit directive, which calls `classList.add(key)` / `classList.remove(key)` — the DOM API throws `InvalidCharacterError` if the token contains spaces.

```fsharp
// WRONG — will throw at runtime:
classMap (ClassInfo.create ["bg-blue-500 text-white font-bold", true])

// CORRECT — one class per entry:
classMap (ClassInfo.create ["bg-blue-500", true; "text-white", true; "font-bold", true])
```

This is especially easy to get wrong with Tailwind, where you naturally group utility classes. When you need conditional groups of space-separated classes, build a class string instead:

```fsharp
let classes (pairs: (string * bool) list) =
    pairs
    |> List.choose (fun (cls, cond) -> if cond then Some cls else None)
    |> String.concat " "

// Use as a plain class attribute:
html $"""<div class={classes ["bg-slate-50 rounded-xl p-3", true; "ring-2 ring-blue-400", isActive]}></div>"""
```

### Attribute vs Property binding

Lit distinguishes between attribute and property bindings in templates:

- **`value={x}` (no dot)** — Binds to the HTML **attribute**. Always converted to a **string**. Represents the element's *initial* state. Lit only sets it when the value changes between renders, but the browser treats attributes as initial values — user edits to form inputs are not overwritten.
- **`.value={x}` (with dot)** — Binds to the JavaScript DOM **property**. Can pass **any data type** (objects, arrays, numbers, booleans) and controls the element's *live, current* state. **On every re-render, Lit writes to the DOM property, overwriting any user input.**

For form inputs where the user types text, prefer `value={x}` (attribute) for the initial value and read the live value from a `ref` on save. Use `.value={x}` (property) only when you need to programmatically control the input's current state on every render.

### Ref usage

```fsharp
let inputRef = createRef<HTMLInputElement>()

override this.render() =
    html $"""<input {ref inputRef} type="text" />"""

// Access later:
match inputRef.value with
| Some input -> input.value
| None -> ""
```

---

## Builders

### Renderable builder

Compose multiple renderables:

```fsharp
renderable {
    html $"<h1>Title</h1>"
    html $"<p>Body</p>"
}
```

### CSS builder

Compose CSS result groups:

```fsharp
cssResultGroup {
    css $":host { display: block; }"
    css $".item { padding: 1rem; }"
}
```

---

## Architectural Patterns

### Pure Elmish — one component, templates all the way down

A single root component owns all state. Every other piece of UI is a template function.

```
TodoApp (Component — owns Elmish loop)
  +-- todoItem   (template)
  +-- todoInput  (template)
  +-- statusBar  (template)
```

Best for: widgets, forms, focused tools with one cohesive UI surface.

### Composite — multiple components, each with their own loop

Complex subsystems get their own components with separate Elmish loops. Communication via properties (in) and DOM events (out).

```
AppShell (Component — routing, auth)
  +-- TodoApp (Component — todo Elmish loop)
  +-- SettingsPanel (Component — settings Elmish loop)
```

Best for: larger apps with distinct subsystems, reusable components.

### Mixed — pragmatic middle ground (most common)

A few components own state; most UI is templates. Decided by practical need.

```
AppShell (Component)
  +-- TodoApp (Component — complex state)
  |     +-- todoItem  (template — simple view)
  |     +-- todoEmpty (template — static)
  +-- ClockWidget (Component — needs setInterval lifecycle)
  +-- navLink (template — pure view)
```

---

## Using Third-Party Web Component Libraries

<!-- aspirational: pattern from brainstorming, not yet demonstrated in samples -->

Because Lit compiles to standard Web Components, any web component library works directly in Firelight templates with zero wrapper code. Just load the library's JS/CSS and use the tags:

```fsharp
// Using Web Awesome (formerly Shoelace) components directly:
html $"""
<div>
    <wa-button variant="primary" @click={fun _ -> dispatch OpenModal}>
        Create Item
    </wa-button>
    <wa-dialog label="New Item" ?open={model.IsModalOpen}
        @wa-request-close={fun _ -> dispatch CloseModal}>
        <wa-input label="Title"
            @wa-input={fun (e: CustomEvent) -> dispatch (UpdateTitle e.target?value)}>
        </wa-input>
        <wa-button slot="footer" variant="primary" @click={fun _ -> dispatch Save}>
            Save
        </wa-button>
    </wa-dialog>
</div>"""
```

Recommended libraries (all built on Web Components):
- **Web Awesome** (formerly Shoelace) — comprehensive, Lit-based, actively maintained
- **Fluent UI Web Components** (Microsoft)
- **Carbon Web Components** (IBM)
- **Lion** (ING Bank) — headless/accessible

Swapping libraries is trivial: change the HTML tags in templates. F# business logic is unaffected.

---

## Integrating Imperative JS Libraries

<!-- aspirational: pattern from brainstorming, not yet demonstrated in samples -->

For libraries like Three.js, D3, Leaflet, or Mapbox that need imperative control of a DOM node, use the "Canvas Wrapper" component pattern:

1. Render a container element (e.g. `<canvas>`) in `render()`
2. Use `firstUpdated` lifecycle to query the element and hand it to the JS library
3. Use `updated` lifecycle to push Elmish state changes into the library imperatively

```fsharp
[<AttachMembers>]
type ThreeScene() as this =
    inherit LitElement()

    override _.render() =
        html $"""<canvas id="gl-canvas" style="width:100%; height:100%;"></canvas>"""

    override _.firstUpdated(_) =
        let canvas = this.renderRoot.querySelector("#gl-canvas")
        // Initialize Three.js with the canvas...
        // Start the render loop...

    override _.updated(_) =
        // Push any Elmish state changes to the JS library imperatively
        ()
```

Lit leaves the canvas DOM node untouched between renders (the template hasn't changed), so the JS library's internal state is never disrupted.

---

## Architecture Rules

### No SSR

Firelight is client-side SPA only. Do not attempt server-side rendering. Reasons:
- Lit SSR requires Node.js; F# backends use .NET
- Elmish `Program.runWith` assumes client-side execution
- The entire Fable/Elmish ecosystem is designed for CSR

### App Shell Pattern for Fast Load

<!-- aspirational: pattern from brainstorming, not yet demonstrated in samples -->

Serve minimal HTML with an inline loading screen. Fetch the JS bundle and initial API data in parallel:

```html
<script>
    window.__INITIAL_STATE__ = fetch('/api/data')
        .then(r => r.json().then(data => ({ success: true, value: data })))
        .catch(err => ({ success: false, error: err.message }));
</script>
<script defer src="/dist/bundle.js"></script>
<my-app>
    <div class="loader">Loading...</div>
</my-app>
```

The promise resolves to a discriminated shape that maps cleanly to F# on the consumer side, preserving error details rather than collapsing failures to `null`.

The component renders `<slot></slot>` while loading, showing the light-DOM loader until data arrives.

### F# Immutability Guarantees Correct Reactivity

F# records are immutable. `{ model with Count = model.Count + 1 }` always creates a new object reference. This guarantees Lit's dirty-checking detects every state change — no stale renders.

### Prefer Templates, Promote When Needed

Start with template functions. Only promote to a component when you need:
- Own state or Elmish loop
- Lifecycle hooks
- Shadow DOM style isolation
- A reusable custom element tag

### Keep Elmish Pure

The `update` function should be a pure transformation with no knowledge of Lit, the DOM, or rendering. The component class is the impure shell that drives the loop.

### Domain Model Separate from Component

Define `Model`, `Msg`, and `update` in their own module (e.g. `TodoModel.fs`), separate from the component file. This keeps domain logic testable and independent of the rendering layer.

---

## Common Imports

```fsharp
open Fable.Core
open Fable.Core.JsInterop
open Browser
open Browser.Types
open Firelight
open type Firelight.Lit          // brings html, css, svg, defineElement, directives into scope

// For context:
open Firelight.Context

// For Elmish:
open Elmish
open Firelight.Elmish
```

---

## Anti-Patterns

- **Don't make every template a component.** Components have overhead (shadow DOM, registry, lifecycle). Use templates for pure view functions.
- **Don't use Elmish Cmds for DOM event communication.** Use native `CustomEvent` for child-to-parent communication. Elmish Cmds are for side-effects (API calls, timers), not DOM plumbing.
- **Don't prop-drill through many layers.** Use Lit Context for data that must reach deeply nested components.
- **Don't fight the DOM.** Lit is a thin wrapper over native browser APIs. Use standard Web Component patterns, not framework-specific abstractions.
- **Don't put non-serializable values in the Elmish model.** API clients, DOM refs, and functions belong in controllers or context, not in model state.
- **Don't forget `[<AttachMembers>]`.** Without it, Fable won't attach members to the JS prototype and Lit's property system won't find them.
- **Don't use `mutable` for UI state in template functions.** Mutating a local variable does not trigger a Lit re-render — the template has already returned and Lit will not call the function again. UI state must live in the Elmish model (dispatch a message) or in a component's reactive property (call `requestUpdate()`). The only safe use of `mutable` inside a template function is for values that don't affect rendering (e.g. accumulating a result before the template is built).
