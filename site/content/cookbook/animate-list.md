---
title: Animating list changes
tagline: items that fade in, fade out and slide, with Firelight.Motion
description: "Animate a Firelight list with Firelight.Motion: new items fade in, the rest slide into place, with one AnimateController for shared timing and reduced motion."
section: cookbook
order: 140
summary: "Fade new items in, and slide the rest into place"
lead: "Build a list whose new items fade in and whose other items slide to their new places as items come and go, and that keeps still for anyone who prefers less motion."
links:
  - text: "Lit Labs: @lit-labs/motion"
    href: https://github.com/lit/lit/tree/main/packages/labs/motion
---

Add a few people, then remove one from the middle: the people below it slide up. If your system
is set to reduce motion, the list changes without animating.

::: example Snippets/AnimatedList.fs
<my-animated-list></my-animated-list>
:::

## How it works

`Motion.animate` from [Firelight.Motion](/packages/motion/) goes on each `<li>`, as an element
directive. At each render it measures where the element was and where it is now, and animates
between the two, so items slide when one above them is added or removed. The
[Motion package page](/packages/motion/) shows that part on its own; this recipe adds the items
that come and go.

- **In, but not out.** `in = Motion.fadeIn` plays when an element first appears. Motion also has
  `out`, for an element being removed: it puts the element back while it fades. Back in the list,
  it still takes up its place, so the items below it wait for the fade, then jump. Let a removed
  item go at once, and the others slide up into its place.
- **Not on the first render.** `skipInitial = true` stops the items that are already there from
  fading in when the page loads, including the [prerendered](/guides/prerendering/) ones, which
  are on screen before the component's code runs.
- **Keyed.** [`repeat`](/guides/templates/#keyed-lists-with-repeat) keeps each person's element
  with them. With `List.map`, the elements would stay put and swap their text, and there would be
  nothing to slide.
- **One controller.** `AnimateController` holds the options every `animate` in the component
  shares, here a 250 ms ease-out, so each item only says what's different about it.
- **Reduced motion.** `connectedCallback` reads the `prefers-reduced-motion` media query, sets the
  controller's `disabled`, which turns off every animation in the component, and listens for the
  setting to change while the page is open. Some people get motion sickness from movement on
  screen, and the list works the same without it.
- **The focus.** Removing an item removes its button, which had the focus, so `Remove` moves the
  focus to the next item's button, or to Add someone.

[Fable](https://fable.io/)'s browser bindings have no `matchMedia`, so the module declares it. The
[Controllers guide](/guides/controllers/#a-controller-that-reads-the-browser) wraps the same media
query in a controller that any component can reuse.

## Related

- [Drag to reorder](/cookbook/drag-reorder/), a list worth animating.
- [Firelight.Motion](/packages/motion/), the package.
