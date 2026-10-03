---
title: Firelight.Motion
tagline: animate Lit components from F#
description: "Animate elements as they move, appear and disappear, declared in the template. F# bindings for @lit-labs/motion."
section: packages
order: 60
summary: Animate elements as they move, appear and disappear
lead: Animate elements as they move, appear and disappear, declared right in the template.
nuget: Firelight.Motion
links:
  - text: "Lit Labs: @lit-labs/motion"
    href: https://github.com/lit/lit/tree/main/packages/labs/motion
  - text: Source
    href: https://github.com/roboz0r/Firelight/tree/main/src/Firelight.Motion
---

## Why use it

When a render moves an element, it jumps to its new place. Animating that by hand means
measuring where the element was before the update and where it is after, then animating between
the two. `Motion.animate()` does that for you: put it on an element in a template, and
when a render moves the element, it slides from its old position to its new one.

It can also animate elements in and out of the page with keyframe presets such as
`Motion.fadeIn`. `AnimateController` controls the animations across a
component, for example to pause them, and `SpringController` adds spring physics.

## Example

The list is re-rendered in a random order. Each item is the same element as before, animated to its new place.

::: example Snippets/ShuffleList.fs
<my-shuffle-list></my-shuffle-list>
:::

## Install

```sh
dotnet add package Firelight.Motion
npm install @lit-labs/motion
```
