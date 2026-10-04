---
title: App architecture and loading
tagline: structuring a larger app and loading it fast
description: "Structure a larger Firelight app: plain F# models, template functions and a few components, where each kind of state lives, routing, and a first screen that loads fast without server rendering."
section: guides
order: 110
summary: "Structure a larger app, place its state, and load it fast without SSR"
lead: "Build a larger app from a few components that own state and many template functions that show it. Keep the model in plain F#, keep each piece of state where the fewest components need it, and serve a static HTML shell that shows a loading screen while the code and the data download side by side."
links:
  - text: Kanban source
    href: https://github.com/roboz0r/Firelight/tree/main/sample/Kanban
  - text: "web.dev: App architecture"
    href: https://web.dev/learn/pwa/architecture
toc: true
---

Firelight doesn't prescribe an app structure. A component is a custom element, a template is a
value, and an Elmish model is plain F#, so an app's structure is the usual F# one: types first,
logic that uses them next, the user interface last. This guide follows the
[Kanban demo](/demos/kanban/), the largest sample in the repository.

## The shape of an app

The Kanban board is one component and a set of template functions. Its files, in compile order:

| File | Holds | Uses Lit |
|---|---|---|
| `KanbanModel.fs` | The types, `init` and `update` | No |
| `Persistence.fs` | Encoding the model for `localStorage` | No |
| `Templates.fs` | Template functions for the board, columns, cards and dialogs | Yes |
| `KanbanApp.fs` | The `<kanban-app>` component, which runs the Elmish loop | Yes |

F# compiles files in order, and a file can only use what comes before it. So the model can't
depend on the views, and `update` is plain F# with no DOM in it. `Kanban.Tests` tests `update`
without a browser, and the development tool `DevTools.withLocalStorage` can keep the board across
reloads, because the model is plain data that encodes to JSON.

Most of the user interface is template functions, which take the model, or the part they show,
and `dispatch`. They need no registration, no properties and no events. Make a part a component
when it needs something a function can't have:

- its own lifecycle, such as a chart or map that must be created and destroyed (see
  [Using JavaScript libraries](/guides/js-libraries/));
- state that only it uses and nothing else reads, such as whether its menu is open;
- a tag that other code, or plain HTML, uses;
- styles isolated in a shadow root.

A larger app often has a few such components, each with its own Elmish loop for a part of the app
that stands alone, such as a settings panel next to the main view. They talk through properties
and events; [Component communication](/guides/communication/) covers the choices.

## Where state lives

Put each piece of state where the fewest components need it, and no further up:

| State | Where it lives | Example |
|---|---|---|
| What the app is about | The Elmish model of the component at the top | Kanban's columns and cards |
| One widget's own state | A reactive property of that component | Whether a menu is open |
| A value a subtree reads | [Context](/packages/context/) | The theme, the signed-in user |
| A value unrelated parts read | [Signals](/packages/signals/) | A count in the header |
| Where the user is | The address, through [the router](/packages/router/) | `/boards/42` |
| What the browser already tracks | The DOM | Focus, scroll position, a text box's selection |

The model holds data, not objects with behaviour. A DOM element, a library's chart or an open
connection belongs to the component that created it, in a private field. Data in the model can be
compared, tested, encoded and replayed; objects in it can't.

Keep state that only a view uses in the model too when it decides what's shown. Kanban's model
records the card being dragged and the dialog being edited, so `update` decides what happens when
a card is dropped, and a test can check it.

## Routing

The page the user is on is state that lives in the address, so links can be shared and the back
button works. `RouterController` in the component at the top keeps a route value in step with the
address, and `render` matches on it:

```fsharp
open Fable.Core
open Browser.Types.URLPattern
open Firelight
open Firelight.Router
open type Firelight.Lit

importPolyfill ()

type Page =
    | Boards
    | Board of id: string
    | NotFound

let private board (result: URLPatternResult) =
    match result.pathname.groups["id"] with
    | Some id -> Board id
    | None -> NotFound

[<AttachMembers>]
type BoardsApp() as this =
    inherit LitElement()

    let routing =
        RouterController(this, createRouter NotFound [ "/", (fun _ -> Boards); "/boards/:id", board ])

    override _.render() =
        match routing.route with
        | Boards -> html $"<board-list></board-list>"
        | Board id -> html $"""{keyed (id, html $"<board-page board-id={id}></board-page>")}"""
        | NotFound -> html $"<h1>Not found</h1>"
```

The route is an F# union, so the compiler warns when a page isn't handled. Each page can be a
template that reads the shared model, or a component that loads its own data, for example with
[Firelight.Task](/packages/task/). [Firelight.Router](/packages/router/) covers patterns and links.

Going from `/boards/42` to `/boards/43` renders the same template with a new id, so Lit keeps the
`<board-page>` element and only changes its attribute. A page that loads its data once, when it's
created, would go on showing board 42. `keyed` makes a new element when the id changes. It returns
a directive result rather than a template, so it goes in a hole of a template of its own. The other
way is a page that reloads whenever its id changes, as a task does when its arguments change.

The server must answer every route the app handles with the app's `index.html`. Otherwise a link
to `/boards/42`, or a reload there, gets the server's 404 page instead of the app. Most hosts have
a setting for this. GitHub Pages has none, but serves `404.html` for unknown paths, with a 404
status; this site carries its client-side routing demo in that file.

## Loading fast without server rendering

An app can put a loading screen in front of the user as soon as its HTML arrives, and its real
first screen as soon as the code and data are both in, with no server rendering. The browser shows
HTML before any JavaScript has run, and it can download the code and the data at the same time.
The steps:

1. **Serve a small HTML shell** with the loading screen inside the app's tag, styled by CSS in
   the page: inline SVG and CSS animation, no JavaScript.
2. **Start every download at once.** A few lines of script in the `<head>` request the data that
   the first screen needs, while the browser fetches the app's module.
3. **Let the app take over.** When its module runs, the component shows the loading screen
   through a `<slot>` until the data arrives, then renders the app in its place.

Here is the shell. Vite adds the module's chunks to the `<head>` when it builds, as
`modulepreload` links, so they download together:

```html
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Boards</title>
  <style>
    .loading { display: grid; place-items: center; min-height: 100vh; font: 1rem system-ui, sans-serif; }
    .loading svg { animation: spin 1s linear infinite; }
    @keyframes spin { to { transform: rotate(1turn); } }
  </style>
  <script>
    // Starts now, while the app's code downloads.
    window.boardRequest = fetch("/api/board")
      .then((r) => (r.ok ? r.json() : Promise.reject(new Error(r.statusText))));
    // Marks a failure as handled until the app reads it; the app still sees the error.
    window.boardRequest.catch(() => {});
  </script>
  <script type="module" src="./build/App.js"></script>
</head>
<body>
  <board-app>
    <div class="loading" role="status">
      <svg width="32" height="32" viewBox="0 0 32 32" aria-hidden="true">
        <circle cx="16" cy="16" r="12" fill="none" stroke="currentColor" stroke-width="4" stroke-dasharray="50 26"/>
      </svg>
      Loading your board…
      <noscript>This app needs JavaScript.</noscript>
    </div>
  </board-app>
</body>
</html>
```

The app's `init` picks up the request that is already on its way, rather than starting one:

```fsharp
open Fable.Core
open Elmish
open Firelight
open Firelight.Elmish
open type Firelight.Lit

/// The board as the API sends it: a typed view of the parsed JSON.
type BoardJson =
    abstract title: string
    abstract cards: string[]

type Model =
    | Loading
    | Loaded of title: string * cards: string list
    | Failed of string

type Msg =
    | Received of BoardJson
    | LoadFailed of exn

/// The request that index.html started before this code arrived.
[<Global("boardRequest")>]
let boardRequest: JS.Promise<BoardJson> = jsNative

let init () =
    Loading, Cmd.OfPromise.either (fun () -> boardRequest) () Received LoadFailed

let update msg model =
    match msg with
    | Received board -> Loaded(board.title, List.ofArray board.cards), Cmd.none
    | LoadFailed e -> Failed e.Message, Cmd.none

[<AttachMembers>]
type BoardApp() as this =
    inherit LitElement()

    let elmish = ElmishController.withCmds this init update

    override _.render() =
        match elmish.model with
        | Loading -> html $"<slot></slot>"
        | Loaded(title, cards) ->
            html $"""<h1>{title}</h1><ul>{cards |> List.map (fun c -> html $"<li>{c}</li>")}</ul>"""
        | Failed message -> html $"""<p role="alert">The board didn't load: {message}</p>"""

defineElement<BoardApp> "board-app"
```

While the model is `Loading`, the component renders a
[`<slot>`](https://lit.dev/docs/components/shadow-dom/#slots), which shows the elements written
inside `<board-app>`: the loading screen from the HTML. Once the board arrives, it renders the
board instead, and the loading screen, no longer slotted, disappears. The loading screen the user
sees before the code runs and after it is the same element, so it isn't replaced or restarted
when the component takes over.

`[<Global("boardRequest")>]` refers to the global variable the script set. `BoardJson` is an
interface over the parsed JSON: it types the fields, but nothing checks that the server sent
them. Decode with a library such as Thoth.Json when the data's shape isn't guaranteed.

Some details decide whether the shell really shows at once:

- **Stylesheets.** A `<link rel="stylesheet">` in the `<head>` holds back the first paint, the
  loading screen included, until it has downloaded. A component's `styles` come with its
  JavaScript, so a Firelight app often has no stylesheet to wait for. If yours imports CSS files,
  Vite links them in the `<head>`: keep them small, and keep the loading screen's own styles inline.
- **Failures before the app runs.** The `.catch` line in the script marks a failed request as
  handled, so the browser doesn't report it as unhandled before the app reads it; the app still
  gets the error. If the app's module itself fails to load, the loading screen stays. The
  `<noscript>` message covers browsers with JavaScript turned off.
- **Content Security Policy.** A policy that forbids inline scripts blocks the request script, and
  the app then fails to find `boardRequest`. Allow the script by its hash or a nonce, or move it to
  a small file of its own.

This takes the place of server-side rendering for an app. Lit SSR can render Firelight components,
but only in Node: a .NET backend can't run it without a Node process beside it. And for an app,
the first screen usually depends on who is signed in, so a server would render it for every
request, then hydrate it in the browser, where any difference between the two is a bug. The shell
is static and the same for everyone, so any host or CDN can serve it.

## When to prerender at build time

Prerendering runs Lit SSR once, when the site is built, and saves each page's HTML. It's the right
tool when the content is known at build time and the same for every visitor: documentation,
marketing pages, a blog, the public pages in front of an app. This site is built that way. Every
page, and most demos, are HTML before any JavaScript runs, so the pages read without JavaScript,
search engines and link previews see their content, and a prerendered demo keeps its place on the
page when it hydrates.

It isn't worth it for screens built from the user's data. At build time there is no user, so a
prerendered app screen could only show a loading state, which the shell already does with less
machinery. Prerendering also constrains the components: they must render in Node, and render the
same thing first in the browser as on the server.

An app with public pages can do both: prerender the public pages, and serve the app itself from a
shell. The Prerendering guide covers how. <!-- link: /guides/prerendering/ -->

## Beyond the browser

A Firelight app is a web app, so it runs wherever a web view does. Installed as a progressive web
app, it gets its own window and icon. Packaged with [Capacitor](https://capacitorjs.com/) for iOS
and Android, or [Tauri](https://tauri.app/) for desktop and mobile, it ships as an app, with
device features through their plugins. Frameworks that draw native controls instead of a DOM,
such as React Native, can't host web components.

## Common mistakes

These all compile; they show up as a slower or broken first screen:

| You wrote | What happens | Write instead |
|---|---|---|
| The data request in `init` | It starts only after the app's code has downloaded and run | Start it in the HTML, and pick it up in `init` |
| A spinner template in `render` while loading | The shell's loading screen vanishes and a different one replaces it | `<slot>`, to keep showing the shell's |
| The shell's loading screen inside a `LightDomElement` app | It stays on screen above the app, since nothing replaces the element's children | A shadow DOM component, or remove it on load |
| No fallback route on the server | A shared link or a reload gets a 404 | Serve `index.html` for the app's routes |
| A DOM element or library object in the model | Persistence, tests and replays break on it | A private field of the component that created it |
| A component for every template | More registrations, properties and events to keep in step | Template functions, and components only where needed |
