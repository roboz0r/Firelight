---
title: Firelight.Task
tagline: async work in Lit components
description: "Run async work from a Lit component and render its pending, complete and error states. F# bindings for @lit/task."
section: packages
order: 80
summary: Async work with pending, complete and error states
lead: Run async work from a component and render its pending, complete and error states.
nuget: Firelight.Task
links:
  - text: "Lit docs: Async tasks"
    href: https://lit.dev/docs/data/task/
  - text: Source
    href: https://github.com/roboz0r/Firelight/tree/main/src/Firelight.Task
---

## Why use it

Loading data in a component takes more than making the request. You need a loading state, the
result or the error, a new request when an input changes, and a way to ignore an older request
that finishes after a newer one.

`LitTask` handles all of that. Give it a function that returns a promise and an
`args` function: the task runs whenever the arguments change, renders the component
again as its status changes, and `render` picks the template for the current status. The
task function also receives an `AbortSignal` you can pass to `fetch`, so
requests that are no longer needed get cancelled. The F# type is called `LitTask` so it
doesn't clash with .NET's `Task`.

## Example

Each button changes the task's argument, which starts a new request. Product 3 fails, to show the
error state. Clear sets the argument to an empty id, for which the task function returns
`initialState`: the task goes back to its initial state, which `render` shows with its `initial`
template, and no request starts.

<p class="notice">
  <code>args</code> must return a plain JavaScript array. <a href="https://fable.io/">Fable</a> compiles numeric arrays such as
  <code>int[]</code> to typed arrays (<code>Int32Array</code>), which <code>@lit/task</code> rejects,
  so this example uses string ids. Arrays of strings, records or <code>obj</code> are fine.
</p>

::: example Snippets/ProductTask.fs ssr=false
<my-product-view></my-product-view>
:::

## Install

```sh
dotnet add package Firelight.Task
npm install @lit/task
```
