---
title: Component communication
tagline: properties down, events up, shared state across
description: "How Firelight components pass data to each other: properties from parent to child, events from child to parent, and context, signals or one Elmish loop for components far apart."
section: guides
order: 80
summary: "Properties down, events up, and context, signals or Elmish across"
lead: "A parent passes data to a child by setting its properties, and a child tells its parent what happened by raising an event. These are the DOM's own channels, so a Firelight component talks to any element the same way. For components far apart, share state through context, signals or a single Elmish loop."
links:
  - text: "Lit docs: Events"
    href: https://lit.dev/docs/components/events/
  - text: "Lit docs: Context"
    href: https://lit.dev/docs/data/context/
toc: true
---

Only components need these patterns. A template function gets everything it shows as arguments,
`dispatch` included, so most of an app's pieces talk to each other through ordinary F# function
calls. The patterns here are for the places where one custom element has to reach another.

## Choosing a pattern

| To share | Use | Example |
|---|---|---|
| Data from a parent to its child | Properties | `<my-chart .points={points}>` |
| What happened, from a child to its parent | Events | `@color-picked={...}` |
| A value for a whole subtree | [Context](/packages/context/) | A theme, the signed-in user, `dispatch` |
| State between unrelated components | [Signals](/packages/signals/) | A basket count in the header and on a page |
| The state of an app or a feature | One [Elmish](/packages/elmish/) loop | A board, its columns and cards |

Start with properties and events. They keep each component independent: a child knows nothing about
its parent, and a parent only knows the child's public properties and events. The other three are
for state that many components need, which otherwise passes through every layer in between.

## Parent to child: properties

A parent sets its child's properties in its template, as it would for any element. The child
declares them in `static member properties`, and Lit re-renders it when they change.

Bind a property with a dot, `.colors={colors}`, to pass any F# value: a list, a record, a function.
An attribute binding without the dot turns the value into a string, which suits only strings and
numbers that you would also write in HTML. [Binding values](/guides/templates/#binding-values) covers
the difference.

Lit re-renders a child when a property gets a new value, compared by identity. An F# list or
record is immutable, so a change always makes a new value, and the child always sees it.

## Child to parent: events

A child reports what happened by dispatching an event from itself. It doesn't call its parent:
any ancestor that cares listens, with the same `@name` binding as for `click`.

```fsharp
open Browser.Types
open Firelight

let pick (host: LitElement) (color: string) =
    host.dispatch (Event.customEvent ("color-picked", color))
```

`Event.customEvent` makes a `CustomEvent` with the value as its `detail`. It sets `bubbles` and
`composed`, so the event travels up the tree and out of the child's shadow root, to listeners on any
ancestor. In the parent's template, `Ev.custom<'T>` types the listener's event as a
`CustomEvent<'T>`, whose `detail` is a `'T option`.

This demo has both directions. The parent owns the colour and passes it to `<my-swatch-picker>`
as properties. The picker raises `color-picked` when a swatch is clicked. Pick a colour, and the
parent's text follows the event. Then press Reset: the parent sets the picker's `selected`
property, and the picker follows.

::: example Snippets/SwatchPicker.fs
<my-swatch-parent></my-swatch-parent>
:::

The picker sets its own `selected` before raising the event, as a native `<select>` changes before
it fires `change`. It works the same whether or not anyone listens.

This parent accepts every pick. A parent that refuses one has to put the picker back, and its own
value hasn't changed, so Lit skips the `.selected` binding as unchanged. Bind
`.selected={live this.color}` and call `this.requestUpdate ()` when refusing: `live` compares
with the picker's current `selected`, not with the last value rendered.

A parent could pass a callback down instead, as a property: `.onPick={fun color -> ...}`. That
works, and it's typed, but only the parent that set it hears about the pick. An event reaches any
ancestor and any listener added from outside, and the child stays usable from plain HTML. Prefer
events for components that others will use, and callbacks only inside a component you own.

For event names, `detail` types, listener options and listening outside templates, see the
[Events guide](/guides/events/).

## Components far apart

An event that bubbles and is composed already reaches distant ancestors, with no handlers in
between. Properties don't travel that way: when the component with the data is several layers
above the one that needs it, each layer in between has to accept the value and pass it on.
Context, signals and a single Elmish loop each avoid that forwarding.

### Context

A provider component holds a value, and any component inside it asks for that value by the
context's key. The components in between don't know it exists. When the provider sets a new value,
consumers that subscribed re-render. It suits values that a whole subtree reads: the theme, the
signed-in user, the language, or an Elmish `dispatch` function.

A consumer gets its value from its nearest provider among its ancestors. The value is a
`'T option`, which is `None` until a provider answers, and stays `None` for a component outside
every provider. [Firelight.Context](/packages/context/) has the code and a demo.

### Signals

A signal is a value outside any component, usually in a module. Components registered with
`LitSignals.defineElement` re-render when a signal they read changes, wherever they are on the page.
The components need no common ancestor, so signals suit state that unrelated parts of a page
share, such as a count in the header and a list in the main content.

A signal in a module is one value for the whole page, shared by every component that reads it.
Keep the state of a single widget in the widget. Lit's signals are experimental.
[Firelight.Signals](/packages/signals/) has the code and a demo.

### One Elmish loop

For an app or a feature with one model, run one [Elmish](/packages/elmish/) loop in the component
at its top, and draw the rest with template functions that take the model, or the part they show,
and `dispatch`:

```fsharp
open Fable.Core
open Firelight
open Firelight.Elmish
open type Firelight.Lit

type Model = { Items: string list; Selected: string option }

type Msg = Select of string

let update (Select item) model = { model with Selected = Some item }

let itemView (selected: string option) (dispatch: Msg -> unit) (item: string) =
    html $"""<li><button aria-pressed={(selected = Some item)} @click={fun _ -> dispatch (Select item)}>{item}</button></li>"""

let listView (model: Model) dispatch =
    html $"""<ul>{model.Items |> List.map (itemView model.Selected dispatch)}</ul>"""

[<AttachMembers>]
type Picker() as this =
    inherit LitElement()

    let elmish =
        ElmishController.simple this (fun () -> { Items = [ "One"; "Two" ]; Selected = None }) update

    override _.render() = listView elmish.model elmish.dispatch
```

Every view reads the same model and sends messages to the same `update`, so there is nothing to
keep in step. The [Kanban demo](/demos/kanban/) is built this way: one component, one loop, and
template functions for the columns and cards.

When a part of the view needs to be a component, for its own lifecycle or a `repeat` that keeps
its element, give it the model and `dispatch` as properties or through context. The
[Todo demo](/demos/todo/) provides both through context, and each item component reads them.

Inside one loop, views don't need DOM events to reach each other: a message already reaches every
view through the model. Keep `update` plain F#, with no DOM in it. Raise an event only to tell
something outside the loop, such as the page around the component, and do it from a command.

## When to use which

- A child that the parent renders directly: properties in, events out.
- A value read across a subtree, which changes rarely: context.
- State read in unrelated places on the page: signals.
- An app's or a feature's own state: one Elmish loop, with templates for most of the view.

The patterns mix. An app can keep its data in an Elmish loop, provide the theme through context, and
use properties and events for a date picker inside a form. Pick the most local one that works:
properties and events are the easiest to follow, because the template shows where data goes.

## Common mistakes

The compiler catches one:

| You wrote | The compiler says | Write instead |
|---|---|---|
| `Ev.custom<string> (fun e -> this.color <- e.detail)` | Type constraint mismatch. The type 'string option' is not compatible with type 'string' | `e.detail \|> Option.iter (fun c -> this.color <- c)` |

The rest compile, because a template's holes, event names and `detail` types aren't checked:

| You wrote | What happens | Write instead |
|---|---|---|
| `colors={colors}`, for a list | The child gets an attribute, not the list | `.colors={colors}` |
| `this.colors.Add c`, on a `ResizeArray` passed down | The child doesn't re-render | An F# list, assigned anew: `this.colors <- this.colors @ [ c ]` |
| `CustomEvent.Create ("color-picked", init)`, with the default options | Only a listener on the child itself hears it. It doesn't bubble or leave a shadow root | `Event.customEvent ("color-picked", c)` |
| `@colour-picked={...}`, a different spelling | Nothing happens | The exact name the child dispatches |
| `Ev.custom<int>` for a `string` detail | `detail` is a string at run time, whatever the type says | The type the child sends |
| A consumer rendered outside every provider | Its value stays `None` | Render it inside the provider, or pass the value as a property |
| A consumer moved out of its provider, such as in a dialog moved to `document.body` | It keeps the last value it got and stops updating | Keep it inside the provider |

An event's name is part of the template's fixed text, so it can't come from an F# constant. Copy it
exactly, and keep each component's event names in one place, such as a comment on the component.
