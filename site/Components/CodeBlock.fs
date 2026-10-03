module Site.Components.CodeBlock

open Fable.Core
open Fable.Core.JsInterop
open Browser
open Browser.Types
open Fable.Shiki
open Firelight
open type Firelight.Lit

module Highlight =

    let private themes =
        ThemeMap.create [ "light", "github-light"; "dark", "github-dark" ]

    // The two GitHub colours below WCAG AA on their theme's background, swapped for AA-safe ones of
    // the same hue. The same as Renderer/Markdown.fs, which explains them.
    let private colorReplacements =
        ColorReplacements.create [
            "github-light", [ "#e36209", "#bc4c00" ]
            "github-dark", [ "#6a737d", "#959da5" ]
        ]

    // One highlighter for the whole page, created on first use. Only these grammars and themes
    // are bundled, and the JavaScript regex engine avoids downloading Oniguruma's WebAssembly.
    let private highlighter =
        lazy
            (ShikiCore.createHighlighterCore (
                HighlighterCoreOptions(
                    ShikiCore.createJavaScriptRegexEngine (),
                    langs =
                        [|
                            LanguageInput.ofImport (importDynamic "shiki/langs/fsharp.mjs")
                            LanguageInput.ofImport (importDynamic "shiki/langs/shellscript.mjs")
                        |],
                    themes =
                        [|
                            ThemeInput.ofImport (importDynamic "shiki/themes/github-light.mjs")
                            ThemeInput.ofImport (importDynamic "shiki/themes/github-dark.mjs")
                        |]
                )
            ))

    let toHtml (lang: string) (code: string) =
        async {
            let! h = highlighter.Value |> Async.AwaitPromise
            return h.codeToHtml (code, MultipleThemeOptions(lang, themes, colorReplacements = colorReplacements))
        }

/// Syntax-highlighted code.
///
/// Progressive enhancement: the plain `<pre>` in the light DOM is shown through a slot
/// until Shiki has loaded, then replaced by the highlighted version.
///
///     <fl-code lang="fsharp"><pre><code>let x = 1</code></pre></fl-code>
///
/// Inside a Lit template, set the `code` property instead: `<fl-code .code={source}>`.
[<AttachMembers>]
type CodeBlock() as this =
    inherit LitElement()

    let mutable source: string option = None
    let mutable highlighted: string option = None
    let mutable latest = 0

    let highlight (text: string) =
        latest <- latest + 1
        let request = latest

        async {
            try
                let! result = Highlight.toHtml this.lang text

                // Ignore results that arrive after a newer request.
                if request = latest then
                    highlighted <- Some result
                    this.requestUpdate ()
            with e ->
                console.error ($"fl-code: could not highlight '{this.lang}'", e)
        }
        |> Async.StartImmediate

    static member properties =
        PropertyDeclarations.create [ "lang", PropertyDeclaration<string>() ]

    static member styles =
        css
            $$"""
        :host { display: block; }
        pre {
            margin: 0;
            padding: var(--fl-code-padding, 1rem 1.25rem);
            border-radius: var(--fl-code-radius, 0.5rem);
            overflow-x: auto;
            font: var(--fl-code-font, 0.875rem/1.6 ui-monospace, monospace);
        }
        @media (prefers-color-scheme: dark) {
            .shiki, .shiki span {
                color: var(--shiki-dark) !important;
                background-color: var(--shiki-dark-bg) !important;
            }
        }
        """

    member val lang = "fsharp" with get, set

    member _.code
        with get () = source |> Option.defaultValue ""
        and set (value: string) =
            source <- Some value
            highlight value

    override _.connectedCallback() =
        base.connectedCallback ()

        if source.IsNone then
            let host: HTMLElement = unbox this
            highlight (host.textContent.Trim('\n'))

    override _.render() =
        match highlighted with
        | Some result -> html $"{unsafeHTML result}"
        | None when source.IsSome -> html $"<pre><code>{source.Value}</code></pre>"
        | None -> html $"<slot></slot>"

defineElement<CodeBlock> "fl-code"
