---
title: Firelight.Context
tagline: share state across a component tree
description: "Share a value with every component in a subtree, without passing it through each layer. F# bindings for Lit's @lit/context."
section: packages
order: 20
summary: Share state across a component tree without passing it through every layer
lead: Implicitly share values with any descendant component in the DOM tree, without prop drilling.
nuget: Firelight.Context
links:
  - text: "Lit docs: Context"
    href: https://lit.dev/docs/data/context/
  - text: Source
    href: https://github.com/roboz0r/Firelight/tree/main/src/Firelight.Context
---

## Why use it

Some values are needed deep in the component tree: the current theme, the active user, or an
Elmish `dispatch` function. Passing them down as properties forces every component in between to
accept and forward values it doesn't use.

Context solves this with an event-based protocol that runs through the DOM:

- **No intermediate plumbing:** a provider component declares a value, and any descendant component
  can request it. The components in between don't know the value passes through them.
- **Subtree scoping:** unlike global stores or signals, context follows the DOM hierarchy. Nest
  providers to scope a value locally, for example a dark theme for a single card while the rest of
  the page stays light.
- **Typed keys:** a Firelight context key is branded with the value's F# type, so a consumer that
  requests a `Theme` receives a `Theme`. When the provider's value changes, subscribed consumers
  re-render.

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
