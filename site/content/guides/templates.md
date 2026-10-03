---
title: Templates
tagline: plain HTML in F# interpolated strings
description: "Write templates as plain HTML in F# interpolated strings: bindings, conditionals, lists, directives and static values, and the F# mistakes to avoid."
section: guides
order: 10
summary: "Plain HTML in F# interpolated strings: bindings, lists, directives"
lead: "Firelight templates are plain HTML written inside F# interpolated strings. Passing them to Lit's `html` tag function enables efficient rendering and surgical DOM updates. The bindings are Lit's too, so what you know about HTML and Lit carries over. This guide covers the parts that are F#."
links:
  - text: "Lit docs: Templates"
    href: https://lit.dev/docs/templates/overview/
  - text: "Lit docs: Expressions"
    href: https://lit.dev/docs/templates/expressions/
  - text: "Lit docs: Built-in directives"
    href: https://lit.dev/docs/templates/directives/
toc: true
---

A template is the value `html` returns: a description of some DOM that Lit renders, then updates
when the values in it change. A component returns one from `render`, and so can any function. Most
of a user interface can be ordinary F# functions that take data and return templates. Only the
parts that need their own state or lifecycle have to be components.

## Templates are interpolated strings

```fsharp
open Firelight
open type Firelight.Lit

let greeting (name: string) =
    html $"""<p class="greeting">Hello, {name}.</p>"""
```

`open type Firelight.Lit` brings `html`, `css`, `nothing` and the directives into scope.

Lit's `html` is a JavaScript tagged template, which keeps a template's fixed text apart from the
values in its holes, such as `{name}` above. An F# interpolated string passed to `html` is a `FormattableString`, which makes
the same split, and Fable compiles it to a tagged template. Lit sees exactly what it would see from
JavaScript: it parses each template once, and on later renders it updates only the values that
changed.

The split also makes templates safe by default. Values are never pasted into the HTML. A string in
a hole becomes text or an attribute value, so a `<script>` tag in someone's name is shown, not run.
For the same reason you can't build a template with `+` or `sprintf`: `html` only accepts an
interpolated string.

Lit doesn't parse the markup itself. The first time a template renders, Lit joins its fixed text
with a marker in place of each hole and hands the result to the browser's own HTML parser, through
a `<template>` element. Lit clones the parsed DOM wherever the template renders, and the markers
tell it which nodes and attributes each hole updates. So a hole only works where the parser leaves
a marker Lit can find. A hole works:

- between tags, as child content;
- in an attribute value, whole or in part;
- in an element's opening tag, as an element directive;
- inside `<title>`, `<style>` or `<script>` (for a component's styles, prefer `css`).

A hole doesn't work:

- in a tag name or an attribute name (see [Static values](#static-values));
- inside an HTML comment, where it isn't updated;
- inside a `<template>` element's content, where Lit throws in development builds;
- inside a `<textarea>` or a `contenteditable` element, where typing breaks Lit's markers. Bind
  `.value` or `.innerText` instead.

Where a hole goes also decides what it sets, which the next section covers.

A hole takes any F# expression: a property, a function call, an `if`, another template. Triple
quotes let the HTML use `"` around attribute values.

In `$"""`, a brace opens a hole, and a literal brace has to be doubled. CSS is full of braces, so
styles use `$$"""` instead. With two dollar signs, single braces are plain text and a hole takes
two: `{{gap}}`.

```fsharp
open Firelight
open type Firelight.Lit

let gap = css $"0.75rem"

let styles =
    css
        $$"""
    :host { display: flex; gap: {{gap}}; }
    button { font: inherit; }
    """
```

`css` only accepts other `css` values and numbers in its holes. `svg $"""..."""` works like `html`,
for fragments that go inside an `<svg>` element.

## Binding values

Where a hole sits decides what it sets. These are Lit's rules; F# only changes the delimiters,
`{value}` where JavaScript has `${value}`.

| Binding | Sets | Example |
|---|---|---|
| `{value}` between tags | Text, a template, a list of them, or nothing | `<p>Hello, {name}</p>` |
| `attr={value}` | An attribute, as a string | `<meter value={used}>` |
| `.prop={value}` | A DOM property, of any type | `<input .value={text}>` |
| `?attr={flag}` | A boolean attribute: present when `true`, removed when `false` | `<button ?disabled={busy}>` |
| `@event={handler}` | An event listener | `<button @click={fun _ -> save ()}>` |
| `<tag {directive}>` | An element directive, such as `ref` | `<input {ref field}>` |

Quotes around an attribute's value are optional. Use them to put fixed text next to a hole:
`class="bar {extra}"`.

This example uses each kind of binding except the last:

::: example Snippets/TemplateBindings.fs
<my-name-field></my-name-field>
:::

The text box binds `.value`, the property, rather than the `value` attribute. The attribute is
only the box's starting value: once you've typed in it, changing the attribute doesn't change what
it shows, so Clear would empty `name` and leave your text where it was. Bind attributes for what
you would write in HTML, and properties for live state and for values that aren't strings, such as
a list passed to a child component.

A lambda in a hole is a new function on every render. That's fine: Lit keeps one listener on the
element and calls the newest function, so a handler always sees the current values. For listener
options such as `passive` or `once`, bind a `LitEventListener(handler, passive = true)` instead of
a function.

## Conditionals and lists

Templates have no syntax of their own for conditions or loops. `if`, `match` and the list
functions already do the job, and a template is a value like any other.

::: example Snippets/TemplateLists.fs
<my-basket></my-basket>
:::

Each branch of the `match` returns a template, so it type-checks as usual. `nothing` is different.
It's Lit's marker for "render nothing here", its type isn't a template, and so
`if empty then nothing else html $"..."` doesn't compile. Annotating the result as
`ChildRenderable`, the type of anything Lit can render, gives both branches a common type.

A hole renders any F# list, array or `seq`, so `List.map` is all a list needs. A list expression
such as `[ for fruit in fruits -> html $"<li>{fruit}</li>" ]` works too.

### Keyed lists with repeat

Lit updates a list by position. The first item's template updates the first element, and so on;
only the end of the list grows or shrinks. That's fast, and right whenever an element shows nothing
but your data. It goes wrong when an element holds state of its own, such as text typed into an
unbound box, focus or a running animation. Remove the first item, and each element is reused for
the item that came after it, while its state stays where it was.

`repeat` gives each item a key and moves its element with it. Type a note next to Bob in both
lists, then remove the first person:

::: example Snippets/TemplateRepeat.fs
<my-repeat-comparison></my-repeat-comparison>
:::

Use `repeat` when items are inserted, removed or reordered and their elements hold state, or are
components. Otherwise `List.map` is simpler and does less work. The key must be unique and stay
with the item, like an id. An item's position isn't a key.

## Directives

Directives are functions that take over how a binding is applied. They are static members of
`Lit`, so `open type Firelight.Lit` brings them into scope, and Fable imports only the ones you
use. These are the ones you'll reach for most:

| Directive | Use it to | Example |
|---|---|---|
| `classMap` | Switch classes on and off | `class={classMap (ClassInfo.create [ "done", isDone ])}` |
| `styleMap` | Set inline styles; `None` removes one | `style={styleMap (StyleInfo.create [ "width", Some "40%" ])}` |
| `ref` | Reach the rendered element | `<input {ref field}>` with `let field = createRef<HTMLInputElement> ()` |
| `ifDefined` | Leave out an attribute when its value is `None` | `href={ifDefined link}` |
| `live` | Compare with the element's current value, not the last one rendered | `.value={live (Some(box text))}` |
| `repeat` | Key a list, so elements move with their items | [Keyed lists](#keyed-lists-with-repeat) |
| `keyed` | Replace an element, rather than update it, when a key changes | `keyed (Some(box user.Id), Some(box (profile user)))` |
| `guard` | Skip re-rendering until a dependency changes | `guard ([\| Some(box rows) \|], fun () -> Some(box (table rows)))` |
| `cache` | Keep the DOM of templates you switch between, such as tabs | `cache (Some(tab :> TemplateResult))` |
| `unsafeHTML` | Render a trusted string as HTML | `unsafeHTML trustedHtml` |

`live`, `keyed` and `guard` take `obj option` arguments in Firelight, hence the `Some(box ...)`;
`cache` takes a `TemplateResult option`. Firelight also binds `join`, `until`, `asyncAppend`, `asyncReplace`,
`templateContent` and `unsafeSVG`. For data that loads asynchronously,
[Firelight.Task](/packages/task/) is usually a better fit than `until`.

This volume control uses the three most common: `classMap` stripes the bar when it's loud,
`styleMap` sets its width, and `ref` lets Reset put the focus back on the slider.

::: example Snippets/TemplateDirectives.fs
<my-volume-control></my-volume-control>
:::

`classMap` and `styleMap` must be the only hole in their attribute, though fixed text can sit
beside them, as `bar` does. Each `classMap` key is a single class name. A key with a space in it,
such as `"bg-red text-white"`, renders the first time, then throws once its value changes.

A ref's `value` is `None` until the element has rendered, so read it in event handlers or after an
update, not in `render`. Use it for what a template can't say: focus, measuring, or calling a method
such as a dialog's `showModal`.

An F# option in an attribute hole doesn't remove the attribute. `None` becomes JavaScript
`undefined`, which Lit writes as an empty attribute. `ifDefined` removes it instead:

```fsharp
open Firelight
open type Firelight.Lit

let link (label: string) (href: string option) =
    html $"""<a href={ifDefined href}>{label}</a>"""
```

`live` is for a property the user can change. Lit only sets a binding when its value differs from
the last render. If the user has typed into a box but your state still holds the value you last
rendered, rendering it again sets nothing, and the typing stays. `live` compares with what the box
holds now.

### unsafeHTML

Some HTML arrives as a string, such as Markdown converted to HTML at build time. `unsafeHTML`
renders it as HTML rather than as text.

```fsharp
open Firelight
open type Firelight.Lit

/// `articleHtml` comes from our own Markdown build, never from users.
let article (articleHtml: string) =
    html $"""<article>{unsafeHTML articleHtml}</article>"""
```

Never pass it text that someone else controls. The string becomes part of your page, so markup
such as `<img src=x onerror="...">` runs that person's script with your page's permissions. If the
HTML comes from users, clean it with a sanitizer built for the job, such as DOMPurify, or show it
as text in an ordinary hole.

## Static values

Because the browser parses a template's fixed text, a tag name or an attribute name has to be part
of that text, not a hole. When one really must vary, such as the heading level
of a card that appears at different depths, use `StaticHTML`:

```fsharp
open Firelight
open type Firelight.Lit

type Level =
    | H2
    | H3

let card (level: Level) (title: string) (body: string) =
    let heading =
        match level with
        | H2 -> StaticHTML.literal $"h2"
        | H3 -> StaticHTML.literal $"h3"

    StaticHTML.html $"""<article><{heading}>{title}</{heading}><p>{body}</p></article>"""
```

`StaticHTML.html` is the `html` from `lit/static-html.js`; the plain `html` doesn't accept static
values. Call it by its full name, since `open type StaticHTML` would hide Lit's `html`.

Static values are part of the template, so each different value makes a separate template. When
the value changes, Lit throws away the old DOM and builds new DOM, rather than updating it. With
only a few cases, a `match` that returns a whole template for each does the same job without
static values.

Choose `StaticHTML.literal` values from a fixed set, as `card` does. `StaticHTML.unsafeStatic`
turns any string into template text, so, like `unsafeHTML`, it must never see untrusted input.

## Common mistakes

The compiler catches some template mistakes, though its message doesn't always point at the
cause:

| You wrote | The compiler says | Write instead |
|---|---|---|
| `html """<p>Hi</p>"""` | The type 'string' is not compatible with the type 'System.FormattableString' | `html $"""<p>Hi</p>"""` |
| `css $"""p { margin: 0; }"""` | Unexpected symbol ';' in expression, or another parse error | `css $$"""p { margin: 0; }"""` |
| `html $"""<p style="width: 50%">"""` | Invalid interpolated string. Bad format specifier | `50%%` in `$"""`. In `$$"""`, `%` is plain text |
| `if empty then nothing else html $"..."` | The type 'nothing' is not compatible with type 'HTMLTemplateResult' | Annotate the result as `ChildRenderable` |
| `ref 0` | No overloads match for method 'ref' | `Operators.ref 0`: Lit's `ref` hides F#'s |

Others compile, because a hole's type is `obj` and it accepts any value:

| You wrote | What happens | Write instead |
|---|---|---|
| `@click={this.Save()}` | `Save` runs on every render, and clicking does nothing | `@click={fun _ -> this.Save()}` |
| `@click={fun (e: KeyboardEvent) -> ...}` | The handler gets a click event, and `e.key` is `undefined` | The event's real type |
| `{price:N2}` | The format is dropped: Lit gets the number as it is | `{price.ToString "N2"}` |
| `{name}` in `$$"""` | Shows the text `{name}` | `{{name}}` |
| `title={maybeTitle}` with an option | `None` leaves an empty `title` attribute | `title={ifDefined maybeTitle}` |
| `this.items.Add item`, on a `ResizeArray` | No re-render | An F# list and `this.items <- this.items @ [ item ]` |

The last row needs a word. Lit re-renders when a reactive property gets a new value, and it decides
by comparing the old and new values by identity. F# lists, records and maps are immutable, so a
change always produces a new value and Lit always notices. A `ResizeArray`, an array or a mutable
field changed in place is still the same object, so nothing happens. Assign a new value, or call
`this.requestUpdate()` after the change.

The same goes for a `mutable` local in a template function. The function has already returned its
template, and changing the variable later doesn't call it again. State that affects what's shown
belongs in a reactive property or an Elmish model.
