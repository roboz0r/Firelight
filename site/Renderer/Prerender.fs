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
    let pages =
        Pages.sources root
        |> Array.map (fun source -> Pages.parse source (readFileSync (resolvePath (root, source), "utf8")))

    // `foo.md` and `foo/index.md` would both become foo/index.html.
    for output, clashing in pages |> Array.groupBy _.Output do
        if clashing.Length > 1 then
            let sources = clashing |> Array.map _.Source |> String.concat " and "
            failwith $"{sources} would both become {output}."

    pages
    |> Array.map (fun page ->
        {|
            source = page.Source
            output = page.Output
        |}
    )

/// Renders `content/...md` to a complete HTML document. Vite then processes it like any other
/// HTML page (module scripts, base path, the includes plugin).
let renderPage (host: Host) (source: string) : JS.Promise<string> =
    async {
        try
            let page =
                Pages.parse source (readFileSync (resolvePath (host.root, source), "utf8"))

            let! highlighter = highlighter.Value |> Async.AwaitPromise

            let md =
                Markdown.create
                    {
                        Root = host.root
                        Base = host.``base``
                        Highlighter = highlighter
                    }

            let body = Markdown.render md page.Body

            // Registering a demo's custom elements is what makes Lit SSR prerender them.
            for demo in body.Demos do
                if demo.Prerender then
                    do! host.loadModule demo.Module |> Async.AwaitPromise |> Async.Ignore

            let layout =
                Layout.page
                    {
                        Page = page
                        Base = host.``base``
                        Content = body.Html
                        Headings = body.Headings
                        Demos = body.Demos
                        Lead = page.Meta.Lead |> Option.map (fun lead -> md.renderInline lead)
                    }

            let! html = LitSsr.renderToString layout |> Async.AwaitPromise
            return Markdown.restoreVerbatim body html
        with e ->
            return failwith $"{source}: {e.Message}"
    }
    |> Async.StartAsPromise
