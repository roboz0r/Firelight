namespace Fable.Shiki

open Fable.Core
open Fable.Core.JsInterop

/// A JavaScript object mapping a color name (for example, light or dark) to a Shiki theme.
type ThemeMap = interface end

[<Erase>]
module ThemeMap =
    let inline create (themes: #seq<string * string>) : ThemeMap =
        !!(createObj !!themes)

/// Colours to swap in the output, per theme: a JavaScript object mapping a theme name to an object
/// mapping each colour the theme uses (lowercase hex) to the colour to show instead.
type ColorReplacements = interface end

[<Erase>]
module ColorReplacements =
    let inline create (byTheme: #seq<string * (string * string) list>) : ColorReplacements =
        !!(createObj [ for theme, colors in byTheme -> theme, createObj !!colors ])

/// A JavaScript object mapping a language alias to a bundled language name.
type LanguageAliasMap = interface end

[<Erase>]
module LanguageAliasMap =
    let inline create (aliases: #seq<string * string>) : LanguageAliasMap =
        !!(createObj !!aliases)

/// Options accepted by codeToHtml, codeToHast, and codeToTokens.
[<AllowNullLiteral>]
type CodeOptions = interface end

/// Highlight using one theme. The language and theme must be loaded on a reusable highlighter.
[<AllowNullLiteral; Global>]
type SingleThemeOptions
    [<ParamObject; Emit("$0")>]
    (lang: string, theme: string, ?rootStyle: U2<string, bool>,
     ?structure: string, ?mergeWhitespaces: U2<bool, string>,
     ?mergeSameStyleTokens: bool, ?tokenizeMaxLineLength: int,
     ?tokenizeTimeLimit: int, ?grammarContextCode: string) =
    member val lang: string = nativeOnly with get, set
    member val theme: string = nativeOnly with get, set
    member val rootStyle: U2<string, bool> option = nativeOnly with get, set
    member val structure: string option = nativeOnly with get, set
    member val mergeWhitespaces: U2<bool, string> option = nativeOnly with get, set
    member val mergeSameStyleTokens: bool option = nativeOnly with get, set
    member val tokenizeMaxLineLength: int option = nativeOnly with get, set
    member val tokenizeTimeLimit: int option = nativeOnly with get, set
    member val grammarContextCode: string option = nativeOnly with get, set
    interface CodeOptions

/// Highlight using several themes, exposed in the HTML as CSS variables.
[<AllowNullLiteral; Global>]
type MultipleThemeOptions
    [<ParamObject; Emit("$0")>]
    (lang: string, themes: ThemeMap, ?defaultColor: U2<string, bool>,
     ?colorsRendering: string, ?cssVariablePrefix: string,
     ?rootStyle: U2<string, bool>, ?structure: string,
     ?mergeWhitespaces: U2<bool, string>, ?mergeSameStyleTokens: bool,
     ?tokenizeMaxLineLength: int, ?tokenizeTimeLimit: int,
     ?grammarContextCode: string, ?colorReplacements: ColorReplacements) =
    member val lang: string = nativeOnly with get, set
    member val themes: ThemeMap = nativeOnly with get, set
    member val colorReplacements: ColorReplacements option = nativeOnly with get, set
    member val defaultColor: U2<string, bool> option = nativeOnly with get, set
    member val colorsRendering: string option = nativeOnly with get, set
    member val cssVariablePrefix: string option = nativeOnly with get, set
    member val rootStyle: U2<string, bool> option = nativeOnly with get, set
    member val structure: string option = nativeOnly with get, set
    member val mergeWhitespaces: U2<bool, string> option = nativeOnly with get, set
    member val mergeSameStyleTokens: bool option = nativeOnly with get, set
    member val tokenizeMaxLineLength: int option = nativeOnly with get, set
    member val tokenizeTimeLimit: int option = nativeOnly with get, set
    member val grammarContextCode: string option = nativeOnly with get, set
    interface CodeOptions

/// Languages and themes to load before using a reusable highlighter.
[<AllowNullLiteral; Global>]
type HighlighterOptions
    [<ParamObject; Emit("$0")>]
    (langs: string[], themes: string[], ?langAlias: LanguageAliasMap, ?warnings: bool) =
    member val langs: string[] = nativeOnly with get, set
    member val themes: string[] = nativeOnly with get, set
    member val langAlias: LanguageAliasMap option = nativeOnly with get, set
    member val warnings: bool option = nativeOnly with get, set

/// A syntax token. Optional fields depend on the selected theme and output mode.
[<AllowNullLiteral>]
type ThemedToken =
    abstract content: string
    abstract offset: int
    abstract color: string option
    abstract bgColor: string option
    abstract fontStyle: int option
    abstract ``type``: int option
    abstract htmlStyle: obj option
    abstract htmlAttrs: obj option
    abstract variants: obj option

/// Token rows and the colors used for the highlighted code.
[<AllowNullLiteral>]
type TokensResult =
    abstract tokens: ThemedToken[][]
    abstract fg: string option
    abstract bg: string option
    abstract themeName: string option
    abstract rootStyle: U2<string, bool> option

/// A highlighter whose loaded languages and themes can be used synchronously.
[<AllowNullLiteral>]
type Highlighter =
    abstract codeToHtml: code: string * options: CodeOptions -> string
    abstract codeToHast: code: string * options: CodeOptions -> obj
    abstract codeToTokens: code: string * options: CodeOptions -> TokensResult
    abstract codeToTokensBase: code: string * options: SingleThemeOptions -> ThemedToken[][]
    abstract codeToTokensWithThemes: code: string * options: MultipleThemeOptions -> ThemedToken[][]
    abstract loadLanguage: lang: string -> JS.Promise<unit>
    abstract loadTheme: theme: string -> JS.Promise<unit>
    abstract getLoadedLanguages: unit -> string[]
    abstract getLoadedThemes: unit -> string[]
    abstract dispose: unit -> unit

/// Functions from Shiki's main bundle. Shorthand calls load and cache languages and themes.
[<Erase>]
type Shiki =
    [<Import("createHighlighter", "shiki")>]
    static member inline createHighlighter(options: HighlighterOptions): JS.Promise<Highlighter> = nativeOnly

    [<Import("codeToHtml", "shiki")>]
    static member inline codeToHtml(code: string, options: CodeOptions): JS.Promise<string> = nativeOnly

    [<Import("codeToHast", "shiki")>]
    static member inline codeToHast(code: string, options: CodeOptions): JS.Promise<obj> = nativeOnly

    [<Import("codeToTokens", "shiki")>]
    static member inline codeToTokens(code: string, options: CodeOptions): JS.Promise<TokensResult> = nativeOnly

    [<Import("codeToTokensBase", "shiki")>]
    static member inline codeToTokensBase(code: string, options: SingleThemeOptions): JS.Promise<ThemedToken[][]> = nativeOnly

    [<Import("codeToTokensWithThemes", "shiki")>]
    static member inline codeToTokensWithThemes(code: string, options: MultipleThemeOptions): JS.Promise<ThemedToken[][]> = nativeOnly
