---
title: Search as you type
tagline: a debounced search box with Firelight.Task
description: "A Firelight search box that searches as you type, waiting for a pause before it sends a request and dropping requests for text that has since changed, with Firelight.Task."
section: cookbook
order: 30
summary: "Search after a pause in typing, and drop requests for old text"
lead: "Build a search box that waits for a pause in typing before it searches, and ignores the results of searches for text that has since changed."
links:
  - text: "Lit docs: Async tasks"
    href: https://lit.dev/docs/data/task/
---

Type "berry" at your usual speed. The status line counts the requests: one for the whole word,
not one for each letter. Type slowly, with a pause after each letter, and each pause sends one.

::: example Snippets/DebouncedSearch.fs ssr=false
<my-debounced-search></my-debounced-search>
:::

The demo isn't prerendered: `LitTask` starts its first run as the component first updates in the
browser, so the first render there differs from the build's. See [Prerendering: Opting a component
out](/guides/prerendering/#opting-a-component-out).

## How it works

A [`LitTask`](/packages/task/) runs its task function each time its arguments change, here at each
keystroke. When a new run starts, the task aborts the signal it gave the run before, and it keeps
only the newest run's result.

That gives a debounce in two lines. The task function waits 300 ms, then calls
`options.signal.throwIfAborted ()`. If another key was pressed in the meantime, a newer run has
aborted this one's signal, so it throws and never sends its request. Only a run that was left alone
for 300 ms gets through, and there's no timer to clear.

A task doesn't abort its run when the component leaves the page, so a search that was already
waiting still goes out. That's harmless here; [Fetch JSON](/cookbook/fetch-json/) aborts it, for
requests that cost something.

The rest:

- An empty box gets an empty list straight away, as `U2.Case1`, with no wait and no request.
  `render` shows the prompt instead of a count while the box is empty.
- `render` reads `matches.status` for the message and `matches.value` for the list. The value
  keeps the last results while the next search runs, so the list doesn't flash empty at each
  keystroke.
- The message is in a `role="status"` paragraph that is always on the page, so screen readers
  announce the count without moving focus from the box.
- `search` stands in for your API. With a real request, pass the signal on, so that a search that
  is already running is cancelled when the text changes. `fetch`'s option for it is `signal`;
  [Fetch JSON](/cookbook/fetch-json/) shows the binding.

Choose the delay by how expensive a search is: around 300 ms feels immediate when typing stops,
and still saves most requests.

## Related

- [Firelight.Task](/packages/task/): the package and its status renderer.
- [Templates: Typed event handlers](/guides/templates/#typed-event-handlers) for `Ev.value`.
