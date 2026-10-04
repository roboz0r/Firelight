---
title: From Lit
tagline: Lit TypeScript and Firelight F# side by side
description: "For developers who know Lit: each topic of Lit's documentation, with the TypeScript you know next to the Firelight F# that does the same."
section: from-lit
order: 0
lead: "For developers who know Lit. Each page takes a topic from Lit's documentation, in its order, and puts the TypeScript you know next to the F# that does the same thing."
links:
  - text: "Lit docs"
    href: https://lit.dev/docs/
---

Firelight binds Lit itself, so the runtime is the same: the same `LitElement`, the same update
cycle, the same template syntax. What changes is the language around it. These are the
differences you meet on every page:

- A component is an F# class that inherits `LitElement`, marked `[<AttachMembers>]` so that
  [Fable](https://fable.io/) puts its members on the JavaScript class, where Lit looks for them.
- F# has no decorators. Registration is a call to `defineElement`, and property options go in a
  `static member properties`, as in Lit without decorators.
- A template hole is `{value}` in an F# interpolated string, where TypeScript has `${value}`.
- Members name their own `this`: `override this.render() = ...`.
- Imports become `open` declarations. `open type Firelight.Lit` brings in `html`, `css`, `nothing`
  and the directives.

Each pair of code blocks does exactly the same thing, with Lit's TypeScript on the left (or first,
on a narrow screen) and Firelight's F# on the right. The TypeScript uses Lit's decorators with
`experimentalDecorators: true` and `useDefineForClassFields: false`, the settings Lit's own
examples assume. Each page ends with what doesn't carry over directly, and with the Lit features
Firelight doesn't bind yet.

## Topics

- [Components](/from-lit/components/): defining, rendering and reactive properties.
- [Styles](/from-lit/styles/): static styles, sharing them, and shadow DOM.
- [Lifecycle](/from-lit/lifecycle/): connecting, the update cycle and `changedProperties`.
- [Events](/from-lit/events/): listening, dispatching custom events and listening on the element.
- [Decorators](/from-lit/decorators/): what each Lit decorator becomes in F#.
- [Templates](/from-lit/templates/): expressions, conditionals, lists, directives and static
  values.
- [Composition](/from-lit/composition/): reactive controllers, and what replaces mixins.
- [Context](/from-lit/context/): providing and consuming values with `@lit/context`.
- [Tasks](/from-lit/tasks/): async work with `@lit/task`.
- [Signals](/from-lit/signals/): shared reactive state with `@lit-labs/signals`.

## Beyond these pages

Some of Lit's documentation needs no F# version, or has none yet:

- **Server rendering.** Lit SSR runs in Node, so it can prerender Firelight components at build
  time, as this site does; see [Prerendering](/guides/prerendering/). A .NET server can't render
  them.
- **Localization.** `@lit/localize` isn't bound. Its tooling reads TypeScript or JavaScript
  source, not F#.
- **React.** `@lit/react` isn't bound. Firelight components are custom elements, so any
  framework that supports custom elements can use them.
- **Tools.** Vite, bundling and publishing work as in Lit; Fable compiles the F# to JavaScript
  modules first. [Get started](/start/) sets this up.
