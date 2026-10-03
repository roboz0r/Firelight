---
title: Firelight
tagline: Lit web components in F#
description: "Firelight's core package: define Lit web components in F# with reactive properties, scoped styles and html templates."
section: packages
order: 10
summary: "Core bindings: `LitElement`, `html`/`css` templates, directives, reactive properties"
lead: "The core package: F# bindings for Lit. Define web components with reactive properties, scoped styles and `html` templates."
links:
  - text: "Lit docs: Components"
    href: https://lit.dev/docs/components/overview/
  - text: NuGet
    href: https://www.nuget.org/packages/Firelight
  - text: Source
    href: https://github.com/roboz0r/Firelight/tree/main/src/Firelight
---

## Why use it

Web components are the browser's own component model. A custom element works in any page,
alongside any framework or none. Lit takes away the boilerplate: you declare properties and
return a template, and Lit updates only the parts of the page that changed.

Firelight lets you write those components in F#. Templates are F# interpolated strings, so the
values and event handlers in them are ordinary typed F# code. The bindings themselves compile
away: your component imports Lit directly, and there is no Firelight runtime to ship.

## Example

A star rating. `value` is a reactive property that also reads and writes the
`value` attribute, the styles are scoped to the component, and choosing a star raises
a `rating-changed` event. The message below comes from plain JavaScript on this page
listening for that event.

::: example Snippets/Rating.fs .stacked
<my-rating value="3"></my-rating>
<p id="rating-message">Pick a rating.</p>
:::

<script type="module">
  const message = document.getElementById("rating-message");
  document.querySelector("my-rating").addEventListener("rating-changed", (e) => {
    message.textContent = `You picked ${e.detail} out of 5.`;
  });
</script>

## Install

```sh
dotnet add package Firelight
npm install lit
```

New to Firelight? The [Get started](/start/) tutorial creates an app from a template, then gives its component an attribute, an event, styles and a list.
