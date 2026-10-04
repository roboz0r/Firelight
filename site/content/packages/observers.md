---
title: Firelight.Observers
tagline: resize, intersection and mutation controllers
description: "Reactive controllers for the browser's resize, intersection, mutation and performance observers. F# bindings for @lit-labs/observers."
section: packages
order: 70
summary: Reactive controllers for resize, intersection, mutation and performance observers
lead: Reactive controllers for the browser's resize, intersection, mutation and performance observers.
nuget: Firelight.Observers
links:
  - text: "Lit Labs: @lit-labs/observers"
    href: https://github.com/lit/lit/tree/main/packages/labs/observers
  - text: Source
    href: https://github.com/roboz0r/Firelight/tree/main/src/Firelight.Observers
---

## Why use it

The browser's observers report things a component often needs to know: its own size, whether it's
on screen, changes to its children. Using them directly means creating the observer when the
component connects, disconnecting it when it's removed, and asking Lit to render when something
changes.

Each controller here does that work. Give it a callback that turns the observer's entries into a
value, then read `.value` in `render`. CSS container queries can restyle a
component by its size; a `ResizeController` can also change what it renders.

## Example

The panel picks its layout from its own width, not the window's. Drag its bottom-right corner to resize it.

::: example Snippets/ResizePanel.fs
<my-resize-panel></my-resize-panel>
:::

## Observe an element in the template

A controller observes its host unless told otherwise. To observe an element the component renders,
give the controller `target = null` and mark the element with a hole in its opening tag:
`<div {controller.target ()}>`. The element is observed while it's rendered. Scroll the box until
the marker shows:

::: example Snippets/ScrollMarker.fs
<my-scroll-marker></my-scroll-marker>
:::

`ResizeController.target` is Lit's own directive. `@lit-labs/observers` has none for
`IntersectionController`, so Firelight's is Lit's `ref` with one callback per controller: it
observes the element a render puts there, and stops observing one a render removes. That makes it
one element per controller. Put `target ()` on two, and Lit moves the ref between them on every
render, so only the last is observed, and the component renders again and again. For several
elements, give each its own controller.

## Install

```sh
dotnet add package Firelight.Observers
npm install @lit-labs/observers
```
