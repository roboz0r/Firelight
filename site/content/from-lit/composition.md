---
title: Composition
tagline: reactive controllers, and what replaces mixins
description: "Lit composition in TypeScript and Firelight F# side by side: reactive controllers, and base classes or controllers where Lit uses mixins."
section: from-lit
order: 70
summary: "Reactive controllers, and what replaces mixins"
lead: "A reactive controller is an F# class that implements `ReactiveController` and adds itself to its host, as in Lit. Mixins don't carry over: F# can't build a class from a function, so shared behaviour goes in a base class or a controller."
links:
  - text: "Lit docs: Controllers"
    href: https://lit.dev/docs/composition/controllers/
  - text: "Lit docs: Mixins"
    href: https://lit.dev/docs/composition/mixins/
  - text: "Lit docs: Component composition"
    href: https://lit.dev/docs/composition/component-composition/
toc: true
---

Composing components, with child elements in a template, properties passed down and events sent
up, works as in Lit: see [Components](/from-lit/components/) and [Events](/from-lit/events/).

## Reactive controllers

The F# controller implements all four callbacks, where TypeScript leaves out the ones it doesn't
need. `as this` lets the constructor add the controller to its host.

::: compare
```ts
import { LitElement, html } from 'lit';
import type {
  ReactiveController, ReactiveControllerHost,
} from 'lit';
import { customElement } from 'lit/decorators.js';

export class WidthController implements ReactiveController {
  width = 0;

  constructor(private host: ReactiveControllerHost) {
    host.addController(this);
  }

  private onResize = () => {
    this.width = window.innerWidth;
    this.host.requestUpdate();
  };

  hostConnected() {
    this.width = window.innerWidth;
    this.host.requestUpdate();
    window.addEventListener('resize', this.onResize);
  }

  hostDisconnected() {
    window.removeEventListener('resize', this.onResize);
  }
}

@customElement('width-label')
export class WidthLabel extends LitElement {
  private size = new WidthController(this);

  render() {
    const wide = this.size.width >= 600;
    const label = wide ? 'Wide' : 'Narrow';
    return html`${label} window`;
  }
}
```
```fsharp
open Fable.Core
open Browser
open Browser.Types
open Firelight
open type Firelight.Lit

type WidthController(host: ReactiveControllerHost) as this =
    let mutable width = 0.0
    let mutable stop = ignore

    do host.addController this

    member _.Width = width

    member private _.Listen() =
        let onResize (_: Event) =
            width <- window.innerWidth
            host.requestUpdate ()

        width <- window.innerWidth
        host.requestUpdate ()
        window.addEventListener ("resize", onResize)

        stop <- fun () ->
            window.removeEventListener ("resize", onResize)

    interface ReactiveController with
        member this.hostConnected() = this.Listen()
        member _.hostDisconnected() = stop ()
        member _.hostUpdate() = ()
        member _.hostUpdated() = ()

[<AttachMembers>]
type WidthLabel() as this =
    inherit LitElement()

    let size = WidthController(this)

    override _.render() =
        let wide = size.Width >= 600.0
        let label = if wide then "Wide" else "Narrow"
        html $"{label} window"

defineElement<WidthLabel> "width-label"
```
:::

The controller doesn't need `[<AttachMembers>]`: Lit calls only the interface's four members, and
an F# interface implementation keeps their names. The listener is created when the host connects
and its removal kept, as on [Lifecycle](/from-lit/lifecycle/#connecting-and-disconnecting).

## Mixins

A mixin adds the same members to classes with different bases. F# has one base class per class,
so a behaviour you'd write as one mixin becomes a base class. Several, combined freely, become
controllers.

::: compare
```ts
import { LitElement, css, html } from 'lit';
import { customElement, property } from 'lit/decorators.js';

type Constructor<T = {}> = new (...args: any[]) => T;

const Highlightable = <T extends Constructor<LitElement>>(
  Base: T,
) => {
  class Highlighted extends Base {
    static styles = css`
      :host([highlighted]) { background: #fff3bf; }
    `;

    @property({ type: Boolean, reflect: true })
    highlighted = false;
  }
  return Highlighted;
};

@customElement('tip-text')
export class TipText extends Highlightable(LitElement) {
  render() {
    return html`<slot></slot>`;
  }
}
```
```fsharp
open Fable.Core
open Fable.Core.JsInterop
open Firelight
open type Firelight.Lit

[<AbstractClass; AttachMembers>]
type Highlightable() =
    inherit LitElement()

    static member styles =
        css $$"""
        :host([highlighted]) { background: #fff3bf; }
        """

    static member properties =
        PropertyDeclarations.create [
            "highlighted",
            PropertyDeclaration<bool>(
                ``type`` = jsConstructor<Boolean>,
                reflect = true)
        ]

    member val highlighted = false with get, set

[<AttachMembers>]
type TipText() =
    inherit Highlightable()

    override _.render() = html $"<slot></slot>"

defineElement<TipText> "tip-text"
```
:::

A subclass can declare `static member properties` of its own; Lit merges them with the base
class's, as it does in TypeScript.

## No direct equivalent

- **Mixins.** No class expressions and no multiple inheritance. Use a base class for one
  behaviour, controllers for several.
- **Optional controller callbacks.** An F# interface implementation has every member; write
  `()` for the ones you don't need.
- **Lit's own mixins.** `SignalWatcher` is applied by `LitSignals.defineElement`; see
  [Signals](/from-lit/signals/).

Firelight packages bind several of Lit's controllers: `ContextProvider` and `ContextConsumer`
([Context](/from-lit/context/)), `Task` as `LitTask` ([Tasks](/from-lit/tasks/)), the resize,
intersection, mutation and performance controllers in
[Firelight.Observers](/packages/observers/), and the animation controllers in
[Firelight.Motion](/packages/motion/). The Controllers guide covers writing your own.
<!-- link: /guides/controllers/ -->
