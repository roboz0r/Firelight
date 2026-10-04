---
title: Lifecycle
tagline: connecting, updating and changedProperties in F#
description: "Lit's lifecycle in TypeScript and Firelight F# side by side: connected and disconnected callbacks, willUpdate and changedProperties, firstUpdated, and waiting for updateComplete."
section: from-lit
order: 30
summary: "Connecting, the update cycle and changedProperties"
lead: "The callbacks are Lit's, overridden with `override`. Call `base` where TypeScript calls `super`, and read `changedProperties` as a dictionary keyed by property name."
links:
  - text: "Lit docs: Lifecycle"
    href: https://lit.dev/docs/components/lifecycle/
toc: true
---

## Connecting and disconnecting

Listeners on `window` or `document` go on in `connectedCallback` and come off in
`disconnectedCallback`, as in Lit. F# has no arrow-function field to hand both calls the same
function, so `Ev.listen` adds the listener and returns the function that removes it.

::: compare
```ts
import { LitElement, html, nothing } from 'lit';
import { customElement, state } from 'lit/decorators.js';

@customElement('esc-notice')
export class EscNotice extends LitElement {
  @state() private shown = true;

  private onKey = (e: KeyboardEvent) => {
    if (e.key === 'Escape') this.shown = false;
  };

  connectedCallback() {
    super.connectedCallback();
    window.addEventListener('keydown', this.onKey);
  }

  disconnectedCallback() {
    super.disconnectedCallback();
    window.removeEventListener('keydown', this.onKey);
  }

  render() {
    return this.shown
      ? html`<p>Press Escape to close</p>`
      : nothing;
  }
}
```
```fsharp
open Fable.Core
open Browser
open Browser.Types
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type EscNotice() =
    inherit LitElement()

    let mutable stop = ignore

    static member properties =
        PropertyDeclarations.create [
            "shown", PropertyDeclaration<bool>(state = true)
        ]

    member val private shown = true with get, set

    override this.connectedCallback() =
        base.connectedCallback ()

        let onKey (e: KeyboardEvent) =
            if e.key = "Escape" then
                this.shown <- false

        stop <- Ev.listen window "keydown" onKey

    override this.disconnectedCallback() =
        base.disconnectedCallback ()
        stop ()

    override this.render() =
        if this.shown then
            html $"<p>Press Escape to close</p>"
        else
            nothing

defineElement<EscNotice> "esc-notice"
```
:::

`Ev.listen` takes a handler of any event type and casts each event to it, unchecked, like `as` in
TypeScript: nothing compares `KeyboardEvent` with the name `"keydown"`.

## Computing values before render

`willUpdate` gets the changed properties as a `Dictionary<string, obj>`: `ContainsKey` where
TypeScript calls `has`, and an indexer for the old value.

::: compare
```ts
import { LitElement, html } from 'lit';
import type { PropertyValues } from 'lit';
import { customElement, property } from 'lit/decorators.js';

const letter = (s: string) => s.slice(0, 1);

@customElement('name-badge')
export class NameBadge extends LitElement {
  @property() first = '';
  @property() last = '';
  private initials = '';

  willUpdate(changed: PropertyValues<this>) {
    if (changed.has('first') || changed.has('last')) {
      this.initials = letter(this.first) + letter(this.last);
    }
  }

  render() {
    return html`<b>${this.initials}</b> ${this.first}`;
  }
}
```
```fsharp
open Fable.Core
open Firelight
open type Firelight.Lit

let letter (s: string) =
    if s = "" then "" else s.Substring(0, 1)

[<AttachMembers>]
type NameBadge() =
    inherit LitElement()

    let mutable initials = ""

    static member properties =
        PropertyDeclarations.create [
            "first", PropertyDeclaration<string>()
            "last", PropertyDeclaration<string>()
        ]

    member val first = "" with get, set
    member val last = "" with get, set

    override this.willUpdate(changed) =
        let has name = changed.ContainsKey name

        if has "first" || has "last" then
            initials <- letter this.first + letter this.last

    override this.render() =
        html $"<b>{initials}</b> {this.first}"

defineElement<NameBadge> "name-badge"
```
:::

`PropertyValues<this>` checks the names you pass to `has`. The F# keys are plain strings, and the
old values are `obj`: `unbox<string> changed["first"]`.

## After the first render

`firstUpdated` runs once the element's DOM exists, so it's where a ref first has a value.

::: compare
```ts
import { LitElement, html } from 'lit';
import { customElement, query } from 'lit/decorators.js';

@customElement('search-start')
export class SearchStart extends LitElement {
  @query('input') private input!: HTMLInputElement;

  firstUpdated() {
    this.input.focus();
  }

  render() {
    return html`<input placeholder="Search">`;
  }
}
```
```fsharp
open Fable.Core
open Browser.Types
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type SearchStart() =
    inherit LitElement()

    let input = createRef<HTMLInputElement> ()

    override _.firstUpdated(_) =
        input.value |> Option.iter _.focus()

    override _.render() =
        html $"""<input {ref input} placeholder="Search">"""

defineElement<SearchStart> "search-start"
```
:::

## Waiting for an update

`updateComplete` is a `JS.Promise<bool>`. Fable.Promise's `promise { }` waits for it with `let!`,
where TypeScript uses `await`.

::: compare
```ts
import { LitElement, html } from 'lit';
import { customElement, state } from 'lit/decorators.js';

@customElement('field-list')
export class FieldList extends LitElement {
  @state() private count = 1;

  private async add() {
    this.count++;
    await this.updateComplete;
    const all = this.renderRoot.querySelectorAll('input');
    all[all.length - 1].focus();
  }

  render() {
    const fields = Array.from(
      { length: this.count }, () => html`<input>`);
    return html`
      <button @click=${this.add}>Add</button>${fields}`;
  }
}
```
```fsharp
open Fable.Core
open Browser.Types
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type FieldList() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [
            "count", PropertyDeclaration<int>(state = true)
        ]

    member val private count = 1 with get, set

    member private this.FocusLast() =
        let all = this.queryAll<HTMLInputElement> "input"
        all.[all.Length - 1].focus ()

    member private this.Add() =
        this.count <- this.count + 1

        promise {
            let! _ = this.updateComplete
            this.FocusLast()
        }
        |> Promise.start

    override this.render() =
        let add _ = this.Add()

        let fields =
            List.init this.count (fun _ -> html $"<input>")

        html $"""
        <button @click={add}>Add</button>{fields}"""

defineElement<FieldList> "field-list"
```
:::

## Every callback

| Lit | Firelight |
|---|---|
| `connectedCallback()`, with `super.connectedCallback()` | `override this.connectedCallback()`, with `base.connectedCallback ()` |
| `disconnectedCallback()` | `override this.disconnectedCallback()`, with `base.disconnectedCallback ()` |
| `attributeChangedCallback(name, old, value)` | `override this.attributeChangedCallback(name, old, value)`; old and new values are `string option` |
| `shouldUpdate(changed)` | `override _.shouldUpdate(changed)`, returning a `bool` |
| `willUpdate(changed)` | `override this.willUpdate(changed)` |
| `update(changed)`, with `super.update(changed)` | `override this.update(changed)`, with `base.update changed` |
| `render()` | `override this.render()` |
| `firstUpdated(changed)` | `override this.firstUpdated(changed)` |
| `updated(changed)` | `override this.updated(changed)` |
| `getUpdateComplete()` | `override this.getUpdateComplete()` |
| `scheduleUpdate()` | `override this.scheduleUpdate()`, returning an `obj` (see below) |
| `performUpdate()`, with `super.performUpdate()` | `override this.performUpdate()`, with `base.performUpdate ()` |
| `createRenderRoot()` | `override this.createRenderRoot()`, returning an `obj`, such as `this`; or inherit [`LightDomElement`](/from-lit/styles/#rendering-without-shadow-dom) |
| `requestUpdate()` | `this.requestUpdate ()` |
| `updateComplete`, `hasUpdated`, `isUpdatePending` | The same names, as members |

## No direct equivalent

- **Arrow-function fields.** F# compiles a `let` function or a `member val` holding a function to
  a method, and passes a new wrapper each time it's used. `removeEventListener` then can't find
  the listener you added, so the listener stays. Add it with `Ev.listen`, which returns its own
  remover, as `EscNotice` does.
- **`await super.scheduleUpdate()` in a deferred update.** `scheduleUpdate` returns an `obj`,
  Lit's `void | Promise<unknown>`: return `box` of a promise to make `updateComplete` wait for it.
  F# doesn't allow `base` inside a lambda or a `promise { }` ("'base' is being used. This is only
  allowed in the direct implementation of members since they could escape their object scope"),
  so call the base method through a member of your own, such as
  `member this.ScheduleNow() = base.scheduleUpdate ()`, from inside the promise.
- **`PropertyValues<this>`.** The keys of `changed` are unchecked strings.
- **`async` and `await`.** Use Fable.Promise's `promise { }`, with `let!` for `await`. F#'s own
  `async` isn't a promise.

## Mistakes that compile

| You wrote | What happens | Write instead |
|---|---|---|
| `window.removeEventListener ("keydown", this.OnKey)` | The listener stays: each use of `this.OnKey` is a new function | `Ev.listen`, as above |
| No `base.connectedCallback ()` | Lit doesn't create the shadow root or start updating, so nothing renders | Call `base` first |
| `changed.ContainsKey "frist"` | Never true | The property's exact name |

The [Lifecycle guide](/guides/lifecycle/) covers the Firelight side in more depth.
