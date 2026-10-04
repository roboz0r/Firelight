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
task function also receives an `AbortSignal` you can pass to `fetch`, so requests that are no
longer needed get cancelled ([Cancelling requests](#cancelling-requests)). The F# type is called
`LitTask` so it doesn't clash with .NET's `Task`.

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

## Cancelling requests

The task function's second argument has one member, `signal`. The task aborts it when the run is
no longer wanted: its arguments changed and a newer run started, or you called `abort`. Pass it to
the request, and the browser cancels the request.

Its type is Fable.Fetch's `AbortSignal`. Firelight.Task depends on
[Fable.Fetch](https://github.com/fable-compiler/fable-fetch), the binding for the browser's
`fetch`, so with `open Fetch` the signal goes straight to `fetch`'s `Signal` option:

```fsharp
open Fable.Core
open Fetch
open Firelight.Task

let loadText (url: string) (signal: AbortSignal) =
    async {
        let! response = fetch url [ Signal signal ] |> Async.AwaitPromise
        return! response.text () |> Async.AwaitPromise
    }
    |> Async.StartAsPromise

let loadPage = TaskFunction(fun (args: string[]) options -> U2.Case2(loadText args[0] options.signal))
```

The value is the browser's own `AbortSignal` object, so any other API that takes a signal accepts
it too. In a binding of your own, such as a hand-written `fetch`, type the parameter as Fable.Fetch's
`AbortSignal`:

```fsharp
open Fable.Core
open Firelight.Task

[<Global>]
let fetch (url: string, init: {| signal: Fetch.Types.AbortSignal |}) : JS.Promise<obj> = jsNative

let loadPage = TaskFunction(fun (args: string[]) options -> U2.Case2(fetch (args[0], {| signal = options.signal |})))
```

Where another library's binding has an `AbortSignal` type of its own, convert at the call with
`unbox options.signal`, which compiles to nothing.

Work that isn't a request can call `options.signal.throwIfAborted ()` after each wait, as
[Search as you type](/cookbook/debounced-search/) does, or read `options.signal.aborted`.

## Install

```sh
dotnet add package Firelight.Task
npm install @lit/task
```

The first command also installs Fable.Fetch.
