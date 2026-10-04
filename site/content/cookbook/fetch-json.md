---
title: Fetch JSON
tagline: load data with loading, error and retry states
description: "Load JSON with fetch in a Firelight component: a minimal fetch binding, a typed view of the JSON, cancellation with the task's AbortSignal, and loading, error and retry states with Firelight.Task."
section: cookbook
order: 40
summary: "Load JSON with `fetch`, with loading, error and retry states"
lead: "Build a component that loads a list from a JSON file with `fetch`, shows a loading message, the list or the error, and can load it again."
links:
  - text: "MDN: Using the Fetch API"
    href: https://developer.mozilla.org/en-US/docs/Web/API/Fetch_API/Using_Fetch
---

Tick the box to ask for a file that isn't there, and the error appears. Untick it, or press Load
again, to load the books.

::: example Snippets/FetchJson.fs ssr=false
<my-book-list></my-book-list>
:::

Like every `LitTask` demo, this one isn't prerendered: the task starts as the component first
updates in the browser, so its first render there shows "Loading" where the build's showed nothing.

## How it works

[Fable](https://fable.io/)'s browser bindings don't include `fetch`, so the module declares it:
`[<Global>]` on a `let` whose body is `jsNative` refers to the browser's own function, and the
`Response` interface types the three members used. The `Fable.Fetch` package has complete
bindings, if you'd rather not write your own.

- **The request.** `getBooks` is an F# `async` that awaits the promises `fetch` and `json` return.
  It checks `response.ok` first: `fetch` only fails when there's no response at all, and a 404 or
  a 500 arrives as an ordinary response.
- **The JSON.** `Book` is an interface over the parsed objects, so `b.title` compiles to plain
  property access. It types the fields but checks nothing: if the server sends something else,
  the mistake shows up wherever a field is used. When you don't control the server, decode the
  JSON with a library such as [Thoth.Json](https://thoth-org.github.io/Thoth.Json/), as the
  [Kanban sample](https://github.com/roboz0r/Firelight/blob/main/sample/Kanban/Persistence.fs) does.
- **Cancelling.** [`LitTask`](/packages/task/) passes each run an `AbortSignal`, and the request
  passes it on to `fetch`. When the arguments change while a request is running, the task aborts
  the signal, and the browser drops the old request.
- **The states.** `StatusRenderer` takes a function for each status: a loading message, the list,
  or the error. Its error is an `obj`, since JavaScript can throw anything; here it's always the
  `Error` that `failwith` or `fetch` made, so `unbox<exn>` reads its message.
- **Loading again.** `books.run ()` runs the task again with the same arguments, for an error
  that may not happen twice, such as a dropped connection. The button stays on the page in every
  state, so pressing it from the keyboard doesn't lose the focus.
- **Leaving the page.** A task doesn't abort its run when its component is removed, so
  `disconnectedCallback` calls `books.abort ()` if a request is still running. An element can be
  removed and added back, as when a list moves it, so `connectedCallback` runs the interrupted
  request again.

The address is relative, so the file is found next to this page. Point it at your API instead.

## With Elmish

In an app with an Elmish loop, start the same request from a command instead, with
`Cmd.OfPromise.either`, and keep the loading and error states in the model.
[App architecture: Loading fast](/guides/architecture/#loading-fast-without-server-rendering) shows
one.

## Related

- [Search as you type](/cookbook/debounced-search/): a task that waits for the user to stop typing.
- [Using JavaScript libraries: Typed bindings](/guides/js-libraries/#typed-bindings) for
  `[<Global>]` and interfaces over JavaScript objects.
