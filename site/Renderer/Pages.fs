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

/// The packages' version, `PackageVersion` in the repository's Directory.Build.props (the site
/// root's parent): the version the site documents.
let packageVersion (root: string) =
    let props = resolvePath (root, "../Directory.Build.props")

    let m =
        Text.RegularExpressions.Regex.Match(readFileSync (props, "utf8"), "<PackageVersion>([^<]+)</PackageVersion>")

    if m.Success then
        m.Groups[1].Value.Trim()
    else
        failwith $"{props} has no <PackageVersion>."

type Link = { Text: string; Href: string }

/// A part of the site that pages declare in their frontmatter (`section: packages`).
type Section =
    {
        Id: string
        /// Its name in the header.
        Name: string
        /// Where the header (and each page's eyebrow link) points. Root-relative; the base path is
        /// added when rendering.
        Href: string
        /// The heading of a list of all the section's pages at the foot of each of them, if any.
        PageList: string option
    }

/// The site's sections, in header order. A page's `section` must be one of these, so adding a
/// section is one line here.
let sections =
    [
        {
            Id = "start"
            Name = "Get started"
            Href = "/start/"
            PageList = None
        }
        {
            Id = "packages"
            Name = "Packages"
            Href = "/#packages"
            PageList = Some "All packages"
        }
        {
            Id = "guides"
            Name = "Guides"
            Href = "/guides/"
            PageList = None
        }
        {
            Id = "demos"
            Name = "Demos"
            Href = "/#demos"
            PageList = None
        }
    ]

/// The YAML block at the top of a page, between `---` lines.
type Frontmatter =
    {
        /// The page heading and its name in navigation.
        Title: string
        /// Completes the `<title>`: "Firelight: Lit web components in F#".
        Tagline: string option
        /// The whole `<title>`, when "title: tagline" doesn't read well. Without either, the
        /// `<title>` is "Title · Firelight".
        PageTitle: string option
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
        /// Links shown under the lead (Lit docs, NuGet, source). With `nuget`, the NuGet link at
        /// the site's version is added, before a "Source" link if there is one.
        Links: Link list
        /// A package page's NuGet package id, such as `Firelight.Router`. Package pages need it.
        NuGet: string option
        /// The small link above the heading. Defaults to the page's section.
        Eyebrow: Link option
        /// Show an h2/h3 table of contents.
        Toc: bool
        /// The page is a single-page app: addresses under its route that have no page of their own
        /// show it too (through 404.html, as GitHub Pages serves that for unknown addresses).
        Spa: bool
        /// The homepage (`layout: home`): the heading, lead and links become the hero, and the
        /// page is in no section.
        Home: bool
        /// A page that is only linked to, such as the search page: in no section, the sitemap,
        /// search or the Markdown for agents, and marked noindex.
        Unlisted: bool
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

/// The page GitHub Pages serves for unknown addresses. It is outside every section and the sitemap.
let notFoundSource = "content/404.md"

let private frontmatter (version: string) (source: string) (yaml: string) =
    let data = parseYaml yaml
    let fail message = failwith $"{source}: {message}"

    let optional key : 'T option =
        let value = data?(key)
        if isNullOrUndefined value then None else Some(unbox value)

    let required key : 'T =
        optional key
        |> Option.defaultWith (fun () -> fail $"frontmatter needs '{key}'.")

    let link (key: string) (value: obj) =
        match (value?text: obj), (value?href: obj) with
        | (:? string as text), (:? string as href) -> { Text = text; Href = href }
        | _ -> fail $"each of '{key}' needs 'text' and 'href'."

    let text key : string option =
        optional key
        |> Option.map (fun (value: obj) ->
            match value with
            | :? string as s -> s
            | _ -> fail $"'{key}' should be text."
        )

    let home =
        match text "layout" with
        | None -> false
        | Some "home" -> true
        | Some layout -> fail $"unknown layout '{layout}' (the only one is 'home')."

    let unlisted = optional "unlisted" |> Option.defaultValue false

    // The not-found page, the homepage and unlisted pages are in no section.
    let sectionless = source = notFoundSource || home || unlisted

    let nuget = text "nuget"

    let links =
        let links =
            optional "links"
            |> Option.map (fun (links: obj[]) -> links |> Array.map (link "links") |> List.ofArray)
            |> Option.defaultValue []

        match nuget with
        | None -> links
        | Some id ->
            let nugetLink =
                {
                    Text = $"NuGet {version}"
                    Href = $"https://www.nuget.org/packages/{id}/{version}"
                }

            let beforeSource = links |> List.takeWhile (fun l -> l.Text <> "Source")
            beforeSource @ nugetLink :: links.[beforeSource.Length ..]

    {
        Title = text "title" |> Option.defaultWith (fun () -> fail "frontmatter needs 'title'.")
        Tagline = text "tagline"
        PageTitle = text "pageTitle"
        Description =
            text "description"
            |> Option.defaultWith (fun () -> fail "frontmatter needs 'description'.")
        Section =
            match text "section" with
            | None when sectionless -> ""
            | None -> fail "frontmatter needs 'section'."
            | Some _ when sectionless -> fail "this page is in no section; remove 'section'."
            | Some section when sections |> List.exists (fun s -> s.Id = section) -> section
            | Some section ->
                let known = sections |> List.map _.Id |> String.concat ", "
                fail $"unknown section '{section}' (known sections: {known})."
        Order =
            if sectionless then
                optional "order" |> Option.defaultValue 0
            else
                required "order"
        Summary = text "summary"
        Lead = text "lead"
        Links = links
        NuGet = nuget
        Eyebrow = optional "eyebrow" |> Option.map (link "eyebrow")
        Toc = optional "toc" |> Option.defaultValue false
        Spa = optional "spa" |> Option.defaultValue false
        Home = home
        Unlisted = unlisted
    }

/// `content/packages/firelight.md` is served at `packages/firelight/`; an `index.md` at its folder;
/// `content/404.md` is `404.html`.
let private route (source: string) =
    let path =
        source.Substring("content/".Length, source.Length - "content/".Length - ".md".Length)

    if source = notFoundSource then
        "404.html"
    elif path = "index" then
        ""
    elif path.EndsWith "/index" then
        path.Substring(0, path.Length - "index".Length)
    else
        path + "/"

/// Splits a page into frontmatter and body. `source` is only used in error messages; `version` is
/// the packages' version, for NuGet links.
let parse (version: string) (source: string) (text: string) =
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
        Output =
            if source = notFoundSource then
                route source
            else
                route source + "index.html"
        Meta = frontmatter version source yaml
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
    let version = packageVersion root

    let pages =
        sources root
        |> Array.map (fun source -> parse version source (readFileSync (resolvePath (root, source), "utf8")))

    for page in pages do
        if page.Meta.Section = "packages" && page.Meta.NuGet.IsNone then
            failwith $"{page.Source}: package pages need 'nuget', the package's id on NuGet."

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

/// The pages before and after `page` in its section, for previous/next links. None for pages in
/// no section.
let neighbours (pages: Page seq) (page: Page) =
    let siblings = inSection page.Meta.Section pages |> Array.ofList

    match siblings |> Array.tryFindIndex (fun p -> p.Source = page.Source) with
    | Some i when page.Meta.Section <> "" -> Array.tryItem (i - 1) siblings, Array.tryItem (i + 1) siblings
    | _ -> None, None
