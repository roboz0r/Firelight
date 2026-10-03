namespace Fable.Shiki

open Fable.Core
open Fable.Core.JsInterop

/// A language for a core highlighter: a grammar, a module whose default export is a grammar,
/// or a promise of either.
type LanguageInput = interface end

[<Erase>]
module LanguageInput =
    /// A grammar module loaded on demand. The path must be a literal so the bundler can split it out:
    ///
    ///     LanguageInput.ofImport (importDynamic "shiki/langs/fsharp.mjs")
    let inline ofImport (modulePromise: JS.Promise<obj>) : LanguageInput = !!modulePromise

/// A theme for a core highlighter: a theme object, a module whose default export is a theme,
/// or a promise of either.
type ThemeInput = interface end

[<Erase>]
module ThemeInput =
    /// A theme module loaded on demand. The path must be a literal so the bundler can split it out:
    ///
    ///     ThemeInput.ofImport (importDynamic "shiki/themes/github-dark.mjs")
    let inline ofImport (modulePromise: JS.Promise<obj>) : ThemeInput = !!modulePromise

/// Matches the regular expressions in TextMate grammars.
type RegexEngine = interface end

/// Options for the JavaScript regex engine.
[<AllowNullLiteral; Global>]
type JavaScriptRegexEngineOptions [<ParamObject; Emit("$0")>] (?forgiving: bool, ?target: string) =
    /// Skip patterns that JavaScript regular expressions can't express, instead of throwing.
    member val forgiving: bool option = nativeOnly with get, set
    /// "auto" (default), "ES2025", "ES2024" or "ES2018".
    member val target: string option = nativeOnly with get, set

/// The engine, languages and themes for a core highlighter. Nothing is bundled: every language
/// and theme it uses must be listed here or loaded later.
[<AllowNullLiteral; Global>]
type HighlighterCoreOptions
    [<ParamObject; Emit("$0")>]
    (engine: RegexEngine, ?langs: LanguageInput[], ?themes: ThemeInput[], ?langAlias: LanguageAliasMap, ?warnings: bool)
    =
    member val engine: RegexEngine = nativeOnly with get, set
    member val langs: LanguageInput[] option = nativeOnly with get, set
    member val themes: ThemeInput[] option = nativeOnly with get, set
    member val langAlias: LanguageAliasMap option = nativeOnly with get, set
    member val warnings: bool option = nativeOnly with get, set

/// A highlighter from `shiki/core`. Highlighting is synchronous once it has been created.
/// Languages and themes are referred to by name after they have been loaded.
[<AllowNullLiteral>]
type HighlighterCore =
    abstract codeToHtml: code: string * options: CodeOptions -> string
    abstract codeToHast: code: string * options: CodeOptions -> obj
    abstract codeToTokens: code: string * options: CodeOptions -> TokensResult
    abstract codeToTokensBase: code: string * options: SingleThemeOptions -> ThemedToken[][]
    abstract codeToTokensWithThemes: code: string * options: MultipleThemeOptions -> ThemedToken[][]
    abstract loadLanguage: lang: LanguageInput -> JS.Promise<unit>
    abstract loadTheme: theme: ThemeInput -> JS.Promise<unit>
    abstract getLoadedLanguages: unit -> string[]
    abstract getLoadedThemes: unit -> string[]
    abstract dispose: unit -> unit

/// Fine-grained bundle: `shiki/core` with an explicit regex engine, languages and themes.
/// Only what is listed ends up in the bundle, unlike the main `shiki` entry.
[<Erase>]
type ShikiCore =
    [<Import("createHighlighterCore", "shiki/core")>]
    static member inline createHighlighterCore(options: HighlighterCoreOptions) : JS.Promise<HighlighterCore> =
        nativeOnly

    /// The JavaScript RegExp engine: no WebAssembly download, but a few grammars use patterns it can't express.
    [<Import("createJavaScriptRegexEngine", "shiki/engine/javascript")>]
    static member inline createJavaScriptRegexEngine(?options: JavaScriptRegexEngineOptions) : RegexEngine = nativeOnly
