/// The Markdown pages in content/: where each one is served, and its frontmatter.
module Site.Renderer.Pages

open System
open Fable.Core
open Fable.Core.JsInterop

[<Import("parse", "yaml")>]
let private parseYaml (text: string) : obj = jsNative

[<Import("readdirSync", "node:fs")>]
let private readdirSync (dir: string, options: obj) : string[] = jsNative

[<Import("resolve", "node:path")>]
let private resolvePath (dir: string, file: string) : string = jsNative

[<Import("readFileSync", "node:fs")>]
let private readFileSync (path: string, encoding: string) : string = jsNative

/// Where the site is published, for absolute URLs (canonical links, Open Graph, the sitemap).
/// The base path (`/Firelight/`) comes from Vite.
let origin = "https://roboz0r.github.io"

type Link = { Text: string; Href: string }

/// A part of the site that pages declare in their frontmatter (`section: packages`).
type Section =
    {
        Id: string
        /// Its name in the header.
        Name: string
        /// Where the header links to. Root-relative; the base path is added when rendering.
        Href: string
    }

/// The site's sections, in header order. A page's `section` must be one of these, so adding a
/// section is one line here.
let sections =
    [
        {
            Id = "packages"
            Name = "Packages"
            Href = "/#packages"
        }
        {
            Id = "demos"
            Name = "Demos"
            Href = "/#demos"
        }
    ]

/// The YAML block at the top of a page, between `---` lines.
type Frontmatter =
    {
        /// The page heading and its name in navigation.
        Title: string
        /// Completes the `<title>`: "Firelight: Lit web components in F#".
        Tagline: string option
        /// The meta description.
        Description: string
        /// The site section the page belongs to, such as `packages` or `guides`.
        Section: string
        /// Position within the section.
        Order: int
        /// One line for lists of pages, such as the homepage package table. Inline Markdown.
        Summary: string option
        /// The paragraph under the heading. Inline Markdown.
        Lead: string option
        /// Links shown under the lead (Lit docs, NuGet, source).
        Links: Link list
        /// Show an h2/h3 table of contents.
        Toc: bool
    }

type Page =
    {
        /// The Markdown file, relative to the site root: `content/packages/firelight.md`.
        Source: string
        /// The page's URL relative to the base path: `packages/firelight/`.
        Route: string
        /// The HTML file it becomes, relative to the site root and `dist/`: `packages/firelight/index.html`.
        Output: string
        Meta: Frontmatter
        /// The Markdown after the frontmatter. The frontmatter lines are kept as blank lines, so line
        /// numbers still match the file.
        Body: string
    }

let private frontmatter (source: string) (yaml: string) =
    let data = parseYaml yaml
    let fail message = failwith $"{source}: {message}"

    let optional key : 'T option =
        let value = data?(key)
        if isNullOrUndefined value then None else Some(unbox value)

    let required key : 'T =
        optional key
        |> Option.defaultWith (fun () -> fail $"frontmatter needs '{key}'.")

    let text key : string option =
        optional key
        |> Option.map (fun (value: obj) ->
            match value with
            | :? string as s -> s
            | _ -> fail $"'{key}' should be text."
        )

    {
        Title = text "title" |> Option.defaultWith (fun () -> fail "frontmatter needs 'title'.")
        Tagline = text "tagline"
        Description =
            text "description"
            |> Option.defaultWith (fun () -> fail "frontmatter needs 'description'.")
        Section =
            match text "section" with
            | None -> fail "frontmatter needs 'section'."
            | Some section when sections |> List.exists (fun s -> s.Id = section) -> section
            | Some section ->
                let known = sections |> List.map _.Id |> String.concat ", "
                fail $"unknown section '{section}' (known sections: {known})."
        Order = required "order"
        Summary = text "summary"
        Lead = text "lead"
        Links =
            optional "links"
            |> Option.map (fun (links: obj[]) ->
                links
                |> Array.map (fun link ->
                    {
                        Text = string link?text
                        Href = string link?href
                    }
                )
                |> List.ofArray
            )
            |> Option.defaultValue []
        Toc = optional "toc" |> Option.defaultValue false
    }

/// `content/packages/firelight.md` is served at `packages/firelight/`; an `index.md` at its folder.
let private route (source: string) =
    let path =
        source.Substring("content/".Length, source.Length - "content/".Length - ".md".Length)

    if path = "index" then
        ""
    elif path.EndsWith "/index" then
        path.Substring(0, path.Length - "index".Length)
    else
        path + "/"

/// Splits a page into frontmatter and body. `source` is only used in error messages.
let parse (source: string) (text: string) =
    let text = text.Replace("\r\n", "\n")

    if not (text.StartsWith "---\n") then
        failwith $"{source}: a page starts with frontmatter between '---' lines."

    let close = text.IndexOf("\n---\n", 3)

    if close < 0 then
        failwith $"{source}: the frontmatter has no closing '---'."

    let yaml = text.Substring(4, max 0 (close - 4))
    let frontLines = text.Substring(0, close + 5).Split('\n').Length - 1

    {
        Source = source
        Route = route source
        Output = route source + "index.html"
        Meta = frontmatter source yaml
        Body = String.replicate frontLines "\n" + text.Substring(close + 5)
    }

/// Every `.md` file under `content/`, as paths relative to the site root.
let sources (root: string) =
    readdirSync (resolvePath (root, "content"), {| recursive = true |})
    |> Array.map (fun file -> "content/" + file.Replace('\\', '/'))
    |> Array.filter _.EndsWith(".md")
    |> Array.sort

/// Reads and parses every page under `content/`.
let load (root: string) =
    let pages =
        sources root
        |> Array.map (fun source -> parse source (readFileSync (resolvePath (root, source), "utf8")))

    // `foo.md` and `foo/index.md` would both become foo/index.html.
    for output, clashing in pages |> Array.groupBy _.Output do
        if clashing.Length > 1 then
            let sources = clashing |> Array.map _.Source |> String.concat " and "
            failwith $"{sources} would both become {output}."

    pages

/// The pages in a section, in navigation order: by `order`, then by route so that pages written
/// in parallel with the same `order` still sort the same way every time.
let inSection (section: string) (pages: Page seq) =
    pages
    |> Seq.filter (fun p -> p.Meta.Section = section)
    |> Seq.sortBy (fun p -> p.Meta.Order, p.Route)
    |> List.ofSeq

/// The pages before and after `page` in its section, for previous/next links.
let neighbours (pages: Page seq) (page: Page) =
    let siblings = inSection page.Meta.Section pages |> Array.ofList

    match siblings |> Array.tryFindIndex (fun p -> p.Source = page.Source) with
    | Some i -> Array.tryItem (i - 1) siblings, Array.tryItem (i + 1) siblings
    | None -> None, None
