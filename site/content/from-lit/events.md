---
title: Events
tagline: listening and dispatching in F#
description: "Lit events in TypeScript and Firelight F# side by side: typed listeners with Ev, dispatching custom events, listening for them, and listening on the element itself."
section: from-lit
order: 40
summary: "Listening, dispatching custom events and listening on the element"
lead: "`@event` bindings don't change. The handler is an F# function: `Ev` gives it its event's type, and `Event.customEvent` builds a custom event that crosses shadow roots."
links:
  - text: "Lit docs: Events"
    href: https://lit.dev/docs/components/events/
toc: true
---

## Listening in a template

Lit calls a listener method with `this` set to the element. In F#, pass the method itself:
`Ev.keyboard this.OnKey` turns it into a function and tells F# the event is a `KeyboardEvent`.

::: compare
```ts
import { LitElement, html } from 'lit';
import { customElement, state } from 'lit/decorators.js';

@customElement('quick-add')
export class QuickAdd extends LitElement {
  @state() private items: string[] = [];

  private onKey(e: KeyboardEvent) {
    const input = e.target as HTMLInputElement;
    const text = input.value;
    if (e.key === 'Enter' && text !== '') {
      this.items = [...this.items, text];
      input.value = '';
    }
  }

  render() {
    const item = (i: string) => html`<li>${i}</li>`;
    return html`
      <input @keydown=${this.onKey}>
      <ul>${this.items.map(item)}</ul>`;
  }
}
```
```fsharp
open Fable.Core
open Browser.Types
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type QuickAdd() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [
            "items",
            PropertyDeclaration<string[]>(state = true)
        ]

    member val private items: string[] = [||] with get, set

    member private this.OnKey(e: KeyboardEvent) =
        let input = e.target :?> HTMLInputElement
        let text = input.value

        if e.key = "Enter" && text <> "" then
            this.items <- Array.append this.items [| text |]
            input.value <- ""

    override this.render() =
        let item i = html $"<li>{i}</li>"

        html $"""
        <input @keydown={Ev.keyboard this.OnKey}>
        <ul>{this.items |> Array.map item}</ul>"""

defineElement<QuickAdd> "quick-add"
```
:::

`Ev.mouse`, `Ev.pointer`, `Ev.focus`, `Ev.input` and the others do the same for their event types.
`Ev.value` and `Ev.checked'` pass the handler the field's value instead of the event; see
[Templates](/from-lit/templates/#expressions). Nothing checks the type against the event's name:
`@click={Ev.keyboard ...}` compiles.

## Dispatching a custom event

`Event.customEvent` sets `bubbles` and `composed` to `true` for you. `dispatchEvent` returns
whether the event went uncancelled, which F# makes you `ignore` or use.

::: compare
```ts
import { LitElement, html } from 'lit';
import { customElement } from 'lit/decorators.js';

@customElement('swatch-row')
export class SwatchRow extends LitElement {
  private pick(color: string) {
    this.dispatchEvent(new CustomEvent('color-picked', {
      detail: color, bubbles: true, composed: true,
    }));
  }

  render() {
    return ['red', 'teal'].map((c) => html`
      <button @click=${() => this.pick(c)}>${c}</button>`);
  }
}
```
```fsharp
open Fable.Core
open Browser.Types
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type SwatchRow() =
    inherit LitElement()

    member private this.Pick(color: string) =
        Event.customEvent ("color-picked", color)
        |> this.dispatchEvent
        |> ignore

    override this.render() =
        let swatch c =
            let pick _ = this.Pick c
            html $"""<button @click={pick}>{c}</button>"""

        html $"""{[ "red"; "teal" ] |> List.map swatch}"""

defineElement<SwatchRow> "swatch-row"
```
:::

`render` in TypeScript can return the array itself. F#'s `render` returns a `ChildRenderable`,
which a list isn't, so the list goes in a template.

## Listening for a custom event

`Ev.custom` types the event as a `CustomEvent<'T>`. Its `detail` is a `'T option`, where
TypeScript has a plain `T`.

::: compare
```ts
import { LitElement, html } from 'lit';
import { customElement, state } from 'lit/decorators.js';

@customElement('color-chooser')
export class ColorChooser extends LitElement {
  @state() private color = 'none';

  private onPicked(e: CustomEvent<string>) {
    this.color = e.detail;
  }

  render() {
    return html`
      <p>Picked: ${this.color}</p>
      <swatch-row @color-picked=${this.onPicked}>
      </swatch-row>`;
  }
}
```
```fsharp
open Fable.Core
open Browser.Types
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type ColorChooser() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [
            "color",
            PropertyDeclaration<string>(state = true)
        ]

    member val private color = "none" with get, set

    member private this.OnPicked(e: CustomEvent<string>) =
        e.detail |> Option.iter (fun c -> this.color <- c)

    override this.render() =
        html $"""
        <p>Picked: {this.color}</p>
        <swatch-row @color-picked={Ev.custom this.OnPicked}>
        </swatch-row>"""

defineElement<ColorChooser> "color-chooser"
```
:::

Nothing checks that the event's `detail` is the type you name, in either language.

## Listening on the element itself

The F# class has no `addEventListener`: [Fable](https://fable.io/)'s `HTMLElement` is an
interface, which
`LitElement` can't inherit. Cast `this` to reach it.

::: compare
```ts
import { LitElement, html } from 'lit';
import { customElement, state } from 'lit/decorators.js';

@customElement('click-total')
export class ClickTotal extends LitElement {
  @state() private clicks = 0;

  constructor() {
    super();
    this.addEventListener('click', () => this.clicks++);
  }

  render() {
    return html`<slot></slot> (${this.clicks})`;
  }
}
```
```fsharp
open Fable.Core
open Browser.Types
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type ClickTotal() as this =
    inherit LitElement()

    do
        let host = box this :?> HTMLElement
        let count _ = this.Count()
        host.addEventListener ("click", count)

    static member properties =
        PropertyDeclarations.create [
            "clicks", PropertyDeclaration<int>(state = true)
        ]

    member val private clicks = 0 with get, set

    member private this.Count() =
        this.clicks <- this.clicks + 1

    override this.render() =
        html $"<slot></slot> ({this.clicks})"

defineElement<ClickTotal> "click-total"
```
:::

`as this` names the instance in the constructor, where `do` runs.

## No direct equivalent

- **Event options.** `@eventOptions` becomes a `LitEventListener`; see
  [Decorators](/from-lit/decorators/#eventoptions).
- **Listeners on `window` or `document`.** Add them in `connectedCallback`, and keep the handler
  to remove it; see [Lifecycle](/from-lit/lifecycle/#connecting-and-disconnecting).
- **Typed `addEventListener`.** TypeScript picks the event type from the event's name. Fable's
  `addEventListener` takes an `Event -> unit`, so cast inside the handler.
- **`detail`.** It's an option in F#: `None` when the event has none.
- **Defaults.** `new CustomEvent(...)` doesn't bubble or cross shadow roots unless you say so.
  `Event.customEvent` does both unless you pass `bubbles = false` or `composed = false`.

The [Events guide](/guides/events/) covers the Firelight side in more depth.
