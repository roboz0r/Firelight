/// The site as Markdown, for coding agents and anything else that reads text: each page's
/// `index.md`, `llms.txt` (an index of them, as https://llmstxt.org/ describes) and `llms-full.txt`
/// (every page in one file). Links are absolute, `::: example` becomes the snippet's source and the
/// demo's HTML, other containers become plain Markdown, and the frontmatter keeps only the title
/// and description.
module Site.Renderer.Agents

open System
open System.Text.RegularExpressions
open Fable.Core
open Fable.Core.JsInterop
open MarkdownIt
open Site.Renderer.Pages

type Settings =
    {
        /// The site root, which snippet paths are relative to.
        Root: string
        /// Vite's base path, such as `/Firelight/`.
        Base: string
        Site: Page list
        /// The site's markdown-it instance, to find the blocks of a page.
        Md: MarkdownIt
        /// The address of the page being converted, which `#anchor` links are on.
        Here: string
    }

/// The page's address: `https://roboz0r.github.io/Firelight/guides/templates/`.
let pageUrl (``base``: string) (page: Page) = origin + ``base`` + page.Route

/// The address of its Markdown version.
let markdownUrl (``base``: string) (page: Page) = pageUrl ``base`` page + "index.md"

/// Its Markdown version in `dist/`: `guides/templates/index.md`.
let markdownFile (page: Page) = page.Route + "index.md"

// `/path` on the site and `#anchor` on the page itself, as absolute addresses.
let private absolute (s: Settings) (url: string) =
    if url.StartsWith "/" && not (url.StartsWith "//") then
        origin + Markdown.withBase s.Base url
    elif url.StartsWith "#" then
        s.Here + url
    else
        url

// Links within the site: `[text](/path)`, `[text](</path>)`, `[text](#anchor)`, `[ref]: /path` and
// `href="/path"` in raw HTML. Code spans, which come first, are left as they are.
let private siteLinks =
    Regex(
        "(`+)[^`]*?\\1|(\\]\\(\\s*<?|^\\s*\\[[^\\]]+\\]:\\s*<?|\\shref\\s*=\\s*[\"'])([/#][^)\\s\"'>]*)",
        RegexOptions.Multiline
    )

let private absoluteLinks (s: Settings) (text: string) =
    siteLinks.Replace(
        text,
        (fun m ->
            if m.Groups[1].Success then
                m.Value
            else
                m.Groups[2].Value + absolute s m.Groups[3].Value
        )
    )

/// A fenced code block, with a fence longer than any run of backticks in the code.
let private fenced (lang: string) (code: string) =
    let longest =
        Regex.Matches(code, "`+")
        |> Seq.cast<Match>
        |> Seq.map _.Length
        |> Seq.fold max 0

    let fence = String('`', max 3 (longest + 1))
    $"{fence}{lang}\n{code.TrimEnd()}\n{fence}"

let private packageTable (s: Settings) =
    let rows =
        inSection "packages" s.Site
        |> List.map (fun p ->
            let summary =
                (p.Meta.Summary |> Option.defaultValue p.Meta.Description).Replace("|", "\\|")

            $"| [{p.Meta.Title}]({pageUrl s.Base p}) | {absoluteLinks { s with Here = pageUrl s.Base p } summary} |"
        )

    String.Join("\n", "| Package | What it does |" :: "| --- | --- |" :: rows)

// Markdown with its containers made plain and its links absolute. Code is left as written.
let rec private convert (s: Settings) (markdown: string) : string =
    let lines = markdown.Split '\n'

    // Blocks replaced whole, by first line: their end (exclusive) and what they become.
    let blocks = Collections.Generic.Dictionary<int, int * string>()

    for t in Markdown.parse s.Md markdown do
        let map: int[] = t?map

        if not (isNull map) then
            let first, last = map[0], map[1]

            match t.``type`` with
            | "container" -> blocks[first] <- (last, container s (Markdown.containerOf t))
            | "fence"
            | "code_block" -> blocks[first] <- (last, String.Join("\n", lines[first .. last - 1]))
            // Page scripts are for the browser.
            | "html_block" when t.content.TrimStart().StartsWith "<script" -> blocks[first] <- (last, "")
            | "heading_open" ->
                // A heading without its `{#id}`; a setext heading's underline stays.
                let heading =
                    Markdown.headingAttributes.Replace(lines[first], "")
                    :: List.ofArray lines[first + 1 .. last - 1]

                blocks[first] <- (last, absoluteLinks s (String.Join("\n", heading)))
            | _ -> ()

    let output = ResizeArray<string>()
    let mutable i = 0

    while i < lines.Length do
        match blocks.TryGetValue i with
        | true, (last, text) ->
            output.Add text
            i <- max last (i + 1)
        | _ ->
            output.Add(absoluteLinks s lines[i])
            i <- i + 1

    String.Join("\n", output)

and private container (s: Settings) (c: Markdown.Container) =
    let demo () =
        if String.IsNullOrWhiteSpace c.Body then
            None
        else
            Some(fenced "html" (c.Body.Trim()))

    match c.Name with
    | "example" ->
        let code = fenced "fsharp" (Markdown.readText s.Root c.Args.Head)

        match demo () with
        | Some demo -> code + "\n\n" + demo
        | None -> code
    | "demo" -> demo () |> Option.defaultValue ""
    | "compare"
    | "cards" -> (convert s c.Body).Trim()
    | "package-table" -> packageTable s
    | name -> failwith $"Line {c.Line + 1}: no Markdown form for '::: {name}'; add one in Renderer/Agents.fs."

// The page's heading, lead, links and body. `header` goes between the heading and the rest.
let private content (s: Settings) (page: Page) (header: string option) =
    let s = { s with Here = pageUrl s.Base page }
    let meta = page.Meta

    let links =
        meta.Links |> List.map (fun link -> $"- [{link.Text}]({absolute s link.Href})")

    [
        $"# {meta.Title}"
        yield! Option.toList header
        yield! meta.Lead |> Option.map (absoluteLinks s) |> Option.toList
        String.Join("\n", links)
        (convert s page.Body).Trim()
    ]
    |> List.filter (String.IsNullOrWhiteSpace >> not)
    |> String.concat "\n\n"

/// A page's `index.md`.
let document (s: Settings) (page: Page) =
    let yaml (text: string) = JS.JSON.stringify text

    $"---\ntitle: {yaml page.Meta.Title}\ndescription: {yaml page.Meta.Description}\n---\n\n"
    + content s page None
    + "\n"

/// The pages with a Markdown version, in navigation order: the homepage, then each section's
/// pages in header order. (Not the not-found page.)
let pages (s: Settings) =
    (s.Site |> List.filter _.Meta.Home)
    @ (sections |> List.collect (fun section -> inSection section.Id s.Site))

/// `llms-full.txt`: every page, in navigation order.
let full (s: Settings) =
    pages s
    |> List.map (fun page -> content s page (Some $"Source: {pageUrl s.Base page}"))
    |> String.concat "\n\n"
    |> fun text -> text + "\n"

/// `llms.txt`: what Firelight is, then a link to each page's Markdown version with a line about it.
let index (s: Settings) =
    let entry (page: Page) =
        let summary =
            (page.Meta.Summary |> Option.defaultValue page.Meta.Description).Replace("\n", " ")

        $"- [{page.Meta.Title}]({markdownUrl s.Base page}): {absoluteLinks { s with Here = pageUrl s.Base page } summary}"

    let home = s.Site |> List.tryFind _.Meta.Home

    let list heading (pages: Page list) =
        if pages.IsEmpty then
            None
        else
            Some($"## {heading}\n\n" + String.Join("\n", pages |> List.map entry))

    [
        "# Firelight"
        yield! home |> Option.map (fun home -> "> " + home.Meta.Description) |> Option.toList
        $"Each page of the site is also Markdown: add `index.md` to its address. [llms-full.txt]({origin}{s.Base}llms-full.txt) has every page in one file, in the order below."
        yield! list "Home" (Option.toList home) |> Option.toList
        yield!
            sections
            |> List.choose (fun section -> list section.Name (inSection section.Id s.Site))
    ]
    |> String.concat "\n\n"
    |> fun text -> text + "\n"
