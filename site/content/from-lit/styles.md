---
title: Styles
tagline: static styles and shadow DOM in F#
description: "Lit styles in TypeScript and Firelight F# side by side: static styles, sharing and inheriting them, values in CSS, stylesheets from files, shadow root options and light DOM."
section: from-lit
order: 20
summary: "Static styles, sharing them, and shadow DOM"
lead: "`static styles` becomes `static member styles`, and the CSS in it doesn't change. Write it in a `$$\"\"\"` string, where single braces are plain text."
links:
  - text: "Lit docs: Styles"
    href: https://lit.dev/docs/components/styles/
  - text: "Lit docs: Shadow DOM"
    href: https://lit.dev/docs/components/shadow-dom/
toc: true
---

## Static styles

In `$"""`, a brace opens a hole. CSS is full of braces, so styles use `$$"""`, where a hole takes
two: `{{value}}`.

::: compare
```ts
import { LitElement, css, html } from 'lit';
import { customElement } from 'lit/decorators.js';

@customElement('price-tag')
export class PriceTag extends LitElement {
  static styles = css`
    :host { display: inline-block; }
    .amount { font-weight: bold; color: #2b8a3e; }
  `;

  render() {
    return html`<span class="amount"><slot></slot></span>`;
  }
}
```
```fsharp
open Fable.Core
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type PriceTag() =
    inherit LitElement()

    static member styles =
        css $$"""
        :host { display: inline-block; }
        .amount { font-weight: bold; color: #2b8a3e; }
        """

    override _.render() =
        html $"""
        <span class="amount"><slot></slot></span>"""

defineElement<PriceTag> "price-tag"
```
:::

## Sharing styles

A shared style is a `css` value in a module. List several in an array, as in Lit.

::: compare
```ts
import { LitElement, css, html } from 'lit';
import { customElement } from 'lit/decorators.js';

export const panel = css`
  :host {
    display: block;
    padding: 1rem;
    border: 1px solid #ced4da;
  }
`;

@customElement('note-panel')
export class NotePanel extends LitElement {
  static styles = [panel, css`p { margin: 0; }`];

  render() {
    return html`<p><slot></slot></p>`;
  }
}
```
```fsharp
open Fable.Core
open Firelight
open type Firelight.Lit

let panel =
    css $$"""
    :host {
        display: block;
        padding: 1rem;
        border: 1px solid #ced4da;
    }
    """

[<AttachMembers>]
type NotePanel() =
    inherit LitElement()

    static member styles =
        [| panel; css $$"""p { margin: 0; }""" |]

    override _.render() = html $"<p><slot></slot></p>"

defineElement<NotePanel> "note-panel"
```
:::

The `cssResultGroup { ... }` builder in `Firelight.Builders` does the same job, for lists that mix
`css` values, stylesheets and other lists.

## Inheriting styles

A subclass names its base class to reach the inherited styles. F# has no `base` in a static
member, so there is no counterpart to `super.styles`.

::: compare
```ts
import { LitElement, css, html } from 'lit';
import type { CSSResultGroup } from 'lit';
import { customElement } from 'lit/decorators.js';

export class BaseCard extends LitElement {
  static styles: CSSResultGroup = css`
    :host { display: block; padding: 1rem; }
  `;
}

@customElement('alert-card')
export class AlertCard extends BaseCard {
  static styles = [
    BaseCard.styles,
    css`:host { border-left: 4px solid #e03131; }`,
  ];

  render() {
    return html`<slot></slot>`;
  }
}
```
```fsharp
open Fable.Core
open Firelight
open type Firelight.Lit

[<AbstractClass; AttachMembers>]
type BaseCard() =
    inherit LitElement()

    static member styles =
        css $$"""
        :host { display: block; padding: 1rem; }
        """

[<AttachMembers>]
type AlertCard() =
    inherit BaseCard()

    static member styles =
        [|
            BaseCard.styles
            css $$"""
            :host { border-left: 4px solid #e03131; }
            """
        |]

    override _.render() = html $"<slot></slot>"

defineElement<AlertCard> "alert-card"
```
:::

`BaseCard` has no `render`, so F# needs it marked `[<AbstractClass>]`, since `render` is abstract
in `LitElement`.

## Values in styles

A hole in `css` takes another `css` value or a number, in both languages. Anything else, such as a
string, goes through `unsafeCSS`.

::: compare
```ts
import { LitElement, css, html, unsafeCSS } from 'lit';
import { customElement } from 'lit/decorators.js';

const accent = css`#1971c2`;
const radius = 6;
const font = unsafeCSS('system-ui, sans-serif');

@customElement('accent-button')
export class AccentButton extends LitElement {
  static styles = css`
    button {
      background: ${accent};
      border-radius: ${radius}px;
      font-family: ${font};
    }
  `;

  render() {
    return html`<button><slot></slot></button>`;
  }
}
```
```fsharp
open Fable.Core
open Firelight
open type Firelight.Lit

let accent = css $"#1971c2"
let radius = 6
let font = unsafeCSS "system-ui, sans-serif"

[<AttachMembers>]
type AccentButton() =
    inherit LitElement()

    static member styles =
        css $$"""
        button {
            background: {{accent}};
            border-radius: {{radius}}px;
            font-family: {{font}};
        }
        """

    override _.render() =
        html $"<button><slot></slot></button>"

defineElement<AccentButton> "accent-button"
```
:::

`unsafeCSS` puts the string into the stylesheet as it is. Keep it to text you wrote: CSS from
someone else can load images from their server, which tells them who viewed the page, or restyle the
page to mislead.

## Styles from a CSS file

In TypeScript, a CSS module script can import a stylesheet:
`import sheet from './card.css' with { type: 'css' }`. [Fable](https://fable.io/) can't emit that
import attribute, so import the file's text through Vite's `?inline` suffix instead, and build a
stylesheet from it. Both sides below do that.

::: compare
```ts
import { LitElement, html } from 'lit';
import { customElement } from 'lit/decorators.js';
import cardCss from './card.css?inline';

const card = new CSSStyleSheet();
card.replaceSync(cardCss);

@customElement('file-card')
export class FileCard extends LitElement {
  static styles = [card];

  render() {
    return html`<slot></slot>`;
  }
}
```
```fsharp
open Fable.Core
open Browser.Types
open Browser.Css
open Firelight
open type Firelight.Lit

[<ImportDefault("./card.css?inline")>]
let cardCss: string = jsNative

let card =
    let sheet = CSSStyleSheet.Create()
    sheet.replaceSync cardCss
    sheet

[<AttachMembers>]
type FileCard() =
    inherit LitElement()

    static member styles = [| card |]

    override _.render() = html $"<slot></slot>"

defineElement<FileCard> "file-card"
```
:::

Every element of the class shares the one stylesheet. Rules from the page's own `<link>`
stylesheets don't reach inside a shadow root, in either language. The
[Styling guide](/guides/styling/#share-styles-between-components) covers using a global
stylesheet, such as Tailwind's, this way.

## Shadow root options

`shadowRootOptions` is a static member too. Fable's `ShadowRootInit` sets fields one by one,
where TypeScript spreads Lit's defaults.

::: compare
```ts
import { LitElement, html } from 'lit';
import { customElement } from 'lit/decorators.js';

@customElement('focus-field')
export class FocusField extends LitElement {
  static shadowRootOptions = {
    ...LitElement.shadowRootOptions,
    delegatesFocus: true,
  };

  render() {
    return html`<input placeholder="Name">`;
  }
}
```
```fsharp
open Fable.Core
open Fable.Core.JsInterop
open Browser.Types
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type FocusField() =
    inherit LitElement()

    static member shadowRootOptions =
        jsOptions<ShadowRootInit> (fun o ->
            o.mode <- EncapsulationMode.Open
            o.delegatesFocus <- true)

    override _.render() =
        html $"""<input placeholder="Name">"""

defineElement<FocusField> "focus-field"
```
:::

Lit's default is `{ mode: 'open' }`, so setting `mode` and `delegatesFocus` gives the same
options.

## Rendering without shadow DOM

`LightDomElement` is a `LitElement` whose `createRenderRoot` returns the element itself.

::: compare
```ts
import { LitElement, html } from 'lit';
import { customElement } from 'lit/decorators.js';

@customElement('page-note')
export class PageNote extends LitElement {
  protected createRenderRoot() {
    return this;
  }

  render() {
    return html`<p class="note">Styled by the page</p>`;
  }
}
```
```fsharp
open Fable.Core
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type PageNote() =
    inherit LightDomElement()

    override _.render() =
        html $"""<p class="note">Styled by the page</p>"""

defineElement<PageNote> "page-note"
```
:::

Without a shadow root, the page's stylesheets apply, `static styles` doesn't, and `<slot>` has
nothing to show.

## No direct equivalent

- **`super.styles`.** Name the base class: `BaseCard.styles`.
- **CSS module scripts.** `import sheet from './x.css' with { type: 'css' }` needs an import
  attribute, which Fable can't emit. Import the text with `?inline`, as above.
- **Spreading objects.** `{ ...LitElement.shadowRootOptions, delegatesFocus: true }` has no F#
  syntax. Set the fields with `jsOptions`, or merge with `JS.Constructors.Object.assign`.

Firelight binds the rest of Lit's styles API: `unsafeCSS`, `adoptStyles`, `getCompatibleStyle`
and `supportsAdoptingStyleSheets`. For classes and inline styles that change with state, see
`classMap` and `styleMap` in [Templates](/from-lit/templates/#built-in-directives).

## Mistakes that compile

| You wrote | What happens | Write instead |
|---|---|---|
| `css $$"""p { color: {{color}}; }"""` with a string | Lit throws: Value passed to 'css' function must be a 'css' function result. Debug builds throw first, naming the hole: css: the hole after "p { color: " holds the string … | `{{unsafeCSS color}}`, for text you wrote |
| `{color}` in `$$"""` | The stylesheet holds the text `{color}`, which the browser can't parse | `{{color}}` |
| `static member styles` on a `LightDomElement` | No effect: there is no shadow root to adopt them | The page's stylesheet |
