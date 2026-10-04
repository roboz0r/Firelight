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
`#hash` anchors. Unmatched links fall back to standard browser navigation. `routing.Navigate`
does the same from code.

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

## Navigate from code

`routing.Navigate url` goes to `url` the way a click on a link to it does. When one of the
router's routes matches, the address changes, a history entry is added and the host renders the
new route, without loading a page. When none matches, the browser loads the address, as it does
for such a link. The example's "Go to user" button calls it.

| Call | What it does |
|---|---|
| `routing.Navigate "/users/42"` | Adds a history entry, so Back returns |
| `routing.Navigate ("/users/42", replace = true)` | Replaces the current entry, so Back skips it: after a form is saved, say |

The address may be relative to the current one. Going to the current address does nothing.

## Routes as Elmish messages

With [Firelight.Elmish](/packages/elmish/), keep the route in the model. Give the controller a
function as its third argument: it gets the route after every change (a link, Back or Forward,
`Navigate`) and when the host connects, so it can dispatch a message.

```fsharp
open Fable.Core
open Browser.Types.URLPattern
open Firelight
open Firelight.Elmish
open Firelight.Router
open type Firelight.Lit

// Before the router is created: browsers without URLPattern get the polyfill.
importPolyfill ()

type Route =
    | Home
    | User of id: int
    | NotFound

let router =
    [
        "/", (fun _ -> Home)
        "/users/:id(\\d+)", (fun (r: URLPatternResult) -> User(int (r.pathname.groups.["id"] |> Option.defaultValue "0")))
    ]
    |> createRouter NotFound

type Model = { Route: Route; Visits: int }

type Msg = RouteChanged of Route

let update msg model =
    match msg with
    | RouteChanged route -> { Route = route; Visits = model.Visits + 1 }

[<AttachMembers>]
type App() as this =
    inherit LitElement()

    let loop =
        ElmishController.simple this (fun () -> { Route = router.OfLocation(); Visits = 0 }) update

    let routing =
        RouterController(this, router, fun route -> loop.dispatch (RouteChanged route))

    override _.render() =
        match loop.model.Route with
        | Home -> html $"""<button @click={fun _ -> routing.Navigate "/users/1"}>First user</button>"""
        | User id -> html $"<p>User {id}, page {loop.model.Visits}</p>"
        | NotFound -> html $"<p>Not found</p>"
```

`update` stays pure: it only records the route it's given. To change the route as the result of a
message, call `routing.Navigate` from the component, where the controller is, rather than from
`update`.

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
