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

## Install

```sh
dotnet add package Firelight.Observers
npm install @lit-labs/observers
```
