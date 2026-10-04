---
title: Firelight.Router
tagline: client-side routing with F# route types
description: "Client-side routing for Lit components on the browser's URL Pattern API, with routes as an F# union."
section: packages
order: 40
summary: Client-side routing on the URL Pattern API, with routes as an F# union
lead: Type-safe client-side routing powered by the browser's URL Pattern API, with routes modelled as an F# union.
nuget: Firelight.Router
links:
  - text: "MDN: URL Pattern API"
    href: https://developer.mozilla.org/en-US/docs/Web/API/URL_Pattern_API
  - text: Source
    href: https://github.com/roboz0r/Firelight/tree/main/src/Firelight.Router
---

## Why use it

Firelight.Router parses browser URLs into your own F# discriminated union. This turns page routing
into a strongly typed pattern match, and helps the compiler ensure every route is handled.

`RouterController` keeps your component synchronized with the browser location. It intercepts
clicks on matching links, manages back and forward history, and handles smooth scrolling for
`#hash` anchors. Unmatched links fall back to standard browser navigation.

Firelight.Router builds directly on the web standard
[URL Pattern API](https://developer.mozilla.org/en-US/docs/Web/API/URL_Pattern_API) (polyfilled
for browsers that don't yet support it natively).

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
