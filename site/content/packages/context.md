---
title: Firelight.Context
tagline: share state across a component tree
description: "Share a value with every component in a subtree, without passing it through each layer. F# bindings for Lit's @lit/context."
section: packages
order: 20
summary: Share state across a component tree without passing it through every layer
lead: Share a value with every component inside a subtree, without passing it through each layer in between.
links:
  - text: "Lit docs: Context"
    href: https://lit.dev/docs/data/context/
  - text: NuGet
    href: https://www.nuget.org/packages/Firelight.Context
  - text: Source
    href: https://github.com/roboz0r/Firelight/tree/main/src/Firelight.Context
---

## Why use it

Some values are needed far from where they're owned: the current theme, the signed-in user,
an Elmish `dispatch` function. Passing them down as properties means every component
in between has to accept and forward them, even when it doesn't use them.

With context, a provider component holds the value and any descendant asks for it. The
components in between don't know it exists. When the provider's value changes, subscribed
consumers re-render. In Firelight the context key is branded with the value's F# type, so a
consumer that asks for a `Theme` gets a `Theme`.

## Example

The provider owns the theme and shows its children through a slot. The badges are nested inside
plain `<div>`s, and nothing passes the theme to them.

::: example Snippets/ThemeContext.fs
<my-theme-provider>
  <div class="nested">
    <div class="nested"><my-themed-badge></my-themed-badge></div>
    <my-themed-badge></my-themed-badge>
  </div>
</my-theme-provider>
:::

The [Todo demo](/demos/todo/) uses the same pattern to share its Elmish state and `dispatch` with each item.

## Install

```sh
dotnet add package Firelight.Context
npm install @lit/context
```
