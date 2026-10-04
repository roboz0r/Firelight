---
title: Tasks
tagline: "@lit/task in F#"
description: "Lit's Task in TypeScript and Firelight F# side by side: loading data when arguments change, rendering each status, and running a task by hand."
section: from-lit
order: 90
summary: "Async work with @lit/task"
lead: "`new Task(this, { ... })` becomes `LitTask(this, TaskConfig(...))`, named so it doesn't clash with .NET's `Task`. The task function returns a promise, and `render` picks the template for the task's status."
links:
  - text: "Lit docs: Async tasks"
    href: https://lit.dev/docs/data/task/
  - text: "Package: Firelight.Task"
    href: /packages/task/
toc: true
---

## Loading when arguments change

The task runs again when `args` returns something new. Both sides import `loadUser` from the
app's own `api.js`, which fetches a user and passes `signal` to `fetch` so that a stale request
is cancelled.

::: compare
```ts
import { LitElement, html } from 'lit';
import { customElement, property } from 'lit/decorators.js';
import { Task } from '@lit/task';
import { loadUser } from './api.js';

@customElement('user-card')
export class UserCard extends LitElement {
  @property() userId = '1';

  private user = new Task(this, {
    task: ([id], { signal }) => loadUser(id, signal),
    args: () => [this.userId],
  });

  render() {
    return this.user.render({
      pending: () => html`<p>Loading…</p>`,
      complete: (u) => html`<p>${u.name}</p>`,
      error: (e) => html`<p>Failed: ${e}</p>`,
    });
  }
}
```
```fsharp
open Fable.Core
open Fable.Core.JsInterop
open Fetch
open Firelight
open Firelight.Task
open type Firelight.Lit

type User =
    abstract name: string

[<Import("loadUser", "./api.js")>]
let loadUser
    (id: string, signal: AbortSignal)
    : JS.Promise<User> =
    jsNative

let fetchUser
    (args: string[])
    (opts: TaskFunctionOptions)
    : TaskResult<User> =
    match args with
    | [| id |] -> !^(loadUser (id, opts.signal))
    | _ -> initialState

let showUser (u: User) = html $"<p>{u.name}</p>"

let userStatus =
    StatusRenderer(
        pending = (fun () -> html $"<p>Loading…</p>"),
        complete = showUser,
        error = (fun e -> html $"<p>Failed: {e}</p>")
    )

[<AttachMembers>]
type UserCard() as this =
    inherit LitElement()

    let user =
        LitTask(
            this,
            TaskConfig(
                TaskFunction fetchUser,
                args = fun () -> [| this.userId |]
            )
        )

    static member properties =
        PropertyDeclarations.create [
            "userId", PropertyDeclaration<string>()
        ]

    member val userId = "1" with get, set

    override _.render() = html $"{user.render userStatus}"

defineElement<UserCard> "user-card"
```
:::

`[id]` becomes the array pattern `[| id |]`, in a `match`. On the parameter itself,
`fun [| id |] opts -> ...`, it compiles with warning FS0025, "Incomplete pattern matches on this
expression. For example, the value '[|_; _|]' may indicate a case not covered by the
pattern(s)." `args` always returns one item, so the other case never runs; `initialState` is a
harmless answer for it.

The task function returns a `TaskResult`, an erased union of a value and a promise, and `!^`
converts the promise to it. `!^` picks the case from the type it converts to, so `fetchUser`
declares `TaskResult<User>`. In a lambda passed straight to `TaskFunction`, as in `ReportButton`
below, nothing says whether the result is the promise or the value it resolves to, and `!^` fails
with FS0043, "A unique overload for method 'op_ErasedCast' could not be determined based on type
information prior to this program point"; write `U2.Case2` there.

`render` returns an option, `None` for a status with no template, and a hole renders `None` as
nothing.

The `signal` is typed as [Fable.Fetch](https://github.com/fable-compiler/fable-fetch)'s
`AbortSignal`, hence `open Fetch`: Firelight.Task depends on Fable.Fetch, so the signal passes
straight to its `fetch`. See [Cancelling requests](/packages/task/#cancelling-requests).

## Running by hand

`autoRun = !^false` keeps the task from running on updates, and `run` starts it.

::: compare
```ts
import { LitElement, html } from 'lit';
import { customElement } from 'lit/decorators.js';
import { Task } from '@lit/task';
import { buildReport } from './api.js';

@customElement('report-button')
export class ReportButton extends LitElement {
  private report = new Task(this, {
    task: () => buildReport(),
    autoRun: false,
  });

  render() {
    const build = () => this.report.run();
    return html`
      <button @click=${build}>Build</button>
      ${this.report.render({
        pending: () => html`<p>Building…</p>`,
        complete: (r) => html`<p>${r}</p>`,
      })}`;
  }
}
```
```fsharp
open Fable.Core
open Fable.Core.JsInterop
open Firelight
open Firelight.Task
open type Firelight.Lit

[<Import("buildReport", "./api.js")>]
let buildReport () : JS.Promise<string> = jsNative

let reportStatus =
    StatusRenderer(
        pending = (fun () -> html $"<p>Building…</p>"),
        complete = fun (r: string) -> html $"<p>{r}</p>"
    )

[<AttachMembers>]
type ReportButton() as this =
    inherit LitElement()

    let report =
        LitTask(
            this,
            TaskConfig(
                TaskFunction(fun (_: obj[]) _ ->
                    U2.Case2(buildReport ())),
                autoRun = !^false
            )
        )

    override _.render() =
        let build _ = report.run () |> ignore

        html $"""
        <button @click={build}>Build</button>
        {report.render reportStatus}"""

defineElement<ReportButton> "report-button"
```
:::

Before the first run the status is `INITIAL`, which neither side gives a template, so the hole
shows nothing.

## The rest of the API

| Lit | Firelight |
|---|---|
| `task.status === TaskStatus.COMPLETE` | `task.status = TaskStatus.COMPLETE` |
| `task.value`, `task.error` | `task.value`, `task.error`, as options |
| `task.run()`, `task.run([id])` | `task.run ()`, `task.run [\| id \|]` |
| `task.abort()` | `task.abort ()` |
| `await task.taskComplete` | `let! value = task.taskComplete`, in `promise { }` |
| `argsEqual: deepArrayEquals` | `argsEqual = TaskArgsEqual(fun a b -> deepArrayEquals a b)` |
| `initialValue`, `onComplete`, `onError` | The same names, as `TaskConfig` arguments |
| `autoRun: 'afterUpdate'` | `autoRun = !^"afterUpdate"` |

## No direct equivalent

- **The name.** `Task` is `LitTask` in F#.
- **Destructuring `[id]`.** An array pattern does it, in a `match` with a case for any other
  length: `match args with [| id |] -> ... | _ -> initialState`.
- **Number arguments.** `args` must return a JavaScript array, and Fable compiles an `int[]` or
  `float[]` to a typed array, so Lit throws "The args function must return an array". Return a
  `string[]`, an `obj[]`, or an F# tuple, which [Fable](https://fable.io/) compiles to an array:
  `fun () -> this.page, this.size`.
- **`async` task functions.** Return a promise. Write one with Fable.Promise's `promise { ... }`,
  where `let!` awaits a promise as `await` does; F#'s own `async` isn't a promise. Fable.Promise
  comes with Firelight.Task, through Fable.Fetch.

The [Firelight.Task](/packages/task/) page has a live example.
