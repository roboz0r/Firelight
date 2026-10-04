---
title: Context
tagline: "@lit/context in F#"
description: "Lit context in TypeScript and Firelight F# side by side: creating a context, providing a value and consuming it, with Firelight.Context."
section: from-lit
order: 80
summary: "Providing and consuming values with @lit/context"
lead: "`@provide` and `@consume` become `ContextProvider` and `ContextConsumer`, the controllers that `@lit/context` also exports. A context is a symbol branded with the type of its value, so a consumer gets that type back."
links:
  - text: "Lit docs: Context"
    href: https://lit.dev/docs/data/context/
  - text: "Package: Firelight.Context"
    href: /packages/context/
toc: true
---

## Providing and consuming

The provider sets a new value with `setValue`, where the decorated field is assigned. To let
other code set it, as a public `@provide` field allows, give the F# class a property whose setter
calls `setValue`. The consumer's `value` is an option: `None` until a provider answers.

::: compare
```ts
import { LitElement, html } from 'lit';
import { customElement } from 'lit/decorators.js';
import {
  consume, createContext, provide,
} from '@lit/context';

type Units = 'metric' | 'imperial';
const unitsContext = createContext<Units>(Symbol('units'));

@customElement('units-provider')
export class UnitsProvider extends LitElement {
  @provide({ context: unitsContext })
  private units: Units = 'metric';

  private toggle() {
    this.units =
      this.units === 'metric' ? 'imperial' : 'metric';
  }

  render() {
    return html`
      <button @click=${this.toggle}>Switch units</button>
      <slot></slot>`;
  }
}

@customElement('run-distance')
export class RunDistance extends LitElement {
  @consume({ context: unitsContext, subscribe: true })
  units?: Units;

  render() {
    const label =
      this.units === 'imperial' ? '3.1 mi' : '5 km';
    return html`${label}`;
  }
}
```
```fsharp
open Fable.Core
open Fable.Core.JsInterop
open Firelight
open Firelight.Context
open type Firelight.Lit

type Units =
    | Metric
    | Imperial

type UnitsContext =
    inherit Context<Units>
    inherit symbol

let unitsContext: UnitsContext =
    LitContext.createContext (JS.Symbol "units")

[<AttachMembers>]
type UnitsProvider() =
    inherit LitElement()

    let mutable units = Metric

    let provider =
        ContextProvider(
            jsThis,
            ContextProvider.Options(unitsContext, units)
        )

    member private _.Toggle() =
        units <- if units = Metric then Imperial else Metric
        provider.setValue units

    override this.render() =
        let toggle _ = this.Toggle()

        html $"""
        <button @click={toggle}>Switch units</button>
        <slot></slot>"""

[<AttachMembers>]
type RunDistance() =
    inherit LitElement()

    let units =
        ContextConsumer(
            jsThis,
            ContextConsumer.Options(
                unitsContext, subscribe = true)
        )

    override _.render() =
        let label =
            if units.value = Some Imperial then "3.1 mi"
            else "5 km"

        html $"{label}"

defineElement<UnitsProvider> "units-provider"
defineElement<RunDistance> "run-distance"
```
:::

The F# context's type lists `Context<Units>` and `symbol`, which `createContext<Units>` does for
TypeScript. Both classes take the element as their host, here as `jsThis`, the JavaScript `this`
of the object being constructed. They also accept the F# `this`, with `as this` on the type.

The provider here doesn't show the value itself, so it doesn't call `this.requestUpdate()`. A
provider that does show it needs to, as `setValue` only updates the consumers.

## Without decorators

Lit code that already uses the controllers translates line by line:

| Lit | Firelight |
|---|---|
| `new ContextProvider(this, { context, initialValue })` | `ContextProvider(jsThis, ContextProvider.Options(context, initialValue))` |
| `provider.setValue(v)` | `provider.setValue v` |
| `new ContextConsumer(this, { context, subscribe: true })` | `ContextConsumer(jsThis, ContextConsumer.Options(context, subscribe = true))` |
| `{ context, callback: (v, unsubscribe) => ... }` | `ContextConsumer.Options(context, callback = fun v unsubscribe -> ...)` |
| `consumer.value` | `consumer.value`, a `'T option` |
| `new ContextRoot().attach(document.body)` | `ContextRoot().attach document.body` |

## No direct equivalent

- **`@provide` and `@consume`.** F# has no decorators; use the controllers.
- **`createContext<T>(key)`.** The value type comes from an interface you declare, which inherits
  `Context<'T>` and `symbol`.

The [Firelight.Context](/packages/context/) page has live examples, including a `ContextRoot`, and the
[Todo demo](/demos/todo/) shares its Elmish state and `dispatch` through context.
