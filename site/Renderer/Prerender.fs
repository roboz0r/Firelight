/// Entry point for the Vite plugin: lists the Markdown pages and renders one to a complete HTML
/// document, with every prerenderable demo rendered to Declarative Shadow DOM by Lit SSR.
module Site.Renderer.Prerender

open Fable.Core
open Site.Renderer

/// What the Vite plugin provides.
type Host =
    /// The site root (the folder with vite.config.js).
    abstract root: string
    /// Vite's base path, such as `/Firelight/`.
    abstract ``base``: string
    /// Imports a module by its root-relative URL, such as `/build/Snippets/Rating.js`. In dev this
    /// goes through Vite, so edited modules are reloaded.
    abstract loadModule: url: string -> JS.Promise<obj>

[<Import("readFileSync", "node:fs")>]
let private readFileSync (path: string, encoding: string) : string = jsNative

[<Import("resolve", "node:path")>]
let private resolvePath (dir: string, file: string) : string = jsNative

// One highlighter for the whole build (or until the renderer is edited, in dev).
let private highlighter = lazy (Markdown.Highlight.create ())

/// Every Markdown page: its source (`content/packages/firelight.md`) and the HTML file it becomes
/// (`packages/firelight/index.html`), both relative to the site root.
let pages (root: string) =
    Pages.load root
    |> Array.map (fun page ->
        {|
            source = page.Source
            output = page.Output
            route = page.Route
            spa = page.Meta.Spa
        |}
    )

let private markdown (host: Host) (site: Pages.Page list) =
    async {
        let! highlighter = highlighter.Value |> Async.AwaitPromise
        // The package table renders summaries with the instance it belongs to.
        let mutable md = Unchecked.defaultof<MarkdownIt.MarkdownIt>

        let packageTable (c: Markdown.Container) =
            if not c.Args.IsEmpty || not (System.String.IsNullOrWhiteSpace c.Body) then
                failwith $"Line {c.Line + 1}: '::: package-table' takes no options and has no body."

            Layout.packageTable host.``base`` (fun s -> md.renderInline s) site

        md <-
            Markdown.create
                {
                    Root = host.root
                    Base = host.``base``
                    Highlighter = highlighter
                    Containers = Map [ "package-table", packageTable ]
                }

        return md
    }

// A page as a complete HTML document, and the demos on it. On 404.html, single-page apps (`spa:
// true`) are rendered too and carried along, so their addresses still work (see Layout.fallbacks).
let rec private renderDocument
    (host: Host)
    (site: Pages.Page list)
    (page: Pages.Page)
    : Async<string * Markdown.Demo list> =
    async {
        let! md = markdown host site
        let body = Markdown.render md page.Body

        // Registering a demo's custom elements is what makes Lit SSR prerender them.
        for demo in body.Demos do
            if demo.Prerender then
                do! host.loadModule demo.Module |> Async.AwaitPromise |> Async.Ignore

        let apps =
            if page.Source = Pages.notFoundSource then
                site |> List.filter _.Meta.Spa
            else
                []

        let mutable fallbacks = []

        for app in apps do
            let! html, demos = renderDocument host site app

            // The app's <main> element, as 404.html's own: not for search (data-pagefind-body).
            let main =
                let start = html.IndexOf "<main"
                let content = html.IndexOf(">", start) + 1
                "<main>" + html.Substring(content, html.LastIndexOf "</main>" + 7 - content)

            fallbacks <-
                fallbacks
                @ [
                    ({
                        Route = host.``base`` + app.Route
                        Title = Layout.title app.Meta
                        Demos = demos
                    }
                    : Layout.Fallback),
                    main
                ]

        let layout =
            Layout.page
                {
                    Page = page
                    Base = host.``base``
                    Content = body.Html
                    Headings = body.Headings
                    Demos = body.Demos
                    Lead = page.Meta.Lead |> Option.map (fun lead -> md.renderInline lead)
                    Intro = body.Intro
                    Site = site
                    Fallbacks = List.map fst fallbacks
                }

        let! html = LitSsr.renderToString layout |> Async.AwaitPromise
        let mains = fallbacks |> List.map snd |> Array.ofList
        // The apps' pages go in after rendering, as they are already rendered.
        let html =
            Layout.fallbackPlaceholder.Replace(
                Markdown.restoreVerbatim body html,
                (fun m -> mains[int m.Groups[1].Value])
            )

        return html, body.Demos
    }

/// Renders `content/...md` to a complete HTML document. Vite then processes it like any other
/// HTML page (module scripts, base path).
let renderPage (host: Host) (source: string) : JS.Promise<string> =
    async {
        try
            let site = Pages.load host.root |> List.ofArray

            let page =
                site
                |> List.tryFind (fun p -> p.Source = source)
                |> Option.defaultWith (fun () -> failwith "no such page.")

            let! html, _ = renderDocument host site page
            return html
        with e ->
            return failwith $"{source}: {e.Message}"
    }
    |> Async.StartAsPromise

/// The site as Markdown for agents (see Agents): each page's `index.md`, `llms.txt` and
/// `llms-full.txt`, as files relative to `dist/` with their text.
let agentFiles (host: Host) : JS.Promise<{| file: string; text: string |}[]> =
    async {
        let site = Pages.load host.root |> List.ofArray
        let! md = markdown host site

        let s: Agents.Settings =
            {
                Root = host.root
                Base = host.``base``
                Site = site
                Md = md
                Here = ""
            }

        // Errors name the page; llms-full.txt has every page, so it fails if one does.
        let document (page: Pages.Page) =
            try
                Agents.document s page
            with e ->
                failwith $"{page.Source}: {e.Message}"

        let pages =
            Agents.pages s
            |> List.map (fun page ->
                {|
                    file = Agents.markdownFile page
                    text = document page
                |}
            )

        return
            Array.ofList (
                pages
                @ [
                    {|
                        file = "llms.txt"
                        text = Agents.index s
                    |}
                    {|
                        file = "llms-full.txt"
                        text = Agents.full s
                    |}
                ]
            )
    }
    |> Async.StartAsPromise

let private escapeXml (text: string) =
    text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")

/// `sitemap.xml`: every Markdown page, plus `otherRoutes` (the demo apps, relative to the base
/// path, such as `demos/todo/`), as absolute URLs.
let sitemap (host: Host) (otherRoutes: string[]) =
    let urls =
        Array.append
            otherRoutes
            (Pages.load host.root
             |> Array.filter (fun p -> p.Source <> Pages.notFoundSource && not p.Meta.Unlisted)
             |> Array.map _.Route)
        |> Array.distinct
        |> Array.sort
        |> Array.map (fun route -> $"  <url><loc>{escapeXml (Pages.origin + host.``base`` + route)}</loc></url>\n")
        |> String.concat ""

    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n"
    + "<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n"
    + urls
    + "</urlset>\n"

/// Checks the internal links on `pages` (see Links.check). The build passes every page with its
/// HTML; dev passes the page being served with its HTML and the rest as link targets only.
let checkLinks (host: Host) (strict: bool) (fileDirs: string[]) (pages: Links.Built[]) =
    Links.check host.root host.``base`` strict fileDirs pages
