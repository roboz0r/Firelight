---
title: Client-side routing
pageTitle: Client-side routing with Firelight.Router
description: "A live demo of Firelight.Router: URL patterns matched to an F# route type, without reloading the page."
section: demos
order: 20
spa: true
eyebrow:
  text: Firelight.Router
  href: /packages/router/
lead: >-
  Follow the links below. The address changes and the back button works, but the page never
  reloads: the router matches each address against URL patterns and turns it into an F#
  `Route` value, which the component renders.
---

::: demo Routing/RouteExplorer.fs .routed ssr=false
<fl-route-explorer></fl-route-explorer>
:::

The "Go to user" button does the same from code, with `routing.Navigate`. Try reloading the page
on one of the routes, or editing the number in the address bar.

## How it works

::: example Routing/RouteExplorer.fs
:::

## Why only this page?

The rest of this site is ordinary HTML pages with components placed in them. That suits a content
site: every page loads directly, and search engines and link previews see the text without running
any JavaScript.

Client-side routing suits applications, where the page stays open and state lives in memory.
It needs the server to return the app for every address it handles, not just the first one.
GitHub Pages can't be configured to do that, so this site copies this page to
`404.html`. Opening or reloading one of the routes above gets GitHub's 404 response,
which is this page, and the router then renders the right route.
