namespace Lit
// css-tag.d.ts
open Fable.Core

/// <summary>
/// A container for a string of CSS text, that may be used to create a CSSStyleSheet.
/// </summary>
/// <remarks>
/// CSSResult is the return value of `css`-tagged template literals and
/// `unsafeCSS()`. In order to ensure that CSSResults are only created via the
/// `css` tag and `unsafeCSS()`, CSSResult cannot be constructed directly.
/// </remarks>
/// <seealso href="https://lit.dev/docs/api/styles/#CSSResult"/>
[<AllowNullLiteral>]
type CSSResult =
    abstract member cssText: string
    abstract member styleSheet: CSSStyleSheet option with get
    abstract member toString: unit -> string

/// A CSSResult or native CSSStyleSheet.
type CSSResultOrNative = U2<CSSResult, CSSStyleSheet>

[<Erase>]
type CSSResultArray = CSSResultArray of U2<CSSResultOrNative, CSSResultArray>

/// A single CSSResult, CSSStyleSheet, or an array or nested arrays of those.
type CSSResultGroup = U2<CSSResultOrNative, CSSResultArray>
