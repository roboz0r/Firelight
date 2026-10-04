---
title: Decorators
tagline: what each Lit decorator becomes in F#
description: "Lit's decorators and their Firelight F# equivalents: static declarations, refs, slot queries and event listener options."
section: from-lit
order: 50
summary: "What each Lit decorator becomes in F#"
lead: "F# has no decorators. Each of Lit's becomes a static member, a function call, or a line or two in a member. Nothing needs configuring: there is no `experimentalDecorators` and no `accessor`."
links:
  - text: "Lit docs: Decorators"
    href: https://lit.dev/docs/components/decorators/
  - text: "Lit API: Decorators"
    href: https://lit.dev/docs/api/decorators/
toc: true
---

| Lit | Firelight |
|---|---|
| `@customElement('x-tag')` | `defineElement<XTag> "x-tag"` after the type ([Components](/from-lit/components/#defining-a-component)) |
| `@property(options)` | An entry in `static member properties`, and a `member val` ([Components](/from-lit/components/#reactive-properties)) |
| `@state()` | `PropertyDeclaration<'T>(state = true)` ([Components](/from-lit/components/#internal-state)) |
| `@query('input')` | A `ref` to the element |
| `@query('input')`, by selector | `this.query<HTMLInputElement> "input"`, an option |
| `@queryAll('li')` | `this.queryAll<HTMLLIElement> "li"`, an array |
| `@queryAsync('input')` | Wait for `this.updateComplete`, then query |
| `@queryAssignedElements()` | `slot.assignedElements ()`, the slot from `Ev.slot` or `this.query` |
| `@queryAssignedNodes()` | `slot.assignedNodes ()` |
| `@eventOptions({ passive: true })` | `LitEventListener(handler, passive = true)` |
| `@provide`, `@consume` | `ContextProvider`, `ContextConsumer` ([Context](/from-lit/context/)) |

## @query

A `ref` does the job of `@query` without a selector. Its `value` is an option: `None` until the
element has rendered.

::: compare
```ts
import { LitElement, html } from 'lit';
import { customElement, query } from 'lit/decorators.js';

@customElement('search-box')
export class SearchBox extends LitElement {
  @query('input') input!: HTMLInputElement;

  render() {
    return html`
      <input type="search">
      <button @click=${() => this.input.focus()}>
        Focus
      </button>`;
  }
}
```
```fsharp
open Fable.Core
open Browser.Types
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type SearchBox() =
    inherit LitElement()

    let input = createRef<HTMLInputElement> ()

    override _.render() =
        let focus _ = input.value |> Option.iter _.focus()

        html $"""
        <input type="search" {ref input}>
        <button @click={focus}>
            Focus
        </button>"""

defineElement<SearchBox> "search-box"
```
:::

To query by selector instead, as `@query` does, call `this.query<HTMLInputElement> "input"` in an
event handler or after an update. It searches the render root, as the decorator does, and returns
`None` when nothing matches. `this.queryAll` returns every match, as an array. The element type is
yours to get right: nothing checks it.

## @queryAssignedElements

Firelight binds `HTMLSlotElement`, which [Fable](https://fable.io/)'s browser bindings lack.
`Ev.slot` hands the handler the `<slot>` that `@slotchange` is bound on, so it can call
`assignedElements` there, when the count changes.

::: compare
```ts
import { LitElement, html } from 'lit';
import {
  customElement, queryAssignedElements, state,
} from 'lit/decorators.js';

@customElement('item-count')
export class ItemCount extends LitElement {
  @queryAssignedElements() items!: Element[];
  @state() private count = 0;

  render() {
    return html`
      <p>${this.count} items</p>
      <slot @slotchange=${() =>
        (this.count = this.items.length)}></slot>`;
  }
}
```
```fsharp
open Fable.Core
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type ItemCount() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [
            "count",
            PropertyDeclaration<int>(state = true)
        ]

    member val private count = 0 with get, set

    override this.render() =
        let recount (slot: HTMLSlotElement) =
            this.count <- slot.assignedElements().Length

        html $"""
        <p>{this.count} items</p>
        <slot @slotchange={Ev.slot recount}></slot>"""

defineElement<ItemCount> "item-count"
```
:::

`Ev.slot` passes the listener's `currentTarget`, so bind it on the `<slot>` itself: `slotchange`
bubbles, and on a parent element `Ev.slot` would pass the parent.

Outside a `slotchange` handler, find the slot with `this.query<HTMLSlotElement>`, as the
decorator's `slot` option does. Its other options become an argument and a filter:
`assignedElements (flatten = true)` for `{ flatten: true }`, and `Array.filter` with `matches`
for `selector`:

```fsharp
open Fable.Core
open Browser.Types
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type ActionBar() =
    inherit LitElement()

    // @queryAssignedElements({ slot: 'actions', selector: 'button', flatten: true })
    member this.buttons: Element[] =
        match this.query<HTMLSlotElement> "slot[name=actions]" with
        | Some slot ->
            slot.assignedElements (flatten = true)
            |> Array.filter (fun e -> e.matches "button")
        | None -> [||]

    override _.render() =
        html $"""<slot name="actions"></slot>"""

defineElement<ActionBar> "action-bar"
```

A component that forwards its own slot, `<action-bar><slot slot="actions"></slot></action-bar>`,
assigns that `<slot>` to `actions`; `flatten = true` replaces it with the elements assigned to
it. `assignedNodes` takes the same argument, and includes text nodes.

## @eventOptions

`LitEventListener` wraps a handler with the options. Bind it where you would bind the method.

::: compare
```ts
import { LitElement, html } from 'lit';
import {
  customElement, eventOptions, state,
} from 'lit/decorators.js';

@customElement('scroll-meter')
export class ScrollMeter extends LitElement {
  @state() private top = 0;

  @eventOptions({ passive: true })
  private onScroll(e: Event) {
    this.top = (e.target as HTMLElement).scrollTop;
  }

  render() {
    return html`
      <p>Scrolled ${this.top}px</p>
      <div style="height: 5rem; overflow: auto"
        @scroll=${this.onScroll}>
        <div style="height: 20rem"></div>
      </div>`;
  }
}
```
```fsharp
open Fable.Core
open Browser.Types
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type ScrollMeter() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [
            "top",
            PropertyDeclaration<float>(state = true)
        ]

    member val private top = 0.0 with get, set

    member private this.OnScroll(e: Event) =
        this.top <- (e.target :?> HTMLElement).scrollTop

    override this.render() =
        let onScroll =
            LitEventListener(
                (fun e -> this.OnScroll e), passive = true)

        html $"""
        <p>Scrolled {this.top}px</p>
        <div style="height: 5rem; overflow: auto"
            @scroll={onScroll}>
            <div style="height: 20rem"></div>
        </div>"""

defineElement<ScrollMeter> "scroll-meter"
```
:::

`:?>` to an interface type such as `HTMLElement` compiles to nothing in JavaScript, like `as` in
TypeScript: it isn't checked when it runs.

## No direct equivalent

- **Attributes don't act.** An F# attribute is metadata. `[<AttachMembers>]` is read by the
  compiler; Firelight reads no attributes of its own, so there is no `[<Property>]` to put on a
  `member val`.
- **`@query('input', true)`.** A cached query keeps the first element it finds. A `ref` needs no
  cache, and follows the element if a render replaces it.
- **`@queryAsync`.** There is no member that returns a promise of the element. Wait for
  `this.updateComplete`, then read the ref or query.
