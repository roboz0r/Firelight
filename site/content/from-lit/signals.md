---
title: Signals
tagline: "@lit-labs/signals in F#"
description: "Lit signals in TypeScript and Firelight F# side by side: shared state with SignalWatcher, signal-aware templates, and update effects, with Firelight.Signals."
section: from-lit
order: 100
summary: "Shared reactive state with @lit-labs/signals"
lead: "`signal` and `computed` keep their names. `SignalWatcher(LitElement)` becomes a registration call, `LitSignals.defineElement`, because F# can't apply a mixin to a base class."
links:
  - text: "Lit docs: Signals"
    href: https://lit.dev/docs/data/signals/
  - text: "Package: Firelight.Signals"
    href: /packages/signals/
toc: true
---

## Watching signals

A component registered with `LitSignals.defineElement` re-renders when a signal it read in
`render` changes, as a `SignalWatcher` class does.

::: compare
```ts
import { LitElement, html } from 'lit';
import {
  SignalWatcher, computed, signal,
} from '@lit-labs/signals';

const count = signal(0);
const doubled = computed(() => count.get() * 2);

export class CountButton extends SignalWatcher(LitElement) {
  render() {
    const add = () => count.set(count.get() + 1);
    return html`
      <button @click=${add}>${count.get()}</button>`;
  }
}
customElements.define('count-button', CountButton);

export class CountDouble extends SignalWatcher(LitElement) {
  render() {
    return html`<p>Doubled: ${doubled.get()}</p>`;
  }
}
customElements.define('count-double', CountDouble);
```
```fsharp
open Fable.Core
open Firelight
open Firelight.Signals
open type Firelight.Lit

let count = LitSignals.signal 0
let doubled =
    LitSignals.computed (fun () -> count.get () * 2)

[<AttachMembers>]
type CountButton() =
    inherit LitElement()

    override _.render() =
        let add _ = count.set (count.get () + 1)
        html $"<button @click={add}>{count.get ()}</button>"

[<AttachMembers>]
type CountDouble() =
    inherit LitElement()

    override _.render() =
        html $"<p>Doubled: {doubled.get ()}</p>"

LitSignals.defineElement<CountButton> "count-button"
LitSignals.defineElement<CountDouble> "count-double"
```
:::

The two elements share no parent, property or event, only the signals. Options go in
`SignalOptions`: `signal(0, { equals: (a, b) => ... })` is
`signal (0, SignalOptions(equals = SignalEquals(fun a b -> ...)))`.

## Updating only what changed

The `html` from `@lit-labs/signals` watches each signal placed in a hole, so a change updates
that part of the template without running `render` again. In F# it's `LitSignals.html`, which
`open type LitSignals` brings into scope as `html`.

::: compare
```ts
import { LitElement } from 'lit';
import {
  SignalWatcher, html, signal,
} from '@lit-labs/signals';

const clicks = signal(0);

export class ClickPanel extends SignalWatcher(LitElement) {
  render() {
    const add = () => clicks.set(clicks.get() + 1);
    return html`
      <button @click=${add}>+1</button>
      <p>Clicks: ${clicks}</p>`;
  }
}
customElements.define('click-panel', ClickPanel);
```
```fsharp
open Fable.Core
open Firelight
open Firelight.Signals
open type LitSignals

let clicks = signal 0

[<AttachMembers>]
type ClickPanel() =
    inherit LitElement()

    override _.render() =
        let add _ = clicks.set (clicks.get () + 1)

        html $"""
        <button @click={add}>+1</button>
        <p>Clicks: {clicks}</p>"""

defineElement<ClickPanel> "click-panel"
```
:::

With Lit's own `html`, `watch` does the same for one hole: `{watch clicks}`, as `${watch(clicks)}`.

## Effects

`updateEffect` runs a function after the element's next update, and again whenever a signal it
read changes. It's a method of a `SignalWatcher` element in TypeScript; in F# it takes the element as
its first argument.

::: compare
```ts
import { LitElement, html } from 'lit';
import { SignalWatcher, signal } from '@lit-labs/signals';

const unread = signal(3);

export class TitleBadge extends SignalWatcher(LitElement) {
  connectedCallback() {
    super.connectedCallback();
    this.updateEffect(() => {
      document.title = `(${unread.get()}) Inbox`;
    });
  }

  render() {
    const read = () => unread.set(0);
    return html`<button @click=${read}>Mark read</button>`;
  }
}
customElements.define('title-badge', TitleBadge);
```
```fsharp
open Fable.Core
open Browser
open Firelight
open Firelight.Signals
open type Firelight.Lit

let unread = LitSignals.signal 3

[<AttachMembers>]
type TitleBadge() =
    inherit LitElement()

    override this.connectedCallback() =
        base.connectedCallback ()

        LitSignals.updateEffect (this, fun () ->
            document.title <- $"({unread.get ()}) Inbox")
        |> ignore

    override _.render() =
        let read _ = unread.set 0
        html $"<button @click={read}>Mark read</button>"

LitSignals.defineElement<TitleBadge> "title-badge"
```
:::

`updateEffect` returns a function that disposes the effect. Lit disposes it when the element
disconnects, so the F# side ignores it, as the TypeScript does.

## No direct equivalent

- **`SignalWatcher(...)`.** F# can't apply a mixin, so `LitSignals.defineElement` applies it when
  it registers the class. The class itself isn't a `SignalWatcher`, so a subclass of it registered
  with Firelight's plain `defineElement` doesn't watch signals.
- **`withWatch`.** Not bound. `LitSignals.html` and `LitSignals.svg` cover Lit's own tags.
- **`Signal.subtle`.** The polyfill's low-level API, such as `Signal.subtle.Watcher` for your own
  effects, isn't bound.

The [Firelight.Signals](/packages/signals/) page has a live example. `@lit-labs/signals` is a Lit
Labs package and still experimental.
