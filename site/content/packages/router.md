---
title: Firelight.Router
tagline: client-side routing with F# route types
description: "Client-side routing for Lit components on the browser's URL Pattern API, with routes as an F# union."
section: packages
order: 40
summary: Client-side routing on the URL Pattern API, with routes as an F# union
lead: Client-side routing on the browser's URL Pattern API, with your routes as an F# union.
nuget: Firelight.Router
links:
  - text: "MDN: URL Pattern API"
    href: https://developer.mozilla.org/en-US/docs/Web/API/URL_Pattern_API
  - text: Source
    href: https://github.com/roboz0r/Firelight/tree/main/src/Firelight.Router
---

## Why use it

In a single-page app the address should still say where the user is, so links can be shared,
a reload lands in the same place and the back button works. Firelight.Router matches the address
against URL patterns and turns the match into a value of your own route type, so rendering a page
is a `match`, and the compiler tells you when a route isn't handled.

`RouterController` keeps a component in step with the address. It handles clicks on
links that match one of its routes, the browser's back and forward buttons, and smooth scrolling
to `#hash` links. Links it has no route for navigate normally. It's built on the
standard [URL Pattern API](https://developer.mozilla.org/en-US/docs/Web/API/URL_Pattern_API),
with a polyfill for browsers that don't have it yet, rather than on `@lit-labs/router`.

## Example

Patterns can constrain their parameters: `:id(\d+)` only matches digits, so
`/users/abc` falls through to the catch-all `*` route and renders
`NotFound` without leaving the page.

::: example Routing/RouteExplorer.fs
:::

<p class="actions"><a class="button primary" href="/client-side-routing/">Try it on the client-side routing page</a></p>

## Install

```sh
dotnet add package Firelight.Router
npm install urlpattern-polyfill
```

Call `importPolyfill ()` once, at the top level of your app's module, before you create a router,
as the example does. A module that creates a router as it loads needs the call itself, above that
code, because the modules it imports run first. A browser with native `URLPattern` skips the
download. The call compiles to a top-level `await`, which JavaScript allows only at the top level:
inside a function, the compiled module fails to load. There, `loadPolyfill ()` returns a promise to
wait on instead.
