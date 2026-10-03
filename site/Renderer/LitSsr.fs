/// Rendering Lit templates to HTML strings in Node, with @lit-labs/ssr.
module Site.Renderer.LitSsr

open System
open Fable.Core
open Firelight


/// The output of `render`: strings, and promises of nested results for async directives.
type RenderResult = interface end

[<Import("html", "@lit-labs/ssr")>]
let private serverHtmlTag: StaticHTML.CoreTag = jsNative

[<Import("unsafeStatic", "lit/static-html.js")>]
let private unsafeStatic (html: string) : StaticHTML.StaticValue = jsNative

// Server-only templates with static value support, so trusted HTML strings become part of the
// template itself. Lit SSR then sees the custom elements in them and renders their shadow roots.
let private serverHtml = StaticHTML.withStatic serverHtmlTag

[<Emit("$0($1, ...$2)")>]
let private callTag (tag: StaticHTML.TemplateFn) (strings: string[]) (values: obj[]) : TemplateResult = jsNative

/// A server-only template, like Firelight's `html` but rendered once in Node and never updated.
/// It can hold a whole document (doctype, `<title>`, inline scripts) and emits no hydration
/// markers. Custom elements inside it are still prerendered and hydrate in the browser.
/// Event and property bindings are not allowed.
let html (fmt: FormattableString) : TemplateResult =
    callTag serverHtml (fmt.GetStrings()) (fmt.GetArguments())

/// Trusted HTML (for example markdown-it output) as a server-only template. Any registered custom
/// elements in it are prerendered with Declarative Shadow DOM. (So are those inside `unsafeHTML`:
/// Lit SSR parses its content as a template too. To keep HTML from being prerendered, splice it into
/// the rendered string instead.)
let markup (trustedHtml: string) : TemplateResult = html $"{unsafeStatic trustedHtml}"

[<Import("render", "@lit-labs/ssr")>]
let private render (value: obj) : RenderResult = jsNative

[<Import("collectResult", "@lit-labs/ssr/lib/render-result.js")>]
let private collectResult (result: RenderResult) : JS.Promise<string> = jsNative

/// Renders a template, including the shadow roots of any registered custom elements, to a string.
let renderToString (template: TemplateResult) : JS.Promise<string> = collectResult (render template)
