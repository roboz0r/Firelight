---
title: Your first component
pageTitle: Get started with Firelight
description: "From an empty folder to a running Firelight component: create an app with dotnet new, then give its component a property, an event, styles and a list."
section: start
order: 10
lead: "From an empty folder to a web component written in F# and running in your browser. Then four small changes to it: an attribute, an event, styles and a list."
toc: true
---

<!--
  Firelight.Templates is first published with Firelight 0.3.0. The site launches with that release
  (PLAN.md, "Launch"), so `dotnet new install Firelight.Templates` works from the day this page is
  live. Before then, pack the template locally as templates/test-template.sh does.
  Snippets/Start/App.fs is a copy of templates/firelight-app/App.fs. check-template.mjs fails the
  build if they differ, or if the template's files, npm scripts or index.html no longer match
  what this page says about them.
-->

A Firelight component is a standard custom element. You write it in F#, the [Fable](https://fable.io/) compiler turns
it into JavaScript, and Lit runs it in the browser. A `dotnet new` template sets all of that up.

## What you need

- The [.NET 10 SDK](https://dotnet.microsoft.com/download) or later, for F# and the Fable compiler.
- [Node.js](https://nodejs.org/) 22.12 or later (20.19 or later on Node 20), for Lit and the Vite dev server.

Nothing runs on .NET in the finished app. What you ship is HTML and JavaScript.

## Create an app

Install the template, then create an app called MyApp from it:

```sh
dotnet new install Firelight.Templates
dotnet new firelight -n MyApp
cd MyApp
```

Then install the Fable compiler and the npm packages, and start the dev server:

```sh
dotnet tool restore
npm install
npm run dev
```

`npm run dev` compiles your F# to JavaScript, then starts Vite, which prints a local address
(`http://localhost:5173/` unless that port is taken). Open it: you'll see the heading "MyApp" and a
button that counts your clicks. Leave it running. When you save an F# file, Fable recompiles it
and the page reloads.

## What's in it

| File | What it's for |
| --- | --- |
| `App.fs` | The component, in F#. |
| `MyApp.fsproj` | The F# project: its source files, and the `Firelight` package. Fable reads it to know what to compile. |
| `index.html` | The page. It uses the component as `<click-counter>` and loads `build/App.js`, which Fable compiles from `App.fs`. |
| `package.json` | The npm side: Lit, Vite, and the `dev`, `build` and `preview` scripts. |
| `vite.config.js` | Settings for Vite, which serves the page while you work and bundles it for production. |
| `.config/dotnet-tools.json` | The version of the Fable compiler, which `dotnet tool restore` installs. |

There's also a `README.md` and a `.gitignore`. Fable writes its JavaScript to `build/`, and the
production build goes to `dist/`.

## The component

`App.fs` defines one component: a button that counts clicks. Here it is, running.

::: example Snippets/Start/App.fs
<click-counter></click-counter>
:::

From the top:

- `ClickCounter` inherits `LitElement`, Lit's base class for components. `[<AttachMembers>]` tells
  Fable to compile its members onto the JavaScript class, where Lit looks for them.
- `properties` declares `count` as a reactive property: setting it re-renders the component.
  `member val count = 0` holds its value, starting at 0.
- `styles` is the component's CSS. There's more on styles [below](#style-it).
- `render` returns the component's HTML, written as an F# interpolated string. `{this.count}`
  inserts a value, and `@click={...}` makes an F# function the click handler. When `count`
  changes, Lit updates only the text that shows it.
- `defineElement` registers the class as the `<click-counter>` element, which is how `index.html`
  can use it.

`css` strings start with `$$"""`, so that single braces are CSS; to insert an F# value there, write
`{{value}}`. `html` strings start with `$"""`, so `{...}` inserts a value.

The steps below each change `App.fs` and show the whole file, running. Two lines differ from
yours: all the steps are compiled together and run on this one page, so each needs its own module
name (the first line) and tag (the last). Keep `module MyApp.App` and `"click-counter"` in your copy.

## Set it from an attribute

HTML elements are configured with attributes, and components can be too. Lit links each reactive
property to an attribute of the same name, so a `label` property can be set from the page. Add it
to `properties`, give it a default with `member val`, and show it in the template:

::: example Snippets/Start/Label.fs .stacked
<click-counter-label label="Liked"></click-counter-label>
<click-counter-label></click-counter-label>
:::

Then set it in `index.html`:

```html
<click-counter label="Liked"></click-counter>
```

Without the attribute, `label` keeps its default, as in the second counter above. Each element has
its own state, so the two count separately.

Attribute values are text. For a number or a Boolean, declare the property's type so Lit converts
the attribute, as the [star rating](/packages/firelight/#example) does for its `value`.

## Tell the page when it changes

Properties and attributes carry data into a component. To send data out, a component raises a DOM
event, just as built-in elements do. Anything on the page can listen, with no reference to your F#
code. Here, each click raises a `count-changed` event with the new count as its `detail`:

::: example Snippets/Start/Events.fs .stacked
<click-counter-events label="Liked"></click-counter-events>
<p id="count-message">The page hasn't heard from the counter yet.</p>
:::

<script type="module">
  const countMessage = document.getElementById("count-message");
  document.querySelector("click-counter-events").addEventListener("count-changed", (e) => {
    countMessage.textContent = `The page heard count-changed: ${e.detail}.`;
  });
</script>

The click handler now calls `Increment`, which updates `count` and then dispatches the event.
`Event.customEvent` (from `open Browser.Types`) creates an event that bubbles and crosses the shadow
DOM boundary, so listeners outside the component hear it.

The message under the counter comes from plain JavaScript on this page. To do the same, add this to
`index.html`, below the counter:

```html
<p id="message">The page hasn't heard from the counter yet.</p>
<script type="module">
  const message = document.getElementById("message");
  document.querySelector("click-counter").addEventListener("count-changed", (e) => {
    message.textContent = `The page heard count-changed: ${e.detail}.`;
  });
</script>
```

## Style it

A component's `styles` apply inside its shadow DOM, and only there. Its rules don't reach the rest
of the page, and the page's rules don't reach in, so the component looks the same wherever it's
used. Inherited properties such as `font` and `color` do pass through, and so do CSS custom
properties, which is how a page can theme a component.

This version gives the button a colour, read from a `--counter-color` custom property with
`rebeccapurple` as the default:

::: example Snippets/Start/Styles.fs .stacked
<click-counter-styles label="Liked"></click-counter-styles>
<click-counter-styles label="Starred" style="--counter-color: #0b6e4f"></click-counter-styles>
<button>A button on the page</button>
:::

The component's `button` rule styles only its own button. The third button belongs to the page, so
the rule doesn't touch it. The second counter sets `--counter-color` in its `style` attribute, and
that does reach the button inside. Try both in `index.html`:

```html
<click-counter label="Starred" style="--counter-color: #0b6e4f"></click-counter>
<button>A button on the page</button>
```

## Render a list

A template can contain a list of templates, so repeated markup comes from F# data and the list
functions you already use. Here, a row of buttons comes from a list of step sizes:

::: example Snippets/Start/List.fs
<click-counter-list label="Liked"></click-counter-list>
:::

`steps` is a plain F# list. `stepButton` makes a button template for one step, `List.map` turns
the list of steps into a list of buttons, and Lit renders them in order. `Increment` becomes
`Add`, which takes the step. The new `:host` rule styles the `<click-counter>` element itself,
laying out the label and buttons in a row.

For a list whose items are added, removed or reordered, Lit's
[`repeat` directive](https://lit.dev/docs/templates/lists/#the-repeat-directive) keeps each item's
DOM with it.

## Build for production

```sh
npm run build
```

This compiles `App.fs` and bundles the app into `dist/`: static files you can host anywhere.
`npm run preview` serves them locally, to check the build before you publish it.

## Where next

- The [Templates guide](/guides/templates/): the F# side of the `html` you've been writing.
- [Firelight](/packages/firelight/), the core package, and the [other packages](/#packages): Elmish,
  context, signals, routing and more, each with a live example.
- The [demos](/#demos): a todo app, a Kanban board and [client-side routing](/client-side-routing/).
- The [GettingStarted sample](https://github.com/roboz0r/Firelight/tree/main/sample/GettingStarted)
  on GitHub covers each core concept one module at a time: rendering, properties, events, styles,
  controllers, context and Elmish.
- [Lit's documentation](https://lit.dev/docs/). What it says about components, templates and
  styles applies to Firelight too.
