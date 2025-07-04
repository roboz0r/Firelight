namespace Lit

open System
open Fable.Core
open Fable.Core.JsInterop
open Browser.Types

module StaticHTML =

    [<AllowNullLiteral>]
    type StaticValue =
        /// A value that can't be decoded from ordinary JSON, make it harder for
        /// a attacker-controlled data that goes through JSON.parse to produce a valid
        /// StaticValue.
        abstract r: obj with get, set


    type TemplateFn = delegate of strings: string[] * [<ParamArray>] values: obj[] -> TemplateResult
    type CoreTag = delegate of strings: string[] * [<ParamArray>] values: obj[] -> U2<TemplateResult, TemplateFn>

open StaticHTML

/// <summary>
/// Static expressions return special values that are interpolated into the template before the template is processed
/// as HTML by Lit. Because they become part of the template's static HTML, they can be placed anywhere in the
/// template - even where expressions would normally be disallowed, such as in attribute and tag names.
/// </summary>
/// <seealso href="https://lit.dev/docs/templates/expressions/#static-expressions"/>
/// <seealso href="https://lit.dev/docs/api/static-html/"/>
type StaticHTML =

    [<Import("html", "lit/static-html.js")>]
    static member inline private htmlInner(strs: string[], [<ParamArray>] args: obj[]) : TemplateResult = nativeOnly

    [<Import("literal", "lit/static-html.js")>]
    static member inline private literalInner(strs: string[], [<ParamArray>] args: obj[]) : StaticValue = nativeOnly

    [<Import("svg", "lit/static-html.js")>]
    static member inline private svgInner(strs: string[], [<ParamArray>] args: obj[]) : TemplateResult = nativeOnly

    /// <summary>
    /// Interprets a template literal as an HTML template that can efficiently render to and update a container.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/api/static-html/#html"/>
    static member inline html(fmt: FormattableString) : TemplateResult =
        StaticHTML.htmlInner (fmt.GetStrings(), fmt.GetArguments())

    /// <summary>
    /// Tags a string literal so that it behaves like part of the static template strings instead of a dynamic value.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/api/static-html/#literal"/>
    /// <remarks>
    /// The only values that may be used in template expressions are other tagged `literal` results or `unsafeStatic`
    /// values (note that untrusted content should never be passed to `unsafeStatic`). Users must take care to
    /// ensure that adding the static string to the template results in well-formed HTML, or else templates may
    /// break unexpectedly. Static values can be changed, but they will cause a complete re-render since they
    /// effectively create a new template.
    /// </remarks>
    static member inline literal(fmt: FormattableString) : StaticValue =
        StaticHTML.literalInner (fmt.GetStrings(), fmt.GetArguments())

    /// <summary>
    /// Interprets a template literal as an SVG fragment that can efficiently render to and update a container.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/api/static-html/#svg"/>
    static member inline svg(fmt: FormattableString) : TemplateResult =
        StaticHTML.svgInner (fmt.GetStrings(), fmt.GetArguments())

    /// <summary>
    /// Wraps a string so that it behaves like part of the static template strings instead of a dynamic value.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/api/static-html/#unsafeStatic"/>
    /// <remarks>
    /// Users must take care to ensure that adding the static string to the template results in well-formed HTML,
    /// or else templates may break unexpectedly. Note that this function is unsafe to use on untrusted content,
    /// as it will be directly parsed into HTML. Do not pass user input to this function without sanitizing it.
    /// Static values can be changed, but they will cause a complete re-render since they effectively create a new template.
    /// </remarks>
    [<Import("unsafeStatic ", "lit/static-html.js")>]
    static member inline unsafeStatic(value: string) : StaticValue = nativeOnly

    /// <summary>
    /// Wraps a lit-html template tag (html or svg) to add static value support.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/api/static-html/#withStatic"/>
    [<Import("withStatic", "lit/static-html.js")>]
    static member inline withStatic(coreTag: CoreTag) : TemplateFn = nativeOnly
