---
title: Controllers
tagline: reusable behaviour for components
description: "Write Lit reactive controllers in F#: bundle state and lifecycle, such as a timer or a media query, into an object any component can use. How the Observers, Task and other packages' controllers fit, and when to choose a controller."
section: guides
order: 60
summary: "Reactive controllers: state and lifecycle that any component can reuse"
lead: "Write a reactive controller to bundle behaviour that has state and needs setting up and tearing down, such as a timer, a media query or a request. Any component can then use it: create it with the component as its host, and read it in `render`. The controller follows the component's lifecycle and asks it to render when its state changes."
links:
  - text: "Lit docs: Reactive controllers"
    href: https://lit.dev/docs/composition/controllers/
toc: true
---

A component adds a controller with one line, such as `let watch = Stopwatch(this)`, and reads it
in `render`. The controller does the rest: it starts and stops with the component and keeps its
own state. Several of Firelight's packages are controllers, and a component can use as many as it
needs.

## Writing a controller

A controller is a class that inherits `ReactiveControllerBase` and registers itself with its host,
the component, when it's created. It overrides the methods it needs, of four, one for each point
in the host's lifecycle:

| Method | Called | Runs on the server |
|---|---|---|
| `hostConnected` | When the host joins a page, in Lit's `connectedCallback` | No |
| `hostDisconnected` | When the host leaves the page, in Lit's `disconnectedCallback` | No |
| `hostUpdate` | At each update, before the host renders | No |
| `hostUpdated` | At each update, after the host has rendered | No |

An update that the host's `shouldUpdate` skips calls neither of the last two. Lit's interface makes
all four optional. `ReactiveControllerBase` gives each one a body that does nothing, so you
override only the ones you use. It implements Firelight's `ReactiveController` interface, which a
class can also implement directly; then F# needs all four members, with `()` for the ones you
don't use. That's the way for a class that already inherits something else. The host is a `ReactiveControllerHost`, which every `LitElement` is. It has `addController`,
`requestUpdate` and `updateComplete`, which is all most controllers need.

`addController` on a host that's already on a page calls `hostConnected` straight away, so set up
the controller's fields before it registers. `removeController` doesn't call `hostDisconnected`:
clean up first.

This stopwatch measures time and re-renders its host ten times a second while it runs. Two
different components use it: one counts up, the other counts down from ten seconds. Start both,
then stop the stopwatch: the countdown carries on, as each component has its own stopwatch.

::: example Snippets/StopwatchTimers.fs .stacked
<my-stopwatch></my-stopwatch>
<my-countdown></my-countdown>
:::

How the parts fit:

- `as this` names the controller in its own constructor, so `do host.addController this` can
  register it. From then on, Lit calls its methods with the host's. The base class doesn't
  register the controller for you: in its constructor, your fields aren't set yet.
- It overrides `hostConnected` and `hostDisconnected`, and leaves the update methods to the base
  class.
- The component creates it in a `let` binding, once. The component needs `as this` too, to pass
  itself as the host.
- `Start` and `Stop` change the controller's state and call `host.requestUpdate ()`, as a
  component calls its own `requestUpdate` after changing a `let mutable` field.
- The timer only runs while the host is on the page: `hostDisconnected` clears it, `hostConnected`
  starts it again if the stopwatch is running, and `Start` only starts it while connected. The
  elapsed time comes from `performance.now()`, not from counting ticks, so it stays right while
  nothing renders.

## A controller that reads the browser

Controllers are a natural home for browser APIs that a component would otherwise set up and tear
down itself. This one tracks a CSS media query, such as the system's dark mode:

```fsharp
open Browser.Types
open Fable.Core
open Firelight
open type Firelight.Lit

// Fable.Browser.Dom has no matchMedia, so declare the part this needs.
type MediaQueryList =
    inherit EventTarget
    abstract matches: bool

[<Emit("window.matchMedia($0)")>]
let matchMedia (query: string) : MediaQueryList = jsNative

/// Whether a CSS media query matches, such as "(prefers-color-scheme: dark)".
type MediaQuery(host: ReactiveControllerHost, query: string) as this =
    inherit ReactiveControllerBase()

    let mutable matches = false
    let mutable stopListening = ignore

    do host.addController this

    member _.Matches = matches

    override _.hostConnected() =
        let list = matchMedia query

        let read () =
            matches <- list.matches
            host.requestUpdate ()

        stopListening <- Ev.listen list "change" (Ev.event (fun _ -> read ()))
        // After the host's first update: see below.
        host.updateComplete.``then`` (fun _ -> read ()) |> ignore

    override _.hostDisconnected() = stopListening ()

[<AttachMembers>]
type ThemeNote() as this =
    inherit LitElement()

    let dark = MediaQuery(this, "(prefers-color-scheme: dark)")

    override _.render() =
        html $"""<p>Your system prefers a {if dark.Matches then "dark" else "light"} theme.</p>"""
```

It touches `window` only in `hostConnected`, so a component that uses it can still be prerendered
at build time, where there is no `window`. It also reads the query only after the host's first
update. The build can't know the answer, so the prerendered page says "light", and a prerendered
component's first render in the browser has to match it. Reading the query in `hostConnected`
would change that first render, and the prerendered text would stay on the page. Without
prerendering, read it in `hostConnected` and skip the wait. [Prerendering components at build
time](/guides/prerendering/) has the rules.

Keep the cleanup next to the setup, as `stopListening` does: `Ev.listen` returns the function that
removes its listener. Removing one with `removeEventListener` yourself needs the very function
`addEventListener` was given, which a method or a class-level `let` function isn't (see
[Events](/guides/events/#listen-outside-the-component)).

## Controllers in Firelight's packages

Several packages ship controllers. You use them like your own: create them in a `let` binding
with the component as the host, and read them in `render`.

| Package | Controller | Gives the component |
|---|---|---|
| [Firelight.Observers](/packages/observers/) | `ResizeController`, `IntersectionController`, `MutationController`, `PerformanceController` | A `value` from the browser's observers: its size, whether it's on screen |
| [Firelight.Task](/packages/task/) | `LitTask` | Async work, run when its arguments change, with pending, complete and error states |
| [Firelight.Elmish](/packages/elmish/) | `ElmishController` | An Elmish loop: `model` and `dispatch` |
| [Firelight.Context](/packages/context/) | `ContextProvider`, `ContextConsumer` | Values shared down the tree |
| [Firelight.Router](/packages/router/) | `RouterController` | The route for the current address |
| [Firelight.Motion](/packages/motion/) | `AnimateController` | Control over the component's animations |

The Observers controllers are a good example of what a controller is for. Each wraps one of the
browser's observers: it starts observing when the host connects, stops when it disconnects, turns
the observer's entries into a value through your callback, and asks the host to render. The
[Observers package page](/packages/observers/) has a panel that lays itself out by its own width.

`LitTask` uses `hostUpdate`: before each render, it checks its arguments and starts the task
when they've changed. The [Task package page](/packages/task/) has an example. `ElmishController`
uses `hostConnected` and `hostDisconnected` to ask for updates only while the host is on a page.

## Controller, function, component or base class

A controller is one of four ways to reuse code between components:

- A **function** that returns a template, when the reused part is markup with no state of its own.
  This is the default; see [Templates](/guides/templates/).
- A **component**, when the reused part has its own markup and state, such as a date picker. Use it
  as an element in other components' templates.
- A **controller**, when the reused part is behaviour with state that follows the lifecycle, such
  as a subscription, a timer or a request, and each component renders it its own way. The
  stopwatch and countdown above share the timing, not the markup.
- A **base class**, which inherits from `LitElement`, can do the same as a controller, but a
  component has only one base class, while it can use any number of controllers. A controller also
  keeps its state to itself, behind the members you give it.

## Common mistakes

The compiler catches some controller mistakes:

| You wrote | The compiler says | Write instead |
|---|---|---|
| `interface ReactiveController with` and only `hostConnected` and `hostDisconnected` | No implementation was given for those members: 'abstract ReactiveController.hostUpdate: unit -> unit' … | `inherit ReactiveControllerBase()` and `override` what you need, or `member _.hostUpdate() = ()` for each method you don't |
| `member _.hostConnected() = ...` in a class that inherits `ReactiveControllerBase` | Warning: This new member hides the abstract member 'abstract ReactiveControllerBase.hostConnected: unit -> unit'. Rename the member or use 'override' instead | `override _.hostConnected() = ...` |
| `do host.addController this`, or `Stopwatch(this)` in a component, without `as this` | The value or constructor 'this' is not defined | `type Stopwatch(host: ReactiveControllerHost) as this =` |

Others compile:

| You wrote | What happens | Write instead |
|---|---|---|
| No `host.addController this` | Lit never calls the controller's methods: it never starts, or never stops | Register it in its constructor |
| `Stopwatch(this)` in `render` | A new controller at each render, each one added to the host and never removed | A `let` binding in the component |
| A timer or listener started in the controller's constructor | It runs before the host is on a page, and on the server when prerendering | Start it in `hostConnected` |
| Nothing undone in `hostDisconnected` | The work goes on after the component has gone, holding it in memory | Undo in `hostDisconnected` what `hostConnected` did |
| `host.requestUpdate ()` in `hostUpdated` | A second update after every update | Request an update where the state changes |
