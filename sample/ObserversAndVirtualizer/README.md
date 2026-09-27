# Observers and Virtualizer

This sample demonstrates the two binding packages together. `ResizeDemo` uses a
`ResizeController` to show its own width as the panel is resized.
`VirtualListDemo` renders 1,000 items with both the `virtualize` directive and
the `<lit-virtualizer>` custom element. The button uses `Virtualizer.get` and
`element(index).scrollIntoView()` to reach an item outside the current DOM range.

Run `npm install` and `npm run dev` from this directory, then open the Vite URL.

The directive must be used in a child expression, such as inside `<ul>`. Set a
height when `scroller = true`. Supply a `keyFunction` when items can have
duplicate values. Call `Virtualizer.defineElement()` before rendering the
custom element; the directive does not require registration.

`Firelight.Observers` also provides `MutationController`,
`IntersectionController`, and `PerformanceController`. Each accepts a typed
configuration object and an optional callback that computes its `value`. The
callback is called once for initial state unless `skipInitial = true`.
