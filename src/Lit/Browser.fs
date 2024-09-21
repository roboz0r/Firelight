[<AutoOpen>]
module Browser

open Fable.Core

/// <summary>
/// The CSSStyleSheet interface represents a single CSS stylesheet
/// </summary>
/// <seealso href="https://developer.mozilla.org/en-US/docs/Web/API/CSSStyleSheet"/>
[<AllowNullLiteral>]
type CSSStyleSheet = interface end

[<AllowNullLiteral>]
type ElementDefinitionOptions =
    abstract member extends: string option with get, set

[<Global>]
type customElements =
    static member define(elementName: string, constructor: obj, ?options: ElementDefinitionOptions) : unit = nativeOnly
