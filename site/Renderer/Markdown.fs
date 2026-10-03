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

    /// The two GitHub colours below WCAG AA's 4.5:1 on their theme's background, each swapped for a
    /// darker or lighter one of the same hue: github-light's orange (identifiers, 3.5:1 on white) and
    /// github-dark's comment grey (3.1:1 on #24292e).
    let private colorReplacements =
        ColorReplacements.create [
            "github-light", [ "#e36209", "#bc4c00" ] // 5.0:1
            "github-dark", [ "#6a737d", "#959da5" ] // 5.3:1
        ]

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

    /// A `<pre class="shiki">` with light colours inline and dark ones as CSS variables (site.css
    /// switches to them in dark mode). Unknown languages are shown as plain text, with a warning.
    let toHtml (highlighter: Highlighter) (lang: string) (code: string) =
        let lang = if String.IsNullOrWhiteSpace lang then "text" else lang
        // A fence's content ends with a newline, which Shiki would show as an empty last line.
        let code = code.TrimEnd('\n')

        try
            highlighter.codeToHtml (code, MultipleThemeOptions(lang, themes, colorReplacements = colorReplacements))
        with e ->
            JS.console.warn ($"Code block language '{lang}' is not loaded; showing it as plain text.", e.Message)
            highlighter.codeToHtml (code, MultipleThemeOptions("text", themes, colorReplacements = colorReplacements))

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
        /// More kinds of container, by name, rendered to HTML: those that need more than the page,
        /// such as `::: package-table`.
        Containers: Map<string, Container -> string>
    }

/// What rendering a page found, besides its HTML.
type Collected =
    {
        Headings: ResizeArray<Heading>
        Demos: ResizeArray<Demo>
        /// Demo HTML that must not be prerendered (`ssr=false`), left as placeholders in the HTML.
        Verbatim: ResizeArray<string>
        /// Every id given to a heading so far, including in containers rendered separately.
        Ids: HashSet<string>
    }

type Rendered =
    {
        /// The page body. `ssr=false` demos are placeholder comments, which Lit SSR passes through:
        /// `restoreVerbatim` puts them back after rendering, so their custom elements are never
        /// prerendered, even if another page has registered them.
        Html: string
        /// Anything before the first h2, which belongs with the page's heading and lead.
        Intro: string
        Verbatim: string list
        Headings: Heading list
        Demos: Demo list
    }

/// `{#id .same-section}` at the end of a heading.
let headingAttributes = Text.RegularExpressions.Regex "\\s*\\{([^{}]*)\\}\\s*$"

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

let private rawLink =
    // `<a ... href = "/path"` with either quote; `\s` before `href` keeps out `data-href`.
    Text.RegularExpressions.Regex "(<a\\s(?:[^>]*?\\s)?href\\s*=\\s*)([\"'])(/[^\"']*)\\2"

/// Adds the base path to root-relative `<a href>`s in raw HTML, so `<a href="/packages/router/">`
/// in Markdown works like `[text](/packages/router/)`. (The link check in the build catches any
/// form this misses.)
let rebaseHtml (``base``: string) (html: string) =
    rawLink.Replace(
        html,
        (fun m ->
            let quote = m.Groups[2].Value
            m.Groups[1].Value + quote + withBase ``base`` m.Groups[3].Value + quote
        )
    )

/// Tracks fenced code blocks line by line: the opening marker of the block that `line` leaves us
/// in, given the one we were in before it (None outside code).
let nextFence (fence: string option) (line: string) =
    let marker =
        let line = line.TrimStart()
        let run (c: char) = line.Length - line.TrimStart(c).Length

        if run '`' >= 3 then Some(String('`', run '`'))
        elif run '~' >= 3 then Some(String('~', run '~'))
        else None

    match fence, marker with
    | None, Some marker -> Some marker
    | Some opening, Some marker when
        marker.[0] = opening.[0]
        && marker.Length >= opening.Length
        && line.Trim() = marker
        ->
        None
    | _ -> fence

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
            // The opening marker of a fenced code block we're inside, whose `:::` lines are code.
            let mutable fence: string option = None

            while close < endLine && (fence.IsSome || lineText(close).Trim() <> ":::") do
                fence <- nextFence fence (lineText close)
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
                // Its lines, opening and closing marker included. (Set directly: the binding's type
                // for `map` is wrong.)
                token?map <- [| startLine; close + 1 |]
                state.line <- close + 1
                true
    )

// `::: example` and `::: demo` take an F# file under the site root, then options.
let private snippetArgs (c: Container) =
    // The path also becomes a module URL in a script, so keep it to plain path characters.
    let isPlainPath (file: string) =
        file
        |> Seq.forall (fun ch -> Char.IsLetterOrDigit ch || "/._-".Contains(string ch))
        && not (file.Contains "..")

    match c.Args with
    | file :: options when file.EndsWith ".fs" && isPlainPath file -> file, options
    | _ ->
        failwith
            $"Line {c.Line + 1}: '::: {c.Name}' needs an F# file under the site root, as in '::: {c.Name} Snippets/Counter.fs'."

// The live demo: the container's HTML in a `.demo` box, with the module that defines its custom
// elements loaded on the page. Options: `.class` adds a class to the box; `ssr=false` skips
// prerendering.
let private liveDemo (settings: Settings) (page: Collected) (c: Container) (file: string) (options: string list) =
    let isClass (option: string) =
        option.Length > 1
        && option.StartsWith "."
        && option.Substring(1)
           |> Seq.forall (fun ch -> Char.IsLetterOrDigit ch || ch = '-' || ch = '_')

    let classes = options |> List.filter isClass |> List.map _.Substring(1)
    let prerender = not (List.contains "ssr=false" options)

    match options |> List.filter (fun o -> not (isClass o) && o <> "ssr=false") with
    | [] -> ()
    | unknown -> failwith $"Line {c.Line + 1}: unknown ::: {c.Name} options {unknown}."

    page.Demos.Add
        {
            Module = "/build/" + file.Substring(0, file.Length - 3) + ".js"
            Prerender = prerender
        }

    let body = rebaseHtml settings.Base c.Body

    let demo =
        if prerender then
            body
        else
            page.Verbatim.Add body
            $"<!--firelight-verbatim:{page.Verbatim.Count - 1}-->"

    let demoClass = String.Join(" ", "demo" :: classes)
    $"<div class=\"{demoClass}\">\n{demo}\n</div>"

/// `::: example <file.fs> [.class ...] [ssr=false]`, with the demo's HTML as the body: the source
/// file next to the live demo. Its module is loaded on the page and, unless `ssr=false`, its
/// custom elements are prerendered. With no body, just the code.
let private example (settings: Settings) (page: Collected) (c: Container) =
    let file, options = snippetArgs c

    let code =
        Highlight.toHtml settings.Highlighter "fsharp" (readText settings.Root file)

    if String.IsNullOrWhiteSpace c.Body then
        if not options.IsEmpty then
            failwith $"Line {c.Line + 1}: options {options} need a demo in the body."

        code
    else
        $"<div class=\"example\">\n{code}\n{liveDemo settings page c file options}\n</div>\n"

/// `::: demo <file.fs> [.class ...] [ssr=false]`: the live demo without its code, for pages that
/// show the code somewhere else.
let private demo (settings: Settings) (page: Collected) (c: Container) =
    let file, options = snippetArgs c

    if String.IsNullOrWhiteSpace c.Body then
        failwith $"Line {c.Line + 1}: '::: demo' needs the demo's HTML in its body."

    liveDemo settings page c file options + "\n"

/// The `::: name args` block that a `container` token stands for.
let containerOf (t: Token) =
    {
        Name = t.info.Split(' ').[0]
        Args = t.info.Split(' ') |> List.ofArray |> List.tail
        Body = t.content
        Line = unbox t.meta.Value
    }

// Core rule, after inline parsing: heading ids and the table of contents, `<section>` around each
// h2 and what follows it (the page styles space sections), and base-path and asset links.
let private postProcess (settings: Settings) (md: MarkdownIt) =
    MarkdownIt.Core.RuleCore(fun state ->
        let page = collected state.env
        let ids = page.Ids
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

                // markdown-it's [start, end] lines of the block, from 0.
                let line: int = if isNull t?map then 0 else t?map?(0) + 1

                // `## Heading {#id .same-section}`: an id of its own, and for an h2, staying in the
                // section before it rather than starting one.
                let explicitId, sameSection =
                    match Array.tryLast children with
                    | Some last when last.``type`` = "text" && headingAttributes.IsMatch last.content ->
                        let m = headingAttributes.Match last.content
                        last.content <- last.content.Substring(0, m.Index)
                        state.tokens[i + 1].content <- headingAttributes.Replace(state.tokens[i + 1].content, "")

                        let attributes =
                            m.Groups[1].Value.Split(' ') |> Array.filter (String.IsNullOrWhiteSpace >> not)

                        let id = attributes |> Array.tryFind _.StartsWith("#") |> Option.map _.Substring(1)

                        match
                            attributes
                            |> Array.filter (fun a -> not (a.StartsWith "#") && a <> ".same-section")
                        with
                        | [||] -> ()
                        | unknown ->
                            let unknown = String.Join(" ", unknown)
                            failwith $"Line {line}: unknown heading attributes {unknown} (known: #id, .same-section)."

                        match id with
                        | Some "" -> failwith $"Line {line}: '#' needs an id after it."
                        | Some id when not (ids.Add id) ->
                            failwith $"Line {line}: another heading already has the id '{id}'."
                        | _ -> ()

                        id, Array.contains ".same-section" attributes
                    | _ -> None, false

                let text =
                    children
                    |> Array.filter (fun c -> c.``type`` = "text" || c.``type`` = "code_inline")
                    |> Array.map _.content
                    |> String.concat ""

                let id =
                    match explicitId with
                    | Some id -> id
                    | None ->
                        let baseId = slug text
                        let mutable id = baseId
                        let mutable n = 1

                        while not (ids.Add id) do
                            n <- n + 1
                            id <- $"{baseId}-{n}"

                        id

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

                if t.tag = "h2" && t.level = 0 && not (sameSection && inSection) then
                    if inSection then
                        tokens.Add(blockToken "section_close" "section" -1)

                    tokens.Add(blockToken "section_open" "section" 1)
                    inSection <- true

            if t.``type`` = "html_block" then
                t.content <- rebaseHtml settings.Base t.content

            if t.``type`` = "inline" then
                for c in t.children |> Option.defaultValue [||] do
                    match c.``type`` with
                    | "html_inline" -> c.content <- rebaseHtml settings.Base c.content
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

    // `::: compare`: Markdown holding two code blocks (Lit in TypeScript and Firelight, say), shown
    // side by side with CSS only. The blocks are ordinary fences, so a plain ```fsharp one is
    // compiled by tests/Docs.Snippets like any other.
    let compare (env: obj option) (c: Container) =
        if not c.Args.IsEmpty then
            failwith $"Line {c.Line + 1}: '::: compare' takes no options."

        $"<div class=\"compare\">\n{md.render (c.Body, env.Value)}</div>\n"

    // `::: cards`: Markdown in which each `###` heading starts a card, laid out in a grid.
    let cards (env: obj option) (c: Container) =
        if not c.Args.IsEmpty then
            failwith $"Line {c.Line + 1}: '::: cards' takes no options."

        let lines = c.Body.TrimEnd('\n').Split('\n')

        if not (lines[0].StartsWith "### ") then
            failwith $"Line {c.Line + 2}: '::: cards' starts with a '### ' heading for its first card."

        // Card headings, not `###` lines in fenced code.
        let starts =
            lines
            |> Array.scan
                (fun (fence, _) line -> nextFence fence line, fence.IsNone && line.StartsWith "### ")
                (None, false)
            |> Array.tail
            |> Array.indexed
            |> Array.filter (snd >> snd)
            |> Array.map fst
            |> List.ofArray

        // The cards' headings stay out of the table of contents: they would come after every
        // other heading, as cards are rendered once the page has been parsed.
        let env =
            Some(
                box
                    { collected env with
                        Headings = ResizeArray()
                    }
            )

        let articles =
            starts
            |> List.mapi (fun n start ->
                let finish = List.tryItem (n + 1) starts |> Option.defaultValue lines.Length
                let card = String.Join("\n", lines[start .. finish - 1])
                $"<article>\n{md.render (card, env.Value)}</article>\n"
            )

        "<div class=\"cards\">\n" + String.concat "" articles + "</div>\n"

    // Containers are rendered here, by name. This is the hook for new kinds.
    md.renderer.rules["container"] <-
        Some(
            Renderer.RenderRule(fun tokens idx _ env _ ->
                let t = tokens[idx]

                let container = containerOf t

                match container.Name with
                | "example" -> example settings (collected env) container
                | "demo" -> demo settings (collected env) container
                | "compare" -> compare env container
                | "cards" -> cards env container
                | name when settings.Containers.ContainsKey name -> settings.Containers[name] container
                | name -> failwith $"Line {container.Line + 1}: unknown container '::: {name}'."
            )
        )

    md

/// Renders a page body. Line numbers in errors count from the start of `markdown`.
let private newCollected () =
    {
        Headings = ResizeArray()
        Demos = ResizeArray()
        Verbatim = ResizeArray()
        Ids = HashSet()
    }

/// A page body's tokens, as `render` sees them before rendering. A `container` token's `map` holds
/// its lines, as for other blocks.
let parse (md: MarkdownIt) (markdown: string) : Token[] =
    md.parse (markdown, Some(box (newCollected ())))

let render (md: MarkdownIt) (markdown: string) : Rendered =
    let page = newCollected ()

    let tokens = md.parse (markdown, Some(box page))

    let firstSection =
        tokens
        |> Array.tryFindIndex (fun t -> t.``type`` = "section_open")
        |> Option.defaultValue tokens.Length

    let html (tokens: Token[]) =
        md.renderer.render (tokens, md.options, Some(box page))

    {
        Html = html tokens[firstSection..]
        Intro = html tokens[.. firstSection - 1]
        Verbatim = List.ofSeq page.Verbatim
        Headings = List.ofSeq page.Headings
        Demos = List.ofSeq page.Demos
    }

/// Puts the `ssr=false` demos back into the rendered page.
let restoreVerbatim (rendered: Rendered) (pageHtml: string) =
    let verbatim = Array.ofList rendered.Verbatim
    verbatimPlaceholder.Replace(pageHtml, (fun m -> verbatim[int m.Groups[1].Value]))
