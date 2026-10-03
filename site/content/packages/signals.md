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

Context shares state down a tree of components. Signals share it with any component, anywhere on
the page. A signal is a value that keeps track of who reads it. Components registered with
`LitSignals.defineElement` re-render when a signal they read changes, and
`computed` derives values that stay up to date by themselves.

Updates can also be smaller than a whole render. Interpolate a signal into a template with
`LitSignals.html` and only that part of the template updates. Lit's signals are based on
the TC39 Signals proposal and are still experimental.

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
