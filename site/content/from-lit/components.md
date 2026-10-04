---
title: Components
tagline: LitElement classes in F#
description: "Lit components in TypeScript and Firelight F# side by side: defining and registering an element, rendering, and reactive properties."
section: from-lit
order: 10
summary: "Defining, rendering and reactive properties"
lead: "A component is still a class that extends `LitElement`. In F#, registration becomes a function call and the property decorators become one static member, as in Lit without decorators."
links:
  - text: "Lit docs: Defining"
    href: https://lit.dev/docs/components/defining/
  - text: "Lit docs: Rendering"
    href: https://lit.dev/docs/components/rendering/
  - text: "Lit docs: Reactive properties"
    href: https://lit.dev/docs/components/properties/
toc: true
---

## Defining a component

`defineElement` takes the place of `@customElement`, and `[<AttachMembers>]` makes
[Fable](https://fable.io/) compile the members onto the JavaScript class, where Lit finds them.

::: compare
```ts
import { LitElement, html } from 'lit';
import { customElement } from 'lit/decorators.js';

@customElement('hello-badge')
export class HelloBadge extends LitElement {
  render() {
    return html`<span>Hello</span>`;
  }
}
```
```fsharp
open Fable.Core
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type HelloBadge() =
    inherit LitElement()

    override _.render() =
        html $"<span>Hello</span>"

defineElement<HelloBadge> "hello-badge"
```
:::

`defineElement` registers the element when the module loads, like the decorator. Lit without
decorators does the same with `customElements.define('hello-badge', HelloBadge)`.

## Rendering

`render` works as in Lit. A helper that only builds a template doesn't need to be a method:
make it a function outside the class.

::: compare
```ts
import { LitElement, html } from 'lit';
import { customElement, property } from 'lit/decorators.js';

@customElement('order-total')
export class OrderTotal extends LitElement {
  @property({ attribute: false }) prices: number[] = [];

  private line(price: number) {
    return html`<li>${price.toFixed(2)}</li>`;
  }

  render() {
    const total = this.prices.reduce((a, b) => a + b, 0);
    return html`
      <ul>${this.prices.map((p) => this.line(p))}</ul>
      <p>Total: ${total.toFixed(2)}</p>`;
  }
}
```
```fsharp
open Fable.Core
open Fable.Core.JsInterop
open Firelight
open type Firelight.Lit

let line (price: float) =
    html $"""<li>{price.ToString "F2"}</li>"""

[<AttachMembers>]
type OrderTotal() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [
            "prices",
            PropertyDeclaration<float[]>(
                attribute = false)
        ]

    member val prices: float[] = [||] with get, set

    override this.render() =
        let total = Array.sum this.prices

        html $"""
        <ul>{this.prices |> Array.map line}</ul>
        <p>Total: {total.ToString "F2"}</p>"""

defineElement<OrderTotal> "order-total"
```
:::

`ToString "F2"` compiles to the same fixed-point formatting as `toFixed(2)`. A string inside a
hole needs the template in triple quotes, `$"""`.

A property that JavaScript code sets should be an F# array, which is a JavaScript array. An F#
list is a Fable class, so code outside F# can't pass one. Between F# components, a list works
well: a hole renders it as it renders an array.

[Templates](/from-lit/templates/) covers expressions, conditionals and lists.

## Reactive properties

Each `@property()` becomes an entry in `static member properties`, named by a string, and a
`member val` with the property's default value.

::: compare
```ts
import { LitElement, html } from 'lit';
import { customElement, property } from 'lit/decorators.js';

@customElement('stock-meter')
export class StockMeter extends LitElement {
  @property() label = 'Stock';
  @property({ type: Number }) level = 0;
  @property({ type: Boolean, reflect: true })
  low = false;

  render() {
    return html`${this.label}:
      <meter max="100" value=${this.level}></meter>`;
  }
}
```
```fsharp
open Fable.Core
open Fable.Core.JsInterop
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type StockMeter() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [
            "label", PropertyDeclaration<string>()
            "level", PropertyDeclaration<float>()
            "low",
            PropertyDeclaration<bool>(
                reflect = true)
        ]

    member val label = "Stock" with get, set
    member val level = 0.0 with get, set
    member val low = false with get, set

    override this.render() =
        html $"""{this.label}:
          <meter max="100" value={this.level}></meter>"""

defineElement<StockMeter> "stock-meter"
```
:::

Lit reads `type` to convert an attribute, as in TypeScript. `PropertyDeclaration<'T>` sets it
from `'T`: `Number` for `float`, `int` and the other numeric types, `Boolean` for `bool`. So
`<stock-meter low>` sets `low` to `true`, and `level="0.5"` sets the number 0.5. Other types get
no `type`; pass ``` ``type`` ``` (`type` is an F# keyword) to choose one yourself.

A JavaScript number is a `float`. An `int` member's setter truncates what it's given, so
`level="40.5"` would become 40.

### Attribute names

`attribute` takes a string or `false`, as in TypeScript.

::: compare
```ts
import { LitElement, html } from 'lit';
import { customElement, property } from 'lit/decorators.js';

@customElement('tag-list')
export class TagList extends LitElement {
  @property({ attribute: 'empty-text' })
  emptyText = 'No tags';
  @property({ attribute: false }) tags: string[] = [];

  render() {
    if (this.tags.length === 0) {
      return html`<i>${this.emptyText}</i>`;
    }
    const tag = (t: string) => html`<b>${t}</b> `;
    return html`${this.tags.map(tag)}`;
  }
}
```
```fsharp
open Fable.Core
open Fable.Core.JsInterop
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type TagList() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [
            "emptyText",
            PropertyDeclaration<string>(
                attribute = "empty-text")
            "tags",
            PropertyDeclaration<string[]>(
                attribute = false)
        ]

    member val emptyText = "No tags" with get, set
    member val tags: string[] = [||] with get, set

    override this.render() =
        if Array.isEmpty this.tags then
            html $"<i>{this.emptyText}</i>"
        else
            let tag t = html $"<b>{t}</b> "
            html $"{this.tags |> Array.map tag}"

defineElement<TagList> "tag-list"
```
:::

### Internal state

`@state()` is `state = true`. A private `member val` keeps its name in JavaScript, so Lit still
finds it. Lit calls a listener method with `this` set to the element; in F#, bind a lambda that
calls the method.

::: compare
```ts
import { LitElement, html, nothing } from 'lit';
import { customElement, state } from 'lit/decorators.js';

@customElement('more-text')
export class MoreText extends LitElement {
  @state() private expanded = false;

  private toggle() {
    this.expanded = !this.expanded;
  }

  render() {
    return html`
      <button @click=${this.toggle}>
        ${this.expanded ? 'Less' : 'More'}
      </button>
      ${this.expanded ? html`<slot></slot>` : nothing}`;
  }
}
```
```fsharp
open Fable.Core
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type MoreText() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [
            "expanded",
            PropertyDeclaration<bool>(state = true)
        ]

    member val private expanded = false with get, set

    member private this.Toggle() =
        this.expanded <- not this.expanded

    override this.render() =
        let body =
            if this.expanded then html $"<slot></slot>"
            else nothing

        html $"""
        <button @click={fun _ -> this.Toggle()}>
            {if this.expanded then "Less" else "More"}
        </button>
        {body}"""

defineElement<MoreText> "more-text"
```
:::

Every option keeps its name, as an optional argument of `PropertyDeclaration`:

| Lit | Firelight |
|---|---|
| `{ attribute: 'empty-text' }` | `attribute = "empty-text"` |
| `{ type: Number }` | Nothing, for a numeric `'T`; or `` ``type`` = jsConstructor<Globals.Number> `` |
| `{ reflect: true }` | `reflect = true` |
| `{ state: true }` | `state = true` |
| `{ hasChanged: (v, old) => ... }` | `hasChanged = PropertyDeclaration.HasChanged(fun v old -> ...)` |
| `{ noAccessor: true }` | `noAccessor = true` |
| `{ useDefault: true }` | `useDefault = true` |
| `{ converter: { fromAttribute, toAttribute } }` | `converter = AttributeConverter(fromAttribute = ..., toAttribute = ...)` |

## No direct equivalent

- **Decorators.** F# has no decorators; [Decorators](/from-lit/decorators/) maps each one.
- **Field declarations.** A TypeScript field is one line. In F# the property's name, as a
  string in `properties`, and its `member val` are separate, and nothing checks that they match.
- **`this`.** Each member names its own: `override this.render()`. Use `_` when the member
  doesn't need it. A lambda captures it, as an arrow function does.
- **Constructors.** The class's primary constructor replaces `constructor() { super(); ... }`:
  `let` and `do` bindings run in it. To use `this` there, declare the type as
  `type MyElement() as this =`.
- **Private fields.** A `let mutable` field is private to the class, but Lit doesn't see it, so
  changing it doesn't re-render. Declare it with `state = true`, or call `this.requestUpdate()`
  after changing it. It's private only to F#: Fable stores it on the element under its own name,
  so a field called `title` or `id` sets the element's attribute of that name (see
  [Properties](/guides/properties/#common-mistakes)).
- **`HTMLElementTagNameMap`.** TypeScript can map a tag name to its class for
  `document.createElement`. F# has no counterpart; tag names in templates are unchecked in both.

## Mistakes that compile

| You wrote | What happens | Write instead |
|---|---|---|
| No `[<AttachMembers>]` | Renders once, then never updates: Lit can't see the properties | `[<AttachMembers>]` on every component |
| `"nmae", PropertyDeclaration<string>()` | Setting `name` doesn't re-render | The `member val`'s exact name |
| `PropertyDeclaration<int64>()` or `<decimal>` for an attribute | The property holds the attribute's text: Lit's default converter has no 64-bit or decimal type | A `converter` that parses it |
| `let mutable count` changed in a handler | Nothing re-renders | A declared property, or `this.requestUpdate()` |

The [Properties and attributes guide](/guides/properties/) covers the Firelight side in more
depth.
