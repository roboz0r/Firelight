---
title: Firelight.Virtualizer
tagline: long lists in Lit components
description: "Render long lists by keeping only the visible items in the DOM. F# bindings for @lit-labs/virtualizer."
section: packages
order: 90
summary: Long lists that only render the visible items
lead: Render long lists quickly by keeping only the visible items in the DOM.
links:
  - text: "Lit Labs: @lit-labs/virtualizer"
    href: https://github.com/lit/lit/tree/main/packages/labs/virtualizer
  - text: NuGet
    href: https://www.nuget.org/packages/Firelight.Virtualizer
  - text: Source
    href: https://github.com/roboz0r/Firelight/tree/main/src/Firelight.Virtualizer
---

## Why use it

A list of ten thousand rows is ten thousand DOM elements, which are slow to create and make the
page heavy to scroll. The virtualizer renders only the items in view, plus a few either side, and
reuses them as you scroll, so the cost stays about the same however long the list gets.

Use the `Virtualizer.virtualize` directive inside your own scrolling element, as below,
or register the `<lit-virtualizer>` element with `Virtualizer.defineElement ()`.
A handle from `Virtualizer.get` can scroll to any item, even one that isn't rendered yet.

## Example

Ten thousand rows. Open your browser's developer tools on the list: only the rows near the visible ones exist in the DOM.

::: example Snippets/BigList.fs ssr=false
<my-big-list></my-big-list>
:::

## Install

```sh
dotnet add package Firelight.Virtualizer
npm install @lit-labs/virtualizer
```
