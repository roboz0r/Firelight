---
title: Firelight compared
pageTitle: "Firelight compared with Fable.Lit, Feliz, Sutil and Lit"
description: "How Firelight compares with Fable.Lit, Feliz, Sutil and Lit in TypeScript: what you write, what runs in the browser, how each works with other frameworks, and when each is the better choice."
section: start
order: 20
summary: "Firelight beside Fable.Lit, Feliz, Sutil and Lit in TypeScript"
eyebrow:
  text: Why Firelight
  href: /#why
lead: "In Firelight you write components as F# classes with HTML templates, and they run as Lit web components. This page sets Firelight beside the other ways to build a browser UI in F# (Fable.Lit, Feliz and Sutil) and beside Lit in TypeScript. Each section ends with when the other choice is the better one."
toc: true
---

<!--
  Sources, checked on 3 October 2026:
  - Fable.Lit: https://github.com/fable-compiler/Fable.Lit (last commit on main 2022-09-05; not
    archived; 11 open issues and 5 open pull requests), https://www.nuget.org/packages/Fable.Lit (1.4.2, 2022-07-14),
    https://www.nuget.org/packages/Fable.Lit.Elmish (1.4.0, 2021-11-13, depends on Fable.Elmish
    3.1.0 or later), docs in docsrc/docs/ of the repository (getting-started.md: "Fable.Lit packages
    require fable 3.6 dotnet tool and Lit 2"; hook-components.md; web-components.md), and
    src/Lit.React/Lit.React.fs (calls ReactDom.render and unmountComponentAtNode, which React 19
    removed: https://react.dev/blog/2024/04/25/react-19-upgrade-guide#removed-deprecated-react-dom-apis).
  - Feliz: https://github.com/fable-hub/Feliz (last commit 2026-09-25),
    https://www.nuget.org/packages/Feliz (3.3.3, 2026-05-18), its README and getting-started docs.
  - Sutil: https://github.com/davedawkins/Sutil (README; last commit 2026-10-02),
    https://www.nuget.org/packages/Sutil (2.0.16, 2024-09-22); WebComponent.fs in the package.
  - React custom elements: https://react.dev/blog/2024/12/05/react-19 ("Custom Elements Support").
  - Lit: https://lit.dev/ ("around 5 KB (minified and compressed)").
  - NuGet total downloads, from https://azuresearch-usnc.nuget.org/query?q=packageid:<id>:
    Feliz 1,267,712; Fable.Lit 54,493; Sutil 33,659; Firelight 1,204. Firelight's first listed
    release on NuGet was published on 2025-07-12.

  Counter sizes, measured on 3 October 2026. Each app is the counter shown on this page, one
  button that counts its clicks, in a project of its own: an index.html with one module script,
  built with Vite 8.3.1 (`vite build`, no config), Node 22.14.0. The F# apps were compiled with
  Fable 5.18.0 (`dotnet fable . -o build -c Release`). The
  size is the gzipped size of every .js file in dist/, with Node's zlib at its default level, as
  site/build-demos.mjs measures the demos. Each app was then opened in Chromium and clicked twice
  to check that it works. The code on this page for each library is the measured source, so the
  `fragment` blocks were compiled and run too, though Docs.Snippets doesn't compile them.
  - Firelight at cdcb0f0 (ProjectReference to src/Firelight), lit 3.3.1: 15,588 bytes, 6,006 gzipped.
  - Fable.Lit 1.4.2, lit 3.3.1: 30,635 bytes, 10,820 gzipped.
  - Feliz 3.3.3, Fable.Browser.Dom 2.20.0, react and react-dom 19.3.0: 227,588 bytes, 70,286 gzipped.
  - Sutil 2.0.16: 79,913 bytes, 22,548 gzipped.
  - Lit 3.3.1 in TypeScript (type-checked with tsc 5.9.3, bundled by Vite): 15,429 bytes, 5,929 gzipped.
-->

Firelight, Fable.Lit, Feliz and Sutil are all compiled to JavaScript by
[Fable](https://fable.io/). What differs is what you write, what ships to the browser, and how
the result fits next to other web code.

## At a glance

| Library | You write | A component is | A counter ships |
|---|---|---|---|
| Firelight | HTML in F# strings | A class; a custom element | 6.0 kB |
| [Fable.Lit](#fable-lit) | HTML in F# strings | A function with hooks; optionally a custom element | 10.8 kB |
| [Feliz](#feliz) | F# lists of elements | A function with hooks; a React component | 70.3 kB |
| [Sutil](#sutil) | F# lists of elements | A function with stores | 22.5 kB |
| [Lit](#lit-in-typescript) | HTML in TypeScript strings | A class; a custom element | 5.9 kB |

The sizes are for the same app built with each: one button that counts its clicks, as shown in
each section below. Each was built with Vite and measured as the gzipped size of all its
JavaScript, on 3 October 2026. They show the least each one ships, not what a real app costs. For comparison, the [Todo demo](/demos/todo/), with Elmish and context,
ships <span data-demo-size="todo">–</span>, Lit included.

This is the Firelight counter:

```fsharp
open Fable.Core
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type ClickCounter() =
    inherit LitElement()

    static member properties = PropertyDeclarations.create [ "count", PropertyDeclaration<int>() ]

    member val count = 0 with get, set

    override this.render() =
        html $"""<button @click={fun _ -> this.count <- this.count + 1}>Clicked {this.count} times</button>"""

defineElement<ClickCounter> "click-counter"
```

Firelight is also the newest of the F# options, first released in July 2025, and the least
downloaded. On NuGet, on 3 October 2026, Feliz had about 1.27 million downloads in total,
Fable.Lit 54,000, Sutil 34,000 and Firelight 1,200.

## Fable.Lit {#fable-lit}

[Fable.Lit](https://github.com/fable-compiler/Fable.Lit) is the earlier set of F# bindings for
Lit, and the closest alternative. Its templates are the same as Firelight's: Lit's `html`, with
the HTML in an F# interpolated string.

```fsharp fragment
open Lit

[<LitElement("click-counter")>]
let ClickCounter () =
    let _ = LitElement.init ()
    let count, setCount = Hook.useState 0
    html $"""<button @click={Ev(fun _ -> setCount (count + 1))}>Clicked {count} times</button>"""
```

The components differ. A Fable.Lit component is a function: `[<LitElement>]` registers it as a
custom element, and it keeps state in hooks modelled on React's, such as `Hook.useState` and
`Hook.useEffect`. `[<HookComponent>]` gives a template function state without making it an
element, and Fable.Lit.Elmish adds `Hook.useElmish`. A Firelight component is a class that
inherits `LitElement`, the shape Lit's own documentation uses, so Lit's
[lifecycle methods](https://lit.dev/docs/components/lifecycle/),
[reactive controllers](https://lit.dev/docs/composition/controllers/) and examples carry over
directly. Elmish runs in a controller, [`ElmishController`](/packages/elmish/).

Event handlers are close. Fable.Lit's `Ev` wraps a handler so F# can type its event, and `EvVal`
passes it an input's value. Firelight's [`Ev` module](/guides/templates/#typed-event-handlers)
has a function per kind of event, such as `Ev.keyboard`, and `Ev.value` for an input's value. In
both, nothing checks the handler against the event's name.

Fable.Lit hasn't changed since 2022. Its last release, 1.4.2, came out on 14 July 2022, and the
last commit to its main branch is from 5 September 2022 (both checked on 3 October 2026). Its
documentation asks for Fable 3.6 and Lit 2, and Fable.Lit.Elmish was built against Elmish 3. The
repository isn't archived, and the counter above still compiles with Fable 5.18 and runs on
Lit 3.3.1. Firelight targets Fable 5, Lit 3 and Elmish 5, and binds Lit's companion packages:
[context](/packages/context/), [signals](/packages/signals/), [task](/packages/task/),
[motion](/packages/motion/), [observers](/packages/observers/) and
[virtualizer](/packages/virtualizer/).

Fable.Lit has things Firelight doesn't. Fable.Lit.React renders React components inside Lit
templates and Lit templates inside React, though it calls `ReactDOM.render`, which React 19
removed, so it needs React 18 or earlier. Fable.Lit.Test has helpers for testing components.
With `Hook.useHmr`, a hook component keeps its state when Vite reloads its module; Firelight's
nearest equivalent saves an Elmish model to local storage across reloads.

Choose Fable.Lit for an app that already uses it and works, or if you'd rather write components
as functions with hooks than as classes. Moving an app to Firelight keeps most of the template
HTML, since both use Lit's syntax. The components need rewriting as classes, and Fable.Lit's F#
helpers, such as `Lit.classes` and `Lit.mapUnique`, become Lit's directives (`classMap`,
`repeat`).

## Feliz (React) {#feliz}

[Feliz](https://github.com/fable-hub/Feliz) is F# bindings for React. You build elements from F#
lists, and a component is a function marked `[<ReactComponent>]` that keeps its state in React's
hooks.

```fsharp fragment
open Feliz
open Browser.Dom

[<ReactComponent>]
let ClickCounter () =
    let count, setCount = React.useState 0

    Html.button [
        prop.onClick (fun _ -> setCount (count + 1))
        prop.text $"Clicked {count} times"
    ]

let root = ReactDOM.createRoot (document.getElementById "root")
root.render (ClickCounter())
```

The lists are typed. Feliz checks element and attribute names, and many CSS values, when you
compile, and `prop.onKeyDown` hands its handler a `KeyboardEvent`. A Firelight template is HTML
in a string, so the compiler checks the F# in its holes, not the HTML around them.
`<input placeholdr="Name">` compiles, and the box shows no placeholder; in Feliz,
`prop.placeholdr` is a compile error. Firelight's `Ev` functions type a handler's event, but
nothing checks that type against the event's name. Feliz.UseElmish runs an Elmish loop inside a
component.

What runs in the browser is React and React DOM. The counter ships 70.3 kB, with React 19.3. A
React component renders into a React root, which can be one part of a page built some other way.
A web component needs no root: it works in any HTML page and inside any framework. Since React 19,
React sets a prop on a custom element as a property when the element has a property of that name,
and as an attribute otherwise
([React 19 release notes](https://react.dev/blog/2024/12/05/react-19), 5 December 2024), so
Firelight components can also be used inside a Feliz app.

Feliz is actively maintained: 3.3.3 came out on 18 May 2026, and its repository had commits in
September 2026. It also has by far the most NuGet downloads of the F# options.

Choose Feliz when you want React's ecosystem of component libraries, many of which have Feliz
bindings, when your team already knows React, or when you'd rather have typed element lists than
HTML.

## Sutil {#sutil}

[Sutil](https://github.com/davedawkins/Sutil) builds elements from F# lists too, and keeps state
in stores: observables bound to the parts of the DOM that show them, with no virtual DOM. It is
modelled on Svelte, supports Elmish, and has no JavaScript dependencies; its runtime is written
in F#.

```fsharp fragment
open Sutil
open Sutil.CoreElements

let clickCounter () =
    let count = Store.make 0

    Html.button [
        disposeOnUnmount [ count ]
        Ev.onClick (fun _ -> count |> Store.modify (fun n -> n + 1))
        Bind.el (count, fun n -> text $"Clicked {n} times")
    ]

Program.mount ("sutil-app", clickCounter ()) |> ignore
```

The counter ships 22.5 kB, which is Sutil's runtime compiled from F#. This app is mounted into a
page element, and Sutil can also register a component as a custom element with
`WebComponent.Register`.

Sutil's last release, 2.0.16, came out on 22 September 2024. Its repository has had commits as
recently as 2 October 2026.

Choose Sutil when you want a UI framework written entirely in F#, with no JavaScript framework
underneath, and state that updates the DOM through stores.

## Lit in TypeScript {#lit-in-typescript}

Firelight components are Lit components, so this is the closest comparison: the same runtime,
and the same components once compiled. The TypeScript counter ships 5.9 kB and the Firelight one 6.0 kB. Firelight's
bindings compile away, and the components import Lit directly.

```ts
import { LitElement, html } from "lit";

class ClickCounter extends LitElement {
  static properties = { count: { type: Number } };
  declare count: number;

  constructor() {
    super();
    this.count = 0;
  }

  render() {
    return html`<button @click=${() => this.count++}>Clicked ${this.count} times</button>`;
  }
}

customElements.define("click-counter", ClickCounter);
```

What F# adds is the model behind the UI: records and discriminated unions for state, pattern
matching in templates, Elmish, and one language shared with a .NET back end. Immutable values
also suit Lit's change detection, which compares by identity (the
[Templates guide](/guides/templates/#common-mistakes) explains).

Choose Lit in TypeScript when your team writes TypeScript, or when you're publishing components
for JavaScript developers, who can then read the source they run. Lit's own documentation,
examples and editor tools are written for TypeScript and JavaScript; with Firelight you read them
and translate.

## What to weigh before choosing Firelight {#before-choosing}

- Firelight hasn't reached 1.0, so its API may still change.
- It is new, so most of what's written about it is on this site and in its repository.
- Templates are strings. The compiler checks the F# in the holes, not the HTML around them, and a
  hole accepts any value.
- You need both the .NET SDK and Node, as with any Fable app.
