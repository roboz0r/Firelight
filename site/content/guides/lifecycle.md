---
title: Lifecycle
tagline: connect, update and clean up
description: "Override Lit's lifecycle methods in F#: connectedCallback and disconnectedCallback, the update cycle, requestUpdate, and where DOM access, subscriptions and cleanup belong."
section: guides
order: 50
summary: "Connecting, updating and cleaning up: which lifecycle method to override for what"
lead: "Override a component's lifecycle methods to run code when it joins a page, each time it updates and when it leaves. Lit calls them; this guide covers which one to use for what: DOM access, subscriptions, cleanup, and changes Lit can't see for itself."
links:
  - text: "Lit docs: Lifecycle"
    href: https://lit.dev/docs/components/lifecycle/
  - text: "Lit docs: Reactive update cycle"
    href: https://lit.dev/docs/components/lifecycle/#reactive-update-cycle
toc: true
---

Most components only override `render`. The other lifecycle methods are for work a template can't
do: listening to the window, starting a timer, focusing an input, handing an element to a
JavaScript library. Each is a member of `LitElement`, so you `override` it in your class, and the
compiler checks its signature.

## Watch the lifecycle

The Count button is a component that logs every lifecycle method it overrides; the rest is the
demo around it. The log starts with what ran as the page loaded. Click the count and read the
cycle that follows. Tick Hold updates and click the count again: `shouldUpdate` returns `false`,
and the button keeps its old count. Untick it, then remove the component and put it back.

::: example Snippets/LifecycleLog.fs
<my-lifecycle-demo></my-lifecycle-demo>
:::

Putting the component back logs `constructor` again. The template creates a new element, so
nothing carries over from the old one. Lit SSR also ran the constructor, `willUpdate` and `render`
when this page was built, to prerender the demo; the log only shows what ran in your browser.
See [Prerendering components at build time](/guides/prerendering/).

## Connected and disconnected

`connectedCallback` runs when the element joins a document, and `disconnectedCallback` when it
leaves. Set up anything outside the component there, such as a listener on `window` or a timer,
and undo it on the way out. An element can be removed and added again, when a list moves it for
example, so set up in `connectedCallback`, not in the constructor.

```fsharp
open Browser
open Browser.Types
open Fable.Core
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type LastKey() =
    inherit LitElement()

    let mutable stopListening = ignore

    static member properties =
        PropertyDeclarations.create [ "key", PropertyDeclaration<string>(state = true) ]

    member val key = "none yet" with get, set

    override this.connectedCallback() =
        base.connectedCallback ()
        stopListening <- Ev.listen window "keydown" (Ev.keyboard (fun e -> this.key <- e.key))

    override this.disconnectedCallback() =
        base.disconnectedCallback ()
        stopListening ()

    override this.render() =
        html $"""<p>Last key pressed: {this.key}</p>"""
```

Call the base method first in both. Lit's `connectedCallback` creates the shadow root and lets
updates start, so a component that skips it never renders. Lit's `disconnectedCallback` tells the
component's [controllers](/guides/controllers/) it has gone.

`Ev.listen` adds the listener and returns the function that removes it, so the cleanup stays next
to the setup. Removing a listener yourself is harder than it looks in F#: `removeEventListener`
only removes the very function `addEventListener` was given, and [Fable](https://fable.io/)
compiles a method such as `this.OnKeyDown`, and a function bound with `let` in the class, to a
member, so passing either one as a value wraps it in a new JavaScript function each time. Removing
it then removes nothing, and the window keeps the element alive. The [Events
guide](/guides/events/#listen-outside-the-component) has more.

## The update cycle

An update starts with a request: assigning a reactive property, or calling `requestUpdate`. Lit
waits until the current code has finished, then runs one update for every change made so far, so
three assignments in one click handler make one update. These are the methods it calls, in order:

| Method | Use it to | Runs on the server |
|---|---|---|
| `shouldUpdate changed` | Skip this update, by returning `false` | No |
| `willUpdate changed` | Compute values from properties before rendering | Yes |
| `update changed` | Render; override only to wrap it, and call the base | No |
| `render ()` | Return the template | Yes |
| `firstUpdated changed` | Do one-time work on the rendered DOM | No |
| `updated changed` | Do DOM work after each change | No |

After `updated`, the element's `updateComplete` promise resolves. The last column matters only
when the component is prerendered.

`changed` is a `Dictionary<string, obj>`: Lit's `changedProperties`, a JavaScript `Map` at runtime.
Its keys are the names of the properties that changed, and its values their *old* values, as
`obj`. Test for a key with `changed.ContainsKey "count"`, and unbox an old value with
`unbox<int> changed["count"]`. On an element's first update in the browser, every reactive
property with a value is in it. A property that changed while `shouldUpdate` returned `false` is
not in the next update's map, as the demo shows when you untick Hold updates.

### Compute values in willUpdate

`willUpdate` is the place for values derived from properties, such as a sorted list. Assigning a
property there joins the current update instead of starting another:

```fsharp
open Fable.Core
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type Leaderboard() =
    inherit LitElement()

    let mutable ranked: (string * int) list = []

    static member properties =
        PropertyDeclarations.create [ "scores", PropertyDeclaration<Map<string, int>>() ]

    member val scores: Map<string, int> = Map.empty with get, set

    override this.willUpdate(changed) =
        if not this.hasUpdated || changed.ContainsKey "scores" then
            ranked <- this.scores |> Map.toList |> List.sortByDescending snd

    override this.render() =
        html $"""<ol>{ranked |> List.map (fun (name, score) -> html $"<li>{name}: {score}</li>")}</ol>"""
```

The `not this.hasUpdated` covers the first render on the server, where `changed` leaves out a
property that still has the value its `member val` gave it. Assigning a property in `updated` instead works, but costs a second
update every time, and Lit's development build warns: "scheduled an update … after an update
completed".

### Reach the DOM after it renders

Before the first update there is no rendered DOM, so the constructor and `render` can't use it,
and on the server they run without a DOM at all. Use `firstUpdated` for one-time work, such as
handing an element to a JavaScript library, and `updated` for work that follows each change.
Reach elements with a [`ref`](/guides/templates/#directives) rather than a query.

When code changes a property and then needs the result in the DOM, await `updateComplete`. This
panel focuses its search box once the box is there:

```fsharp
open Browser.Types
open Fable.Core
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type SearchPanel() =
    inherit LitElement()

    let field = createRef<HTMLInputElement> ()

    static member properties =
        PropertyDeclarations.create [ "isOpen", PropertyDeclaration<bool>(state = true) ]

    member val isOpen = false with get, set

    member this.Open() =
        this.isOpen <- true

        async {
            let! _ = this.updateComplete |> Async.AwaitPromise
            field.value |> Option.iter (fun input -> input.focus ())
        }
        |> Async.StartImmediate

    override this.render() =
        if this.isOpen then
            html $"""<input type="search" {ref field}>"""
        else
            html $"""<button @click={fun _ -> this.Open()}>Search</button>"""
```

`updateComplete` waits for this element's update only, not for components inside it, and it
resolves even when `shouldUpdate` skipped the render.

## When Lit can't see a change

Lit starts an update when a reactive property is assigned a different value. It can't see a
`let mutable` field change, a value change in place (see the
[Templates guide](/guides/templates/#common-mistakes)), or an object that a JavaScript library
updates. After any of these, call `this.requestUpdate ()`. It's the same request a property
assignment makes, and requests made before the update runs share it. The Elmish and Task
controllers call it for you when their state changes.

Prefer reactive properties for state the component renders, with `state = true` for private
state. Use a `let mutable` field and `requestUpdate` for state that isn't a single value, or that
other code changes.

Lit compares a property's old and new values by identity. An F# record, list or map with equal
contents is still a new value, so assigning one renders again. That's usually harmless; when it
isn't, give the property F#'s equality:

```fsharp
open Fable.Core
open Firelight
open type Firelight.Lit

type Point = { X: float; Y: float }

[<AttachMembers>]
type Marker() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [
            "point",
            PropertyDeclaration<Point>(hasChanged = PropertyDeclaration.HasChanged(fun value old -> value <> old))
        ]

    member val point = { X = 0.0; Y = 0.0 } with get, set

    override this.render() =
        html $"""<p>{this.point.X}, {this.point.Y}</p>"""
```

## Where things go

| You need to | Put it in |
|---|---|
| Set initial values, create controllers | The constructor: `let` and `member val` |
| Listen to `window` or `document`, start a timer | `connectedCallback`, undone in `disconnectedCallback` |
| Derive values from properties | `willUpdate` |
| Focus, measure, start a JavaScript library | `firstUpdated` |
| Change the DOM after a property changes | `updated` |
| Wait for the DOM after setting a property | `updateComplete` |
| Load data | [Firelight.Task](/packages/task/) |
| Reuse any of these across components | A [controller](/guides/controllers/) |

## Common mistakes

The compiler catches some lifecycle mistakes:

| You wrote | The compiler says | Write instead |
|---|---|---|
| `member this.connectedCallback() = ...` | Warning: This new member hides the abstract member 'abstract LitElement.connectedCallback: unit -> unit'. Rename the member or use 'override' instead | `override`, and call the base |
| `override this.firstUpdated() = ...` | This override takes a different number of arguments to the corresponding abstract member | `override this.firstUpdated(_) = ...` |
| `override this.updated(changed: Map<string, obj>)` | This expression was expected to have type 'Dictionary<string,obj>' but here has type 'Map<string,obj>' | Leave the type off: `override this.updated(changed)` |
| `changed["count"] + 1` | The type 'int' does not match the type 'obj' | `unbox<int> changed["count"] + 1` |

The first row is only a warning, so the build carries on unless your project treats warnings as
errors. The hiding member replaces Lit's
`connectedCallback` on the element, so the component never renders.

Others compile:

| You wrote | What happens | Write instead |
|---|---|---|
| An override of `connectedCallback` or `update` without the base call | Nothing renders | `base.connectedCallback ()`, `base.update changed` |
| An override of `disconnectedCallback` without the base call | Controllers keep running after the element has gone | `base.disconnectedCallback ()` |
| `window.removeEventListener ("resize", this.OnResize)` | Removes nothing | `Ev.listen`, as `LastKey` does |
| `window.addEventListener` in the constructor | Never removed; prerendering fails with "window is not defined" | `connectedCallback` |
| `this.shadowRoot.querySelector` in the constructor or `render` | Throws or finds nothing: before the first update, the shadow root is missing or empty | A `ref`, read in `firstUpdated` or later |
| `this.total <- ...` in `updated` | A second update after every update | Compute it in `willUpdate` |
