---
title: Firelight.Signals
tagline: shared reactive state for Lit components
description: "Share reactive state between any components on the page, with updates targeted to the template parts that use it. F# bindings for @lit-labs/signals."
section: packages
order: 50
summary: Shared reactive state that any component can read
lead: Reactive state that lives outside your components, shared by any component that reads it.
nuget: Firelight.Signals
links:
  - text: "Lit docs: Signals"
    href: https://lit.dev/docs/data/signals/
  - text: Source
    href: https://github.com/roboz0r/Firelight/tree/main/src/Firelight.Signals
---

## Why use it

Lit's context passes state through the DOM hierarchy with providers and consumers. Signals keep
reactive state outside the DOM.

A signal is a standalone value that tracks who reads it:

- **Subscriptions without wiring:** any component registered with `LitSignals.defineElement`
  re-renders when a signal it read while rendering changes. This needs no providers, context keys or
  manual subscriptions.
- **Derived state:** `computed` creates values from other signals. They stay up to date, compute
  lazily and cache their results.
- **Surgical updates:** with `LitSignals.html`, a signal interpolated into a template updates only
  that binding, instead of re-rendering the whole component (as long as the render doesn't also read
  the signal elsewhere).

Lit's signals are based on the TC39 Signals proposal and are still experimental.

## Example

The button and the total are separate elements on this page, with no shared parent, property or
event between them.

::: example Snippets/SharedSignal.fs .stacked
<my-click-button></my-click-button>
<my-click-total></my-click-total>
:::

## Install

```sh
dotnet add package Firelight.Signals
npm install @lit-labs/signals
```
