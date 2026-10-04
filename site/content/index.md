---
title: Web Components for F#
pageTitle: "Firelight: Web Components for F#"
description: Build standards-based web components in F# with Lit and Fable. Type-safe templates, Elmish, context and routing, compiled to lean JavaScript.
layout: home
lead: >-
  Firelight is a set of F# bindings for [Lit](https://lit.dev/), compiled with
  [Fable](https://fable.io/). Write type-safe components with reactive properties,
  templates and the Elmish loop, and ship standard custom elements that work in any page.
links:
  - text: Get started
    href: /start/
  - text: View on GitHub
    href: https://github.com/roboz0r/Firelight
---

## A component in a few lines {#examples}

The code below is the source file for the live component beside it. This site is built with Firelight.

::: example Snippets/Counter.fs
<my-counter></my-counter>
:::

### Why a class?

You write a component as an F# class with mutable properties, which can look out of place in F#.
But a custom element is a class: the browser creates it, calls its lifecycle methods and sets its
properties, and Lit's components extend `LitElement`. Firelight keeps that mapping direct: a
component is an ordinary F# class.

- **What you write is what runs.** There's no Firelight-specific compiler plugin or build step:
  `[<AttachMembers>]` tells Fable to put the members on the JavaScript class under their own
  names, where Lit looks for them.
- **Lit's features map directly.** Lifecycle callbacks, controllers and shadow DOM options are
  members you override or call on the class, so [Lit's documentation](https://lit.dev/docs/components/overview/)
  for them carries over.
- **The class is a thin shell.** Changing `this.count`, a declared reactive property, schedules a
  render. Keep domain logic in functions and state in immutable values; the next example moves the
  state into an Elmish loop.

## The Elmish loop, inside a component {.same-section}

`ElmishController` runs Model-View-Update inside a component. The update function
stays pure and every new model triggers a render.

::: example Snippets/ElmishCounter.fs
<my-elmish-counter></my-elmish-counter>
:::

## Why Firelight {#why}

::: cards
### Built on web standards

Components are native custom elements with Shadow DOM. Nothing takes over the page, so you can use them in plain HTML or alongside any other framework.

### Idiomatic F#

Records, discriminated unions and pattern matching drive the UI. Immutable models mean every change is seen by Lit's change detection.

### Works with other web components

Libraries like Web Awesome, Fluent UI and Carbon are also web components, so you use their tags directly in your templates without wrappers.

### No runtime of its own

The core bindings compile away: your components import Lit directly. The [Todo demo](/demos/todo/), Lit included, ships <span data-demo-size="todo">–</span> of gzipped JavaScript.
:::

## Packages {#packages}

Start with `Firelight` and add the others as you need them. Each page explains when you'd want the package and has a live example.

::: package-table
:::

## Demo apps {#demos}

::: cards
### [Todo](/demos/todo/)

An Elmish loop in the root component, with state and dispatch shared through context. <span data-demo-size="todo">–</span> of gzipped JavaScript.

### [Kanban](/demos/kanban/)

A board with drag and drop between columns, an edit dialog and Tailwind styles, saved to local storage as you go. <span data-demo-size="kanban">–</span> of gzipped JavaScript.

### [Client-side routing](/client-side-routing/)

URL patterns matched to an F# route type with `Firelight.Router`, and why the rest of this site doesn't use it.
:::

## Get started {#get-started}

Add the package to a Fable project, and install Lit from npm:

```sh
dotnet add package Firelight
npm install lit
```

New to Firelight? The [Get started](/start/) tutorial creates an app from a template,
then gives its component an attribute, an event, styles and a list.

<p class="notice">Firelight hasn't reached 1.0 yet, so its API may still change before then.</p>
