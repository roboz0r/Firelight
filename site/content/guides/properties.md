---
title: Properties and attributes
tagline: a component's inputs, from HTML and from F#
description: "Declare a Firelight component's reactive properties: attributes and their types, reflection, internal state, and how Lit's change detection meets immutable F# values."
section: guides
order: 20
summary: "Reactive properties: attributes, types, reflection, state and change detection"
lead: "Declare a component's inputs as reactive properties in F#. Lit re-renders the component when one changes, and links each one to an HTML attribute, so the same component can be set up from HTML or from an F# template. This guide covers the declarations and how they meet F# types."
links:
  - text: "Lit docs: Reactive properties"
    href: https://lit.dev/docs/components/properties/
toc: true
---

A reactive property is a member that Lit watches. Set it, from F#, from JavaScript or through an
attribute, and Lit schedules a render with the new value. The render runs after the current code
finishes, so setting several properties at once causes one render. Until then the DOM shows the old
values; to read it after a change, wait for the component's `updateComplete` promise.

## Declare a property

A property has two halves: a `member val` that holds the value and its default, and an entry in
`properties` that tells Lit to watch it.

```fsharp
open Fable.Core
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type Greeting() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [ "name", PropertyDeclaration<string>() ]

    member val name = "World" with get, set

    override this.render() = html $"""<p>Hello, {this.name}.</p>"""

defineElement<Greeting> "my-greeting"
```

`<my-greeting name="Ada"></my-greeting>` says "Hello, Ada." in a page, and so does setting
`greeting.name <- "Ada"` from F#.

[Fable](https://fable.io/) compiles `member val` to a getter and a setter on the JavaScript class.
`[<AttachMembers>]` puts them there, and Lit wraps them with its own setter, which asks for an
update. The string in `properties` is how Lit finds the member, so it must match the member's name
exactly, case included. The compiler doesn't check it. To have it checked, write
`nameof Unchecked.defaultof<Greeting>.name` in place of `"name"`.

The type argument of `PropertyDeclaration<'T>` is the member's type. It types the `hasChanged` and
`converter` options, and it decides how Lit reads the attribute, as the [next
section](#attributes-are-text) shows. `PropertyDeclaration<'T>` comes with `open type Firelight.Lit`,
like `html`. Its options:

| Option | What it does | Example |
|---|---|---|
| `attribute` | Names the attribute, or turns it off | `attribute = "warn-at"`, `attribute = false` |
| `reflect` | Copies the property back to its attribute | `reflect = true` |
| `useDefault` | Keeps the default out of the attribute and restores it when the attribute is removed | `useDefault = true` |
| `state` | Marks internal state, with no attribute | `state = true` |
| `hasChanged` | Decides what counts as a change | `hasChanged = fun next prev -> next <> prev` |
| `converter` | Converts the attribute to and from the property | `converter = AttributeConverter(fromAttribute = parse)` |
| ``` ``type`` ``` | Replaces the conversion chosen from `'T` | ``` ``type`` = jsConstructor<Globals.Array> ``` |
| `noAccessor` | Leaves the setter to you; call `requestUpdate` yourself | `noAccessor = true` |

`type` is an F# keyword, so that option is written in double backticks.

## Attributes are text

HTML can only set attributes, and an attribute's value is always text. A property can hold any
value. Lit links each declared property to an attribute named after it in lower case (`maxItems`
to `maxitems`), and converts the attribute's text by the property's type:

| `'T` | `step="0.5"` becomes | When the attribute is removed |
|---|---|---|
| `float`, `int` and the other numeric types | `0.5` | `null` |
| `bool` | `true`: any value, even `"false"` | `false` |
| `string`, and any other type | `"0.5"`, a string | `null` |

`PropertyDeclaration<'T>` sets Lit's `type` option to `Number` for the types Fable
compiles to JavaScript numbers (`float`, `float32`, `int`, `int16`, `uint16`, `uint32`, `sbyte`
and `byte`) and to `Boolean` for `bool`. Any other type gets the text as it is, whatever its F#
type says. `int64` and `decimal` are numbers in F# but not JavaScript numbers, and a list, an option
or a record has no text form. Give those a `converter`, or turn the attribute off.

Here `step` is a `float`, so `step="0.5"` arrives as the number 0.5. The tags are a `string list`,
with a converter that splits `tags="lit, fable, fsharp"` into a list, and joins the list back into
the attribute, which it reflects. Click both buttons, and watch the second element's `tags`
attribute in your browser's developer tools:

::: example Snippets/PropertyTypes.fs .stacked
<my-quantity step="0.5"></my-quantity>
<my-tags tags="lit, fable, fsharp"></my-tags>
:::

`fromAttribute` gets the attribute's text as a `string option`, `None` once the attribute is
removed. `toAttribute` returns the text to write, or `None` to remove the attribute; Lit calls it
only for a property with `reflect = true`. Leave either out to keep Lit's conversion for that
direction.

A Boolean attribute works like HTML's own `disabled`: present means `true`, absent means `false`.
`open="false"` is still present, so it's `true`. Give a Boolean property a `false` default, since
an attribute can't turn a `true` default off. From a template, bind it with `?open={isOpen}`, which
adds or removes the attribute.

An attribute the page leaves out does nothing, and the property keeps its F# default. Removing one
later sets the property, as in the table's last column, whatever its F# type says. An `int` turns `null`
into `0`; a `string` or a `float` stays `null`. `useDefault = true` restores the default instead.

For a different attribute name, such as `warn-at` for `warnAt`, set
`attribute = "warn-at"`. For a value that can't be text, such as a list, a record or a function,
set `attribute = false`, and pass it as a property instead.

### Passing F# values with .prop

A component used in another component's template takes values through properties. `.players={…}`
sets the property itself, with no text in between, so any F# value arrives as it is. The
[Templates guide](/guides/templates/#binding-values) lists the other bindings.

This scoreboard keeps a list of player records and passes it, sorted, to two rankings with
`.players={ranked}`. Click the +1 buttons, then type in Note:

::: example Snippets/PropertyObjects.fs
<my-scoreboard></my-scoreboard>
:::

Both rankings update when a score changes. While you type a note, the first ranking renders again
on every key and the second doesn't. The [next section](#change-detection-and-immutable-values)
explains why.

## Change detection and immutable values

When a property is set, Lit compares the new value with the old one by identity (JavaScript's
`Object.is`), and renders only if they differ. A number or a string is compared by value. A list or
a record is compared by reference: the same object or not.

F# lists, records and maps are immutable, so changing one produces a new value, and Lit notices
(the [Templates guide](/guides/templates/#common-mistakes) covers values changed in place, which it
doesn't). But a new list can hold the same players as the old one. The scoreboard
sorts its players in `render`, and `List.sortByDescending` returns a new list every time. Typing a
note re-renders the scoreboard, which passes a new but equal list to both rankings, and the first
ranking renders again for nothing.

`hasChanged` replaces Lit's comparison. The second ranking declares
`hasChanged = fun next prev -> next <> prev`, and F#'s `<>` compares lists and records by their
contents, so an equal list isn't a change. The comparison walks the whole value on every set, so
for a long list it can cost more than the render it saves. There, compare something cheaper, such
as an id or a version number, or keep the list out of `render` so it's only rebuilt when it
changes.

## Reflect a property to its attribute

With `reflect = true`, Lit copies the property to its attribute after each change. Selectors can
then see the component's state: its own `:host([expanded])` rule, the page's CSS, and
`querySelector`. The [star rating](/packages/firelight/#example) reflects its `value`: inspect it
in your browser's developer tools and click a star, and the attribute follows.

```fsharp
open Fable.Core
open Fable.Core.JsInterop
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type Disclosure() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [
            "expanded", PropertyDeclaration<bool>(reflect = true)
        ]

    static member styles =
        css
            $$"""
        :host { display: block; }
        :host([expanded]) { outline: 2px solid var(--accent, orange); }
        .body { display: none; }
        :host([expanded]) .body { display: block; }
        """

    member val expanded = false with get, set

    override this.render() =
        html
            $"""
        <button aria-expanded={string this.expanded} @click={fun _ -> this.expanded <- not this.expanded}>Details</button>
        <div class="body"><slot></slot></div>"""

defineElement<Disclosure> "my-disclosure"
```

Reflect what selectors or the page need to see. Don't reflect values that change many times a
second or that are large: each change writes the attribute. A reflected default appears on the
element as soon as it renders. With `useDefault = true` it doesn't, and removing the attribute
puts the default back.

## Internal state

Some values only the component itself sets: whether a menu is open, what's been typed so far.
Declare them with `state = true`. They render when they change, like any property, but have no
attribute.

```fsharp
open Fable.Core
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type Counter() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [ "count", PropertyDeclaration<int>(state = true) ]

    member val count = 0 with get, set

    override this.render() =
        html $"""<button @click={fun _ -> this.count <- this.count + 1}>Clicked {this.count} times</button>"""

defineElement<Counter> "my-counter"
```

`state` is about attributes, not access. The member is still public in F# and in JavaScript.
When a component's state grows past a few values, or changes in ways that are worth naming, keep
it in one immutable model with [Firelight.Elmish](/packages/elmish/) instead.

## Common mistakes

The compiler catches a few:

| You wrote | The compiler says | Write instead |
|---|---|---|
| `PropertyDeclaration<int>()` without `open type Firelight.Lit` | Invalid use of a type name | `open type Firelight.Lit` |
| `attribute = 5` | This expression was expected to have type 'PropertyAttribute' but here has type 'int' | A name, `attribute = "count"`, or `false` |
| `PropertyDeclaration<int>(type = …)` | Unmatched '(' | ``` ``type`` = … ``` |
| `jsConstructor<Array>` with `open System` | Fable: Only declared types define a function constructor in JS | `jsConstructor<Globals.Array>` |
| `hasChanged` with the wrong types | This expression was expected to have type 'Player list' but here has type 'string' | A function of two `'T`s, the type in `PropertyDeclaration<'T>` |

Most compile, because Lit reads the declarations at run time:

| You wrote | What happens | Write instead |
|---|---|---|
| `"Name"` in `properties`, `member val name` | The greeting never changes: the `name` attribute sets a separate `Name` property, and setting `name` doesn't render | The member's exact name, or `nameof` |
| No `[<AttachMembers>]` | Lit can't find the members: attributes and property sets are ignored | `[<AttachMembers>]` on every component |
| `PropertyDeclaration<int64>()` or `<decimal>` set from an attribute | The value is the attribute's text, a string | A `converter` that parses it |
| `open="false"` on a `bool` property | `true`: the attribute is present | Leave the attribute out, or bind `?open={isOpen}` |
| `<my-list max-items="5">` for `maxItems` | Ignored: Lit listens for `maxitems` | `attribute = "max-items"` |
| `items="{list}"` or `items={list}` with a list | The list becomes text | `.items={list}` |
| A list or record built in `render` and passed down | The child renders every time the parent does | `hasChanged`, or build it outside `render` |
| `let mutable title = "Notes"` in the class, or a `let` named `id`, `hidden`, `lang` or another `HTMLElement` property | Fable makes a class `let` a property of the element itself, so this sets the element's `title`, which adds a `title` attribute. In a template the element gets a tooltip; `document.createElement` fails with "The result must not have attributes" and leaves an `HTMLUnknownElement` | Another name, such as `heading`, or `member val private Heading = "Notes"`, which Fable stores under `Heading@` |

The [Templates guide](/guides/templates/#common-mistakes) lists more mistakes, in templates.
