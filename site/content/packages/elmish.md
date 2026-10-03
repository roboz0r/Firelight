---
title: Firelight.Elmish
tagline: Model-View-Update inside a Lit component
description: "Run an Elmish Model-View-Update loop inside a Lit web component: one immutable model, one pure update function."
section: packages
order: 30
summary: Run an Elmish (Model-View-Update) loop inside a component
lead: "Run the Model-View-Update loop inside a component: one immutable model, one pure `update` function."
nuget: Firelight.Elmish
links:
  - text: Elmish docs
    href: https://elmish.github.io/elmish/
  - text: "Lit docs: Reactive controllers"
    href: https://lit.dev/docs/composition/controllers/
  - text: Source
    href: https://github.com/roboz0r/Firelight/tree/main/src/Firelight.Elmish
---

## Why use it

As a component's state grows, mutable fields changed from different event handlers get hard to
follow. Elmish keeps all of the state in one immutable model, and every change goes through a single
`update` function: given a message and the current model, it returns the next model.
That function is plain F#, with no DOM or Lit in it, so it's easy to read and to test.

`ElmishController` connects the loop to a Lit component. It's a
[reactive controller](https://lit.dev/docs/composition/controllers/): `init` runs when the
component is created, and each new model renders the component. Use
`ElmishController.withCmds` for side effects such as HTTP requests, or pass a full
Elmish `Program` for subscriptions and tracing.

## Example

Because every model is an immutable value, undo is just a list of the earlier ones. Nothing in the
component has to know how to reverse a change.

::: example Snippets/UndoCounter.fs
<my-undo-counter></my-undo-counter>
:::

For a larger example, the [Todo demo](/demos/todo/) runs one Elmish loop and shares its state with each item through context.

## Install

```sh
dotnet add package Firelight.Elmish
```
