---
title: Infinite scroll
tagline: load more as the list scrolls, with Firelight.Observers
description: "A Firelight list that loads its next page when its end scrolls into view, with an IntersectionController from Firelight.Observers, and a Load more button for keyboards and screen readers."
section: cookbook
order: 50
summary: "Load the next page when the end of a list scrolls into view"
lead: "Build a list that loads its next page when its end scrolls into view, with a Load more button that does the same for anyone who doesn't scroll."
links:
  - text: "MDN: Intersection Observer API"
    href: https://developer.mozilla.org/en-US/docs/Web/API/Intersection_Observer_API
---

Scroll to the bottom of the box, with the mouse or the arrow keys, and the next ten items arrive.
Or Tab to Load more and press Enter.

::: example Snippets/LoadMore.fs
<my-load-more></my-load-more>
:::

## How it works

An `IntersectionController` from [Firelight.Observers](/packages/observers/) wraps the browser's
`IntersectionObserver`: it starts observing when the component joins the page, stops when it
leaves, and asks the component to render when what it observes changes. Its callback turns the
observer's entries into a value, here whether the end of the list is in view, and calls `LoadMore`
when it is.

- **What to observe.** By default the controller observes the component itself. `target = null`
  starts it with nothing, and `firstUpdated` hands it the element after the list, through a
  [ref](/guides/templates/#directives), once it exists.
- **No root.** The observer's root is the window, but an element inside a scrolling box only counts
  as visible when it's scrolled into view in the box too. So the same code works for a box and
  for a whole page.
- **Observing again.** An observer reports changes. If the end of the list is still in view after
  a page arrives, as on a tall screen, nothing has changed and no report comes. `LoadMore` waits
  for the new items to render, then stops observing the end and starts again, which makes the
  observer report where it is now. It skips that if the component has left the page while the
  request was running, since leaving stopped the observer.
- **One request at a time.** The callback and the button both call `LoadMore`, which does nothing
  while `loading` is set.
- **The keyboard.** The box has `tabindex="0"`, so the arrow keys can scroll it, and a
  `role="region"` with a label, so a screen reader says what it is when it gets the focus.
- **The button.** Infinite scroll alone shuts out anyone who can't scroll the box, and a reader
  who tabs past the list never reaches its end. The button sits below the box, where new
  items don't push it out of sight, and it stays on the page to the end, with
  `aria-disabled` once everything has loaded. Disabling it, or removing it, would drop the focus.

The first page is in the component's initial state, so the [prerendered](/guides/prerendering/)
HTML shows it before any JavaScript runs.

Thousands of items, all loaded at once, are a different problem: render only the visible ones with
[Firelight.Virtualizer](/packages/virtualizer/).

## Related

- [Controllers](/guides/controllers/), for how the Observers controllers follow the lifecycle.
- [Fetch JSON](/cookbook/fetch-json/), for the request itself.
