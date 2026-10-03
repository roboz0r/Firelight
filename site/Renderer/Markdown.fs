/// Markdown to HTML: markdown-it, plus build-time Shiki highlighting, heading ids and a table of
/// contents, `<section>` per h2, base-path links, and `::: name` containers such as `::: example`.
module Site.Renderer.Markdown

open System
open System.Collections.Generic
open Fable.Core
open Fable.Core.JsInterop
open Fable.Shiki
open MarkdownIt

[<Import("readFileSync", "node:fs")>]
let private readFileSync (path: string, encoding: string) : string = jsNative

[<Import("resolve", "node:path")>]
let private resolvePath (dir: string, file: string) : string = jsNative

/// A file relative to the site root, with Windows line endings and trailing whitespace removed
/// (the same text the browser-side includes show).
let readText (root: string) (file: string) =
    readFileSync(resolvePath (root, file), "utf8").Replace("\r\n", "\n").TrimEnd()

module Highlight =

    let private themes =
        ThemeMap.create [ "light", "github-light"; "dark", "github-dark" ]

    /// Languages that fenced code blocks can use, by name or alias (`sh` for shellscript, `fs` for fsharp...).
    let languages =
        [|
            "fsharp"
            "shellscript"
            "powershell"
            "typescript"
            "javascript"
            "html"
            "css"
            "json"
        |]

    /// Shiki's full bundle with the Oniguruma engine: in Node at build time, size doesn't matter.
    let create () =
        Shiki.createHighlighter (HighlighterOptions(languages, [| "github-light"; "github-dark" |]))

    /// A `<pre class="shiki">` with light colours inline and dark ones as CSS variables, the same
    /// output as `fl-code`. Unknown languages are shown as plain text, with a warning.
    let toHtml (highlighter: Highlighter) (lang: string) (code: string) =
        let lang = if String.IsNullOrWhiteSpace lang then "text" else lang
        // A fence's content ends with a newline, which Shiki would show as an empty last line.
        let code = code.TrimEnd('\n')

        try
            highlighter.codeToHtml (code, MultipleThemeOptions(lang, themes))
        with e ->
            JS.console.warn ($"Code block language '{lang}' is not loaded; showing it as plain text.", e.Message)
            highlighter.codeToHtml (code, MultipleThemeOptions("text", themes))

/// An h2 or h3, for the table of contents. `Html` is the heading's rendered inline content.
type Heading =
    { Level: int; Id: string; Html: string }

/// A live demo on the page: the module that defines its custom elements, and whether to prerender it.
type Demo = { Module: string; Prerender: bool }

/// A `::: name args` block. `Body` is the raw text up to the closing `:::`, not parsed as Markdown.
type Container =
    {
        Name: string
        Args: string list
        Body: string
        Line: int
    }

type Settings =
    {
        /// The site root: snippet paths in `::: example` are relative to it.
        Root: string
        /// Vite's base path, such as `/Firelight/`. Prefixed to root-relative links.
        Base: string
        Highlighter: Highlighter
    }

/// What rendering a page found, besides its HTML.
type Collected =
    {
        Headings: ResizeArray<Heading>
        Demos: ResizeArray<Demo>
        /// Demo HTML that must not be prerendered (`ssr=false`), left as placeholders in the HTML.
        Verbatim: ResizeArray<string>
    }

type Rendered =
    {
        /// The page body. `ssr=false` demos are placeholder comments, which Lit SSR passes through:
        /// `restoreVerbatim` puts them back after rendering, so their custom elements are never
        /// prerendered, even if another page has registered them.
        Html: string
        Verbatim: string list
        Headings: Heading list
        Demos: Demo list
    }

let private verbatimPlaceholder =
    Text.RegularExpressions.Regex "<!--firelight-verbatim:(\d+)-->"

// markdown-it passes `env` to every rule; it carries what this render collects.
let private collected (env: obj option) : Collected = unbox env.Value

[<Emit("new $0.Token($1, $2, $3)")>]
let private newToken (state: StateCore) (``type``: string) (tag: string) (nesting: int) : Token = jsNative

/// Lowercase letters and digits, with runs of spaces, `-` and `_` as one dash: "Why use it" -> "why-use-it".
let slug (text: string) =
    let sb = Text.StringBuilder()
    let mutable dash = false

    for c in text do
        if Char.IsLetterOrDigit c then
            if dash && sb.Length > 0 then
                sb.Append '-' |> ignore

            if sb.Length = 0 && Char.IsDigit c then
                sb.Append "id-" |> ignore

            sb.Append(Char.ToLowerInvariant c) |> ignore
            dash <- false
        elif c = ' ' || c = '-' || c = '_' then
            dash <- true

    if sb.Length = 0 then "section" else sb.ToString()

// Links to files (rather than pages) open in a new tab.
let private assetExtensions =
    set [
        "pdf"
        "zip"
        "gz"
        "7z"
        "csv"
        "txt"
        "json"
        "xml"
        "mp3"
        "mp4"
        "webm"
        "png"
        "jpg"
        "jpeg"
        "gif"
        "svg"
        "webp"
    ]

let private isAsset (href: string) =
    let path = href.Split([| '?'; '#' |]).[0]
    let name = path.Substring(path.LastIndexOf '/' + 1)
    let dot = name.LastIndexOf '.'
    dot > 0 && assetExtensions.Contains(name.Substring(dot + 1).ToLowerInvariant())

/// Root-relative URLs (`/guides/events/`) get the base path; others are left alone.
let withBase (``base``: string) (url: string) =
    if url.StartsWith "/" && not (url.StartsWith "//") then
        ``base`` + url.Substring 1
    else
        url

// Block rule: `::: name args` up to a line that is just `:::`, as one `container` token.
// Containers don't nest, and an unclosed one is an error rather than swallowing the page.
let private containerRule =
    MarkdownIt.ParserBlock.RuleBlock(fun state startLine endLine silent ->
        let lineText line =
            let start = state.bMarks[line] + state.tShift[line]
            state.src.Substring(start, state.eMarks[line] - start)

        let first = lineText startLine

        // Indented four or more spaces, it's an indented code block.
        if state.sCount[startLine] - state.blkIndent >= 4 || not (first.StartsWith ":::") then
            false
        elif silent then
            true
        else
            let mutable close = startLine + 1

            while close < endLine && lineText(close).Trim() <> ":::" do
                close <- close + 1

            let words =
                first.Substring(3).Split(' ')
                |> Array.filter (String.IsNullOrWhiteSpace >> not)
                |> List.ofArray

            match words with
            | [] ->
                failwith $"Line {startLine + 1}: a container needs a name, as in '::: example Snippets/Counter.fs'."
            | _ when close >= endLine -> failwith $"Line {startLine + 1}: '{first}' has no closing ':::'."
            | name :: args ->
                let token = state.push ("container", "div", MarkdownIt.Token.Nesting.SelfClosing)
                token.info <- String.Join(" ", name :: args)
                token.content <- state.getLines (startLine + 1, close, state.blkIndent, false)
                token.meta <- Some(box startLine)
                state.line <- close + 1
                true
    )

/// `::: example <file.fs> [.class ...] [ssr=false]`, with the demo's HTML as the body: the source
/// file next to the live demo. Its module is loaded on the page and, unless `ssr=false`, its
/// custom elements are prerendered. With no body, just the code.
let private example (settings: Settings) (page: Collected) (c: Container) =
    // The path also becomes a module URL in a script, so keep it to plain path characters.
    let isPlainPath (file: string) =
        file
        |> Seq.forall (fun ch -> Char.IsLetterOrDigit ch || "/._-".Contains(string ch))
        && not (file.Contains "..")

    match c.Args with
    | file :: options when file.EndsWith ".fs" && isPlainPath file ->
        let code =
            Highlight.toHtml settings.Highlighter "fsharp" (readText settings.Root file)

        if String.IsNullOrWhiteSpace c.Body then
            code
        else
            let isClass (option: string) =
                option.Length > 1
                && option.StartsWith "."
                && option.Substring(1)
                   |> Seq.forall (fun ch -> Char.IsLetterOrDigit ch || ch = '-' || ch = '_')

            let classes = options |> List.filter isClass |> List.map _.Substring(1)
            let prerender = not (List.contains "ssr=false" options)

            match options |> List.filter (fun o -> not (isClass o) && o <> "ssr=false") with
            | [] -> ()
            | unknown -> failwith $"Line {c.Line + 1}: unknown ::: example options {unknown}."

            page.Demos.Add
                {
                    Module = "/build/" + file.Substring(0, file.Length - 3) + ".js"
                    Prerender = prerender
                }

            let demo =
                if prerender then
                    c.Body
                else
                    page.Verbatim.Add c.Body
                    $"<!--firelight-verbatim:{page.Verbatim.Count - 1}-->"

            let demoClass = String.Join(" ", "demo" :: classes)
            $"<div class=\"example\">\n{code}\n<div class=\"{demoClass}\">\n{demo}\n</div>\n</div>\n"
    | _ ->
        failwith
            $"Line {c.Line + 1}: '::: example' needs an F# file under the site root, as in '::: example Snippets/Counter.fs'."

// Core rule, after inline parsing: heading ids and the table of contents, `<section>` around each
// h2 and what follows it (the page styles space sections), and base-path and asset links.
let private postProcess (settings: Settings) (md: MarkdownIt) =
    MarkdownIt.Core.RuleCore(fun state ->
        let page = collected state.env
        let ids = HashSet<string>()
        let tokens = ResizeArray<Token>()
        let mutable inSection = false

        let blockToken ``type`` tag nesting =
            let t = newToken state ``type`` tag nesting
            t.block <- true
            t

        for i in 0 .. state.tokens.Length - 1 do
            let t = state.tokens[i]

            if t.``type`` = "heading_open" then
                let children = state.tokens[i + 1].children |> Option.defaultValue [||]

                let text =
                    children
                    |> Array.filter (fun c -> c.``type`` = "text" || c.``type`` = "code_inline")
                    |> Array.map _.content
                    |> String.concat ""

                let baseId = slug text
                let mutable id = baseId
                let mutable n = 1

                while not (ids.Add id) do
                    n <- n + 1
                    id <- $"{baseId}-{n}"

                t.attrSet ("id", id)
                let level = int (t.tag.Substring 1)

                // The TOC entry keeps inline formatting but not links: it becomes a link itself.
                if level = 2 || level = 3 then
                    let label =
                        children
                        |> Array.filter (fun c -> c.``type`` <> "link_open" && c.``type`` <> "link_close")

                    page.Headings.Add
                        {
                            Level = level
                            Id = id
                            Html = md.renderer.renderInline (label, md.options, state.env)
                        }

                if t.tag = "h2" && t.level = 0 then
                    if inSection then
                        tokens.Add(blockToken "section_close" "section" -1)

                    tokens.Add(blockToken "section_open" "section" 1)
                    inSection <- true

            if t.``type`` = "inline" then
                for c in t.children |> Option.defaultValue [||] do
                    match c.``type`` with
                    | "link_open" ->
                        match c.attrGet "href" with
                        | Some href ->
                            c.attrSet ("href", withBase settings.Base href)

                            if isAsset href then
                                c.attrSet ("target", "_blank")
                                c.attrSet ("rel", "noopener noreferrer")
                        | None -> ()
                    | "image" ->
                        c.attrGet "src"
                        |> Option.iter (fun src -> c.attrSet ("src", withBase settings.Base src))
                    | _ -> ()

            tokens.Add t

        if inSection then
            tokens.Add(blockToken "section_close" "section" -1)

        state.tokens <- tokens.ToArray()
    )

/// A markdown-it instance for the site. Raw HTML is allowed (demo markup, page scripts). Quotes,
/// dashes and line breaks are left as written, so migrated pages keep their exact text.
let create (settings: Settings) =
    let md =
        markdownit.Invoke(
            Options(
                html = true,
                highlight =
                    MarkdownIt.HighlightOptions(fun code lang _ -> Highlight.toHtml settings.Highlighter lang code)
            )
        )

    md.block.ruler.before (
        "fence",
        "container",
        containerRule,
        !!{|
            alt = [| "paragraph"; "reference"; "blockquote"; "list" |]
        |}
    )

    md.core.ruler.push ("site_post_process", postProcess settings md)

    // Containers are rendered here, by name. This is the hook for new kinds (`::: compare`...).
    md.renderer.rules["container"] <-
        Some(
            Renderer.RenderRule(fun tokens idx _ env _ ->
                let t = tokens[idx]

                let container =
                    {
                        Name = t.info.Split(' ').[0]
                        Args = t.info.Split(' ') |> List.ofArray |> List.tail
                        Body = t.content
                        Line = unbox t.meta.Value
                    }

                match container.Name with
                | "example" -> example settings (collected env) container
                | name -> failwith $"Line {container.Line + 1}: unknown container '::: {name}'."
            )
        )

    md

/// Renders a page body. Line numbers in errors count from the start of `markdown`.
let render (md: MarkdownIt) (markdown: string) : Rendered =
    let page =
        {
            Headings = ResizeArray()
            Demos = ResizeArray()
            Verbatim = ResizeArray()
        }

    {
        Html = md.render (markdown, page)
        Verbatim = List.ofSeq page.Verbatim
        Headings = List.ofSeq page.Headings
        Demos = List.ofSeq page.Demos
    }

/// Puts the `ssr=false` demos back into the rendered page.
let restoreVerbatim (rendered: Rendered) (pageHtml: string) =
    let verbatim = Array.ofList rendered.Verbatim
    verbatimPlaceholder.Replace(pageHtml, (fun m -> verbatim[int m.Groups[1].Value]))
