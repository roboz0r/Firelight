---
title: Drag to reorder
tagline: reorder a list with native drag and drop
description: "Reorder a Firelight list by dragging, with the browser's drag and drop events typed by Ev.drag, repeat to move each item's element, and buttons that do the same from the keyboard."
section: cookbook
order: 130
summary: "Reorder a list by dragging, or with buttons from the keyboard"
lead: "Build a list whose items can be dragged into a new order, with Move up and Move down buttons that do the same from the keyboard."
links:
  - text: "MDN: HTML Drag and Drop API"
    href: https://developer.mozilla.org/en-US/docs/Web/API/HTML_Drag_and_Drop_API
---

Drag Walk the dog to the top of the list. Or Tab to its ↑ button and press Enter a few times.

::: example Snippets/DragReorder.fs
<my-reorder-list></my-reorder-list>
:::

## How it works

The order is an F# list, and `moveTo` is a plain function that returns the list with one step
moved. Dragging and the buttons both end in `Move`, which calls it.

- **Dragging.** `draggable="true"` lets an item be picked up. Its `dragstart` records which step
  is moving, and `drop`, on the item it lands on, moves it to that item's place. `Ev.drag` types
  each handler's event as a `DragEvent`, so `e.dataTransfer` needs no cast.
- **Allowing the drop.** An element only accepts a drop if its `dragover` handler calls
  `preventDefault`, which is the browser's way of saying "drop here". Without it, `drop` never
  fires.
- **Firefox.** Firefox doesn't start a drag that carries no data, so `dragstart` puts the step's
  text in `dataTransfer`, though nothing reads it.
- **Keyed.** [`repeat`](/guides/templates/#keyed-lists-with-repeat) keys each item by its id, so
  each `<li>` moves with its step. With `List.map`, each element would stay put and show another
  step's text.
- **The keyboard.** Dragging needs a pointer, so each item also has ↑ and ↓ buttons, named "Move
  Walk the dog up" and so on for screen readers. Moving an element in the DOM takes the focus off
  the button inside it, so `Nudge` puts the focus back once the update has rendered. At the top or
  the bottom, a button is `aria-disabled` rather than disabled, so it keeps the focus.
- **Saying what happened.** The `role="status"` line announces the new position, for anyone who
  can't see the list move.

Touch screens don't send these drag events. For dragging on phones, listen for `pointerdown`,
`pointermove` and `pointerup` instead, or use a library built on them. The buttons work everywhere.

The [Kanban demo](/demos/kanban/) drags cards between columns the same way, with the drag state in
its Elmish model.

## Related

- [Events: Typed handlers with Ev](/guides/events/#typed-handlers-with-ev), for `Ev.drag` and the
  other event types.
- [Animating list changes](/cookbook/animate-list/), to slide the items to their new places.
