---
title: Templates
tagline: Lit templates as F# interpolated strings
description: "Lit templates in TypeScript and Firelight F# side by side: expressions, conditionals, lists, built-in directives, static values and rendering outside a component."
section: from-lit
order: 60
summary: "Expressions, conditionals, lists, directives and static values"
lead: "A template is an F# interpolated string passed to `html`. A hole is `{value}` where TypeScript has `${value}`, and the bindings, directives and update rules are Lit's."
links:
  - text: "Lit docs: Templates"
    href: https://lit.dev/docs/templates/overview/
  - text: "Lit docs: Built-in directives"
    href: https://lit.dev/docs/templates/directives/
  - text: "Firelight guide: Templates"
    href: /guides/templates/
toc: true
---

The [Templates guide](/guides/templates/) explains how F# strings become Lit templates and the
mistakes to avoid. This page maps what you write in TypeScript to what you write in F#.

## Expressions

Every kind of binding keeps its prefix: none for a child or an attribute, `.` for a property, `?`
for a boolean attribute and `@` for an event. `Ev.checked'` and `Ev.value` hand the handler the
element's `checked` or `value`, which replaces the cast to `HTMLInputElement`.

::: compare
```ts
import { LitElement, html } from 'lit';
import { customElement, state } from 'lit/decorators.js';

@customElement('gift-note')
export class GiftNote extends LitElement {
  @state() private gift = false;
  @state() private note = '';

  render() {
    return html`
      <label>
        <input type="checkbox" .checked=${this.gift}
          @change=${(e: Event) => (this.gift =
            (e.target as HTMLInputElement).checked)}>
        Gift
      </label>
      <input placeholder="Note" ?disabled=${!this.gift}
        .value=${this.note}
        @input=${(e: Event) => (this.note =
          (e.target as HTMLInputElement).value)}>
      <p class=${this.gift ? 'on' : 'off'}>
        ${this.gift ? `Note: ${this.note}` : 'No gift'}
      </p>`;
  }
}
```
```fsharp
open Fable.Core
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type GiftNote() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [
            "gift", PropertyDeclaration<bool>(state = true)
            "note",
            PropertyDeclaration<string>(state = true)
        ]

    member val private gift = false with get, set
    member val private note = "" with get, set

    override this.render() =
        let setGift c = this.gift <- c
        let setNote v = this.note <- v

        let summary =
            if this.gift then $"Note: {this.note}"
            else "No gift"

        html $"""
        <label>
            <input type="checkbox" .checked={this.gift}
                @change={Ev.checked' setGift}>
            Gift
        </label>
        <input placeholder="Note" ?disabled={not this.gift}
            .value={this.note}
            @input={Ev.value setNote}>
        <p class={if this.gift then "on" else "off"}>
            {summary}
        </p>"""

defineElement<GiftNote> "gift-note"
```
:::

The F# template is in triple quotes, `$"""`, so its attributes can use `"` and its holes can hold
strings.

## Conditionals

`if` and `match` do the work of the ternary operator and of `choose`. Each returns a template or
`nothing`, which fits wherever a template does.

::: compare
```ts
import { LitElement, html, nothing } from 'lit';
import { customElement, property } from 'lit/decorators.js';
import { choose } from 'lit/directives/choose.js';

@customElement('save-status')
export class SaveStatus extends LitElement {
  @property() status = 'idle';
  @property({ type: Boolean }) dirty = false;

  render() {
    const unsaved = this.dirty
      ? html`<span>Unsaved changes</span>`
      : nothing;
    const status = choose(this.status, [
      ['saving', () => html`<p>Saving…</p>`],
      ['error', () => html`<p class="error">Failed</p>`],
    ], () => html`<p>All saved</p>`);
    return html`${unsaved}${status}`;
  }
}
```
```fsharp
open Fable.Core
open Fable.Core.JsInterop
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type SaveStatus() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [
            "status", PropertyDeclaration<string>()
            "dirty", PropertyDeclaration<bool>()
        ]

    member val status = "idle" with get, set
    member val dirty = false with get, set

    override this.render() =
        let unsaved =
            if this.dirty then
                html $"<span>Unsaved changes</span>"
            else
                nothing

        let status =
            match this.status with
            | "saving" -> html $"<p>Saving…</p>"
            | "error" ->
                html $"""<p class="error">Failed</p>"""
            | _ -> html $"<p>All saved</p>"

        html $"{unsaved}{status}"

defineElement<SaveStatus> "save-status"
```
:::

Firelight binds `choose` and `when` too, as `choose` and `when'` (`when` is an F# keyword), but
`match` and `if` read better and the compiler checks them.

## Lists

`List.map` replaces `Array.map`, and `repeat` keeps its three arguments. Its key and template
functions take the item and its index, so `fun t _ ->` ignores the index.

::: compare
```ts
import { LitElement, html } from 'lit';
import { customElement, state } from 'lit/decorators.js';
import { repeat } from 'lit/directives/repeat.js';

interface Task { id: number; text: string }

@customElement('task-list')
export class TaskList extends LitElement {
  @state() private tasks: Task[] = [
    { id: 1, text: 'Water plants' },
    { id: 2, text: 'Post letter' },
  ];

  private finish(id: number) {
    this.tasks = this.tasks.filter((t) => t.id !== id);
  }

  private row(t: Task) {
    const done = () => this.finish(t.id);
    return html`<li>
      ${t.text} <button @click=${done}>Done</button>
    </li>`;
  }

  render() {
    const rows = repeat(
      this.tasks, (t) => t.id, (t) => this.row(t));
    return html`<ul>${rows}</ul>`;
  }
}
```
```fsharp
open Fable.Core
open Firelight
open type Firelight.Lit

type Task = { Id: int; Text: string }

[<AttachMembers>]
type TaskList() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [
            "tasks",
            PropertyDeclaration<Task list>(state = true)
        ]

    member val private tasks =
        [ { Id = 1; Text = "Water plants" }
          { Id = 2; Text = "Post letter" } ] with get, set

    member private this.Finish(id: int) =
        this.tasks <-
            this.tasks |> List.filter (fun t -> t.Id <> id)

    member private this.Row(t: Task) =
        let finish _ = this.Finish t.Id

        html $"""<li>
            {t.Text} <button @click={finish}>Done</button>
        </li>"""

    override this.render() =
        let rows =
            repeat (
                this.tasks,
                (fun t _ -> t.Id),
                fun t _ -> this.Row t
            )

        html $"<ul>{rows}</ul>"

defineElement<TaskList> "task-list"
```
:::

Without keys, `this.tasks |> List.map this.Row` in the hole renders the same list. The Templates
guide has a [demo of the difference](/guides/templates/#keyed-lists-with-repeat): type into a row,
then remove the one above it.
`List.filter` returns a new list, so Lit sees the change, as it does for `Array.filter`.

## Built-in directives

Each directive is a static member of `Lit`, so `open type Firelight.Lit` brings them all in
scope, with no import per directive. `classMap` and `styleMap` take their objects through
`ClassInfo.create` and `StyleInfo.create`.

::: compare
```ts
import { LitElement, html } from 'lit';
import { customElement, property } from 'lit/decorators.js';
import { classMap } from 'lit/directives/class-map.js';
import { styleMap } from 'lit/directives/style-map.js';

@customElement('fill-bar')
export class FillBar extends LitElement {
  @property({ type: Number }) value = 0;

  render() {
    const full = this.value >= 100;
    return html`<div
      class=${classMap({ bar: true, full })}
      style=${styleMap({ width: `${this.value}%` })}>
    </div>`;
  }
}
```
```fsharp
open Fable.Core
open Fable.Core.JsInterop
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type FillBar() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [
            "value", PropertyDeclaration<float>()
        ]

    member val value = 0.0 with get, set

    override this.render() =
        let full = this.value >= 100.0
        let classes =
            ClassInfo.create [ "bar", true; "full", full ]

        let percent = $"{this.value}%%"
        let width =
            StyleInfo.create [ "width", Some percent ]


        html $"""<div
            class={classMap classes}
            style={styleMap width}>
        </div>"""

defineElement<FillBar> "fill-bar"
```
:::

`%%` is a literal `%` in an F# interpolated string. A style whose value is `None` is removed,
where TypeScript would use `undefined` or `null`.

| Lit | Firelight |
|---|---|
| `classMap({ on: x })` | `classMap (ClassInfo.create [ "on", x ])` |
| `styleMap({ color: c })` | `styleMap (StyleInfo.create [ "color", Some c ])` |
| `repeat(items, key, tpl)` | `repeat (items, (fun i _ -> key), fun i _ -> tpl)` |
| `ifDefined(x)` | `ifDefined x`, where `x` is an option |
| `live(x)` | `live x` |
| `ref(r)`, `createRef()` | `ref r`, `createRef<HTMLInputElement> ()` |
| `guard([a, b], () => t)` | `guard ([\| a; b \|], fun () -> t)` |
| `cache(t)`, `keyed(k, t)` | `cache t`, `keyed (k, t)` |
| `until(p, placeholder)` | `until (p, placeholder)` |
| `join(xs, sep)` | `join (xs, sep)` |
| `when(c, a, b)` | `when' (c, a, b)` |
| `choose`, `map`, `range` | `choose`, `map`, `range` |
| `asyncAppend`, `asyncReplace` | `asyncAppend`, `asyncReplace` |
| `unsafeHTML`, `unsafeSVG` | `unsafeHTML`, `unsafeSVG` |
| `templateContent(t)` | `templateContent t` |

`open type Firelight.Lit` hides F#'s own `ref`; write `Operators.ref` for a reference cell.

## Static values

`literal` and the static `html` live in `StaticHTML`. Call them by their full names, so the plain
`html` stays in scope.

::: compare
```ts
import { LitElement } from 'lit';
import { customElement, property } from 'lit/decorators.js';
import { html, literal } from 'lit/static-html.js';

@customElement('section-title')
export class SectionTitle extends LitElement {
  @property({ type: Number }) level = 2;

  render() {
    const tag =
      this.level === 3 ? literal`h3` : literal`h2`;
    return html`<${tag}><slot></slot></${tag}>`;
  }
}
```
```fsharp
open Fable.Core
open Fable.Core.JsInterop
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type SectionTitle() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [
            "level", PropertyDeclaration<int>()
        ]

    member val level = 2 with get, set

    override this.render() =
        let tag =
            if this.level = 3 then StaticHTML.literal $"h3"
            else StaticHTML.literal $"h2"

        StaticHTML.html $"<{tag}><slot></slot></{tag}>"

defineElement<SectionTitle> "section-title"
```
:::

## Rendering outside a component

`render` is a static member of `Lit` too. It returns the `RootPart`, which F# makes you use or
`ignore`.

::: compare
```ts
import { html, render } from 'lit';

const greeting = (name: string) =>
  html`<p>Hello, ${name}</p>`;

render(greeting('Ada'), document.body);
```
```fsharp
open Browser
open Firelight
open type Firelight.Lit

let greeting (name: string) =
    html $"<p>Hello, {name}</p>"

render (greeting "Ada", document.body) |> ignore
```
:::

## No direct equivalent

- **Custom directives.** Lit's `directive()`, `Directive` and `AsyncDirective` aren't bound, so
  you can't write a directive class in F#. A function that returns a template covers most uses.
  For one that needs Lit's parts, write the directive in JavaScript and import it with
  `[<Import>]`.
- **`unsafeMathML`.** Not bound. `mathml` templates are.
- **Template type checking.** `lit-analyzer` and its editor plugin check bindings in TypeScript
  templates. Nothing reads the HTML inside an F# string: a hole's type is `obj`, so any value
  compiles, and nothing checks an `Ev` handler against the event's name.
- **Format specifiers.** In TypeScript you format inside the hole: `${price.toFixed(2)}`. In F#,
  `{price:F2}` compiles but Lit never sees the format, and Debug builds throw. Write
  `{price.ToString "F2"}`.
