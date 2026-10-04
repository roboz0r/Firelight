---
title: Events
tagline: raise and handle DOM events from F#
description: "Handle DOM events in Firelight templates, raise CustomEvents from components with typed details, and see how bubbles, composed and shadow DOM decide who hears them."
section: guides
order: 30
summary: "Listen with @event, raise CustomEvents, and how they cross shadow DOM"
lead: "Components talk to the page the way built-in elements do: they listen for DOM events and raise their own. Firelight adds typed handlers and a helper for custom events. The events themselves are the browser's, so anything on the page can listen, in F# or not."
links:
  - text: "Lit docs: Events"
    href: https://lit.dev/docs/components/events/
  - text: "MDN: Event bubbling"
    href: https://developer.mozilla.org/en-US/docs/Learn_web_development/Core/Scripting/Event_bubbling
toc: true
---

A button raises `click`, and a text box raises `input`. A component can raise `count-changed` or
`note-closing` in the same way, and whoever uses it listens with `@count-changed` in a template or
`addEventListener` in JavaScript. Nothing about the component's F# types leaks into the page.

This guide covers events on one component and its parent. How larger apps pass data between
components is the subject of the [component communication guide](/guides/communication/).

## Listen with @event

An `@event` binding adds a listener to the element it's on:

```fsharp
open Firelight
open type Firelight.Lit

let saveButton (save: unit -> unit) =
    html $"""<button @click={fun _ -> save ()}>Save</button>"""
```

The hole takes any function of the event. A new lambda on each render is fine: Lit keeps one DOM
listener and calls the newest function.

### Typed handlers with Ev

A hole's type is `obj`, so F# can't infer what the event is. The `Ev` module supplies the type, as
the [Templates guide](/guides/templates/#typed-event-handlers) shows: in
`Ev.keyboard (fun e -> ...)`, `e` is a `KeyboardEvent`. Pick the function for the event you bind:

| Function | Your function gets | Bind it to |
|---|---|---|
| `Ev.mouse` | A `MouseEvent` | `click`, `dblclick`, `contextmenu` and the other mouse events |
| `Ev.pointer` | A `PointerEvent` | `pointerdown`, `pointermove`, `pointerup` and so on |
| `Ev.keyboard` | A `KeyboardEvent` | `keydown`, `keyup` |
| `Ev.focus` | A `FocusEvent` | `focus`, `blur`, `focusin`, `focusout` |
| `Ev.input` | An `InputEvent` | `input`, `beforeinput` |
| `Ev.wheel` | A `WheelEvent` | `wheel` |
| `Ev.drag` | A `DragEvent` | `dragstart`, `dragover`, `drop` and so on |
| `Ev.touch` | A `TouchEvent` | `touchstart`, `touchmove`, `touchend` |
| `Ev.submit` | A `SubmitEvent` | `submit` on a `<form>` |
| `Ev.custom<'T>` | A `CustomEvent<'T>` | A component's own events: [Typed details](#typed-details) |
| `Ev.event` | An `Event` | Anything else, such as `change` |
| `Ev.value` | The field's `value`, a string | `input` or `change` on the field |
| `Ev.checked'` | Whether the box is checked | `change` on the checkbox |
| `Ev.valueAs<'T>` | The element's `value`, unboxed as a `'T` | A component whose `value` isn't a string |
| `Ev.slot` | The `<slot>`, an `HTMLSlotElement` | `slotchange` on the slot |

`Ev.value` and `Ev.checked'` read the element the listener is on, so bind them on the field itself,
not on a `<form>` around it. `Ev.valueAs<'T>` is for components whose `value` property holds
something other than a string, such as a slider's number: it unboxes the value and parses nothing.
A native `<input type="number">`'s `value` is still a string, so use `Ev.value` there and parse it.
`Ev.slot` also reads the element the listener is on, so bind it on the `<slot>`; the
[slots recipe](/cookbook/slots/) uses it.
Nothing checks the function against the event's name; see [Typed details](#typed-details).

A few members of the event are worth knowing in any handler:

| Member | What it is | Typical use |
|---|---|---|
| `e.preventDefault ()` | Stops the browser's own action | A form's submit, a link's navigation |
| `e.currentTarget` | The element the listener is on | Reading the field that changed |
| `e.target` | Where the event started, as seen from the listener | Finding which child was clicked |
| `e.stopPropagation ()` | Stops the event reaching listeners further up | Rarely; it hides the event from the page too |

A form is the common case for `preventDefault`. Without it, submitting reloads the page:

```fsharp
open Firelight
open type Firelight.Lit

let searchForm (query: string) (setQuery: string -> unit) (search: unit -> unit) =
    let submit =
        Ev.submit (fun e ->
            e.preventDefault ()
            search ())

    html
        $"""
    <form @submit={submit}>
        <input aria-label="Search" .value={query} @input={Ev.value setQuery}>
        <button>Search</button>
    </form>"""
```

### Listener options

For the options of `addEventListener`, bind a `LitEventListener` instead of a function. `passive`
promises the browser that the handler won't call `preventDefault`, so it can scroll without waiting
for it. `once` removes the listener after the first event, and `capture` runs it on the way down,
before the event reaches its target.

```fsharp
open Browser.Types
open Firelight
open type Firelight.Lit

let zoomArea (zoom: float -> unit) =
    let onWheel = LitEventListener(Ev.wheel (fun e -> zoom e.deltaY), passive = true)

    html $"""<div class="canvas" @wheel={onWheel}><slot></slot></div>"""
```

### Listen outside the component

A template's listeners only cover the component's own elements. For a key pressed anywhere, or a
click outside a menu, add a listener to `window` or `document` in `connectedCallback`, and remove it
in `disconnectedCallback`. Otherwise every component that's been removed keeps listening, and
keeps the component alive in memory.

`Ev.listen target name handler` adds the listener and returns a function that removes it. Keep
that function, and call it on the way out:

```fsharp
open Fable.Core
open Browser
open Browser.Types
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type Menu() =
    inherit LitElement()

    let mutable stopListening = ignore

    static member properties =
        PropertyDeclarations.create [ "isOpen", PropertyDeclaration<bool>(state = true) ]

    member val isOpen = false with get, set

    override this.connectedCallback() =
        base.connectedCallback ()

        stopListening <-
            Ev.listen window "keydown" (Ev.keyboard (fun e -> if e.key = "Escape" then this.isOpen <- false))

    override this.disconnectedCallback() =
        base.disconnectedCallback ()
        stopListening ()

    override this.render() =
        html
            $"""
        <button @click={fun _ -> this.isOpen <- not this.isOpen}>Menu</button>
        {if this.isOpen then html $"<ul><li>Escape closes this</li></ul>" else nothing}"""

defineElement<Menu> "my-menu"
```

The handler can be typed with any `Ev` function; `listen` doesn't check it against the event's
name. `connectedCallback` also keeps `window` out of the constructor and `render`, which Lit SSR
runs where there is no `window`.

Calling `window.addEventListener` and `removeEventListener` yourself works too, with one trap:
`removeEventListener` only removes the very function it's given. A method, or a function bound
with `let` in the class, doesn't qualify: [Fable](https://fable.io/) wraps it in a new JavaScript
function each time it's passed, so removing it removes nothing. The remover `Ev.listen` returns
holds the one function it added.

## Raise an event

To tell its parent something happened, a component dispatches an event on itself:

```fsharp
open Fable.Core
open Browser.Types
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type Counter() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [ "count", PropertyDeclaration<int>(state = true) ]

    member val count = 0 with get, set

    member this.Increment() =
        this.count <- this.count + 1
        this.dispatch (Event.customEvent ("count-changed", this.count))

    override this.render() =
        html $"""<button @click={fun _ -> this.Increment()}>Clicked {this.count} times</button>"""

defineElement<Counter> "my-counter"
```

`Event.customEvent (name, detail)` comes with `open Browser.Types`. It makes a `CustomEvent` that
carries `detail` and, by default, bubbles and is composed: the [next section](#how-far-an-event-goes)
explains both. `this.dispatch` raises it from the component. It's Lit's `dispatchEvent` without
the result: `dispatchEvent` returns `false` if a listener cancelled the event, and `true`
otherwise, which only matters for [an event a listener can cancel](#events-a-listener-can-cancel).

Name events like the browser's own, in lower case with hyphens. Event names are case-sensitive, so
a listener for `countchanged` misses `countChanged`. Raise the event after the change, so a listener
that reads the component's properties sees the new values. Its DOM is still the old one until the
next render; to raise an event once that's done, wait for `this.updateComplete` first.

### Typed details

`detail` can be any F# value: a number, a record, a union. Nothing serialises it, so the listener
gets the same object. In a template, `Ev.custom<'T>` types the handler, and `e.detail` is a
`'T option`, `None` if the event came without one.

Nothing connects the two ends. The name in `@count-changed` is template text that only Lit reads,
and `Ev.custom<string>` compiles on an event whose detail is an `int`. Keep the name, the detail
type and the handler together in one module, so there is one place to get them right:

```fsharp
open Browser.Types
open Firelight
open type Firelight.Lit

type Swatch = { Name: string; Hex: string }

module SwatchPicked =
    /// Bind as @swatch-picked.
    let name = "swatch-picked"

    let raise (host: LitElement) (swatch: Swatch) =
        host.dispatch (Event.customEvent (name, swatch))

    let handle (onPicked: Swatch -> unit) =
        Ev.custom<Swatch> (fun e -> e.detail |> Option.iter onPicked)

let picker (pick: Swatch -> unit) =
    html $"""<my-swatch-picker @swatch-picked={SwatchPicked.handle pick}></my-swatch-picker>"""
```

A JavaScript listener sees Fable's representation of the value. A record arrives as an object
with its fields, and a string or a number as itself, but a union, a list or a `Map` arrive as
Fable's own classes. For events that pages written in JavaScript will handle, use strings, numbers,
records or anonymous records.

### Events a listener can cancel

Some events ask permission: the component raises one before it acts, and acts only if no listener
calls `preventDefault`. Such an event must be created with `cancelable = true`, and raised with
`dispatchEvent`, whose result says whether a listener cancelled it.

Tick the box, then close the note:

::: example Snippets/EventCancel.fs
<my-note-board></my-note-board>
:::

The note raises `note-closing`, and closes only if `dispatchEvent` returns `true`. The board
calls `e.preventDefault ()` while the box is ticked. Without `cancelable`, `preventDefault` does
nothing and `dispatchEvent` returns `true` regardless.

`dispatchEvent` runs every listener before it returns, so a listener must decide at once. One that
waits, say for a confirmation dialog or a request, is too late: by the time it calls
`preventDefault`, the note has closed. To ask first, cancel the event, then tell the component to
close later.

## How far an event goes

An event starts at its target and, if it **bubbles**, travels up through each ancestor, where
listeners can hear it. Shadow DOM adds a boundary: an event raised inside a shadow root stops at
it unless it's **composed**. The browser's own UI events, such as `click`, `input` and `keydown`,
are both. `CustomEvent`s are neither unless you say so, which is why `Event.customEvent` sets both
by default.

`this.dispatchEvent` raises the event on the component's host element, which lives in its parent's
tree, not its own shadow root. So the parent hears it even when it isn't composed. Composed only
decides whether it goes further: out of the parent's shadow root, to the page and the components
around it.

This component has the buttons in its shadow DOM, and listens to them. The page listens on
`document`. Raise a few signals of each kind:

::: example Snippets/EventPath.fs .stacked
<my-event-path></my-event-path>
<p id="page-heard">The page hasn't heard a signal yet.</p>
:::

<script type="module">
  const pageHeard = document.getElementById("page-heard");
  document.addEventListener("signal", (e) => {
    const origin = e.composedPath()[0].localName;
    pageHeard.textContent = `The page heard signal ${e.detail.Number}. Its target: <${e.target.localName}>; where it started: <${origin}>.`;
  });
</script>

The component hears every signal. The page hears only the composed ones, and their target is
`<my-event-path>`, not the buttons component. As an event leaves a shadow root, the browser
**retargets** it to the host, so listeners outside don't see inside. `e.composedPath ()` still
lists the elements it passed through, starting from the real one, except those inside closed
shadow roots. Lit's are open unless you say otherwise.

Keep the defaults for an event that's part of the component's public face, such as `count-changed`:
the page, and any component the element ends up inside, can hear it. For an event that only the
direct parent should see, such as one between two parts of a larger component, pass
`bubbles = false, composed = false`. Then the parent hears it only on the element itself, as in
`<my-picker @picked={...}>` (or with a capturing listener higher up).

## Common mistakes

The compiler catches a few:

| You wrote | The compiler says | Write instead |
|---|---|---|
| `this.dispatchEvent (Event.customEvent (...))` as a statement | The result of this expression has type 'bool' and is implicitly ignored (a warning) | `this.dispatch (...)`, or use the result |
| `e.detail.Name` | The type 'Option<_>' does not define a field, constructor, or member named 'Name' | `e.detail \|> Option.iter (fun s -> ...)` |
| `Event.customEvent` without `open Browser.Types` | The value, constructor, namespace or type 'customEvent' is not defined | `open Browser.Types` |

Most compile, because event names are strings and a hole accepts any value:

| You wrote | What happens | Write instead |
|---|---|---|
| `"countChanged"` raised, `@count-changed` bound | The handler never runs | The same name at both ends, lower case with hyphens |
| `Ev.custom<string>` for a detail that's an `int` | `e.detail` holds an `int`; string functions fail at run time | The detail's real type, kept beside the name |
| `composed = false`, from a component inside another's shadow DOM | The page never hears it | Composed, the default of `Event.customEvent` |
| A page listener reading `e.target` for the inner element | It gets the outermost host | `e.composedPath ()`, or put what it needs in `detail` |
| `e.preventDefault ()` on an event made by `Event.customEvent (name, detail)` | Nothing: the event isn't cancelable | `Event.customEvent (name, detail, cancelable = true)` |
| `window.addEventListener` in the constructor or `render` | Fails in Lit SSR; with no removal, listens forever | `connectedCallback`, removed in `disconnectedCallback` |
| `removeEventListener` with a class `let` function or a method | The listener stays | `Ev.listen`, and call the function it returns |
