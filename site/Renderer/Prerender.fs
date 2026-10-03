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
        |}
    )

/// Renders `content/...md` to a complete HTML document. Vite then processes it like any other
/// HTML page (module scripts, base path, the includes plugin).
let renderPage (host: Host) (source: string) : JS.Promise<string> =
    async {
        try
            let site = Pages.load host.root |> List.ofArray

            let page =
                site
                |> List.tryFind (fun p -> p.Source = source)
                |> Option.defaultWith (fun () -> failwith "no such page.")

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
                        Site = site
                    }

            let! html = LitSsr.renderToString layout |> Async.AwaitPromise
            return Markdown.restoreVerbatim body html
        with e ->
            return failwith $"{source}: {e.Message}"
    }
    |> Async.StartAsPromise

// `<!-- firelight:name -->` in a hand-written page.
let private placeholder =
    System.Text.RegularExpressions.Regex "<!-- firelight:([a-z-]+) -->"

[<Emit("String.fromCodePoint($0)")>]
let private fromCodePoint (codePoint: int) : string = jsNative

// Decodes the character references an attribute or <title> may hold, so they aren't escaped twice
// when the text is repeated in the social tags: numeric ones and the five XML names. Other named
// references are an error rather than a silently wrong preview; write the character itself.
let private unescapeHtml (source: string) (text: string) =
    System.Text.RegularExpressions.Regex.Replace(
        text,
        "&(#[0-9]+|#[xX][0-9a-fA-F]+|[a-zA-Z][a-zA-Z0-9]*);",
        fun (m: System.Text.RegularExpressions.Match) ->
            match m.Groups[1].Value with
            | "lt" -> "<"
            | "gt" -> ">"
            | "quot" -> "\""
            | "apos" -> "'"
            | "amp" -> "&"
            | ref when ref.StartsWith "#x" || ref.StartsWith "#X" ->
                fromCodePoint (System.Convert.ToInt32(ref.Substring 2, 16))
            | ref when ref.StartsWith "#" -> fromCodePoint (int (ref.Substring 1))
            | ref -> failwith $"{source}: write '&{ref};' in the <title> or description as the character itself."
    )

// The page's own <title> and description, which the head placeholder repeats for link previews.
let private headOf (source: string) (html: string) : Layout.Head =
    let find (pattern: string) what =
        let m = System.Text.RegularExpressions.Regex.Match(html, pattern)

        if m.Success then
            unescapeHtml source (m.Groups[1].Value.Trim())
        else
            failwith $"{source}: <!-- firelight:head --> needs the page's {what} before it."

    {
        Title = find "<title>([^<]*)</title>" "<title>"
        Description = find "<meta name=\"description\" content=\"([^\"]*)\">" "<meta name=\"description\">"
        // index.html -> "", demos/index.html -> "demos/"
        Route = Some(source.Substring(0, source.Length - "index.html".Length))
    }

/// Fills in the `<!-- firelight:name -->` placeholders in a hand-written HTML page (`head`,
/// `header`, `footer`), so it shares the generated parts of the layout. `source` is the page's path
/// relative to the site root, such as `index.html`.
let renderIncludes (host: Host) (source: string) (html: string) : JS.Promise<string> =
    async {
        let fragment name =
            match name with
            | "head" when source.EndsWith "index.html" -> Layout.headMeta host.``base`` (headOf source html)
            | "header" -> Layout.header host.``base``
            | "footer" -> Layout.footer
            | _ -> failwith $"{source}: unknown placeholder '<!-- firelight:{name} -->'."

        let names =
            placeholder.Matches html
            |> Seq.cast<System.Text.RegularExpressions.Match>
            |> Seq.map (fun m -> m.Groups[1].Value)
            |> Seq.distinct
            |> List.ofSeq

        let rendered = System.Collections.Generic.Dictionary<string, string>()

        for name in names do
            let! text = LitSsr.renderToString (fragment name) |> Async.AwaitPromise
            rendered[name] <- text

        return placeholder.Replace(html, (fun m -> rendered[m.Groups[1].Value]))
    }
    |> Async.StartAsPromise

let private escapeXml (text: string) =
    text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")

/// `sitemap.xml`: every Markdown page, plus `otherRoutes` (hand-written pages and demo apps,
/// relative to the base path, such as `demos/todo/`), as absolute URLs.
let sitemap (host: Host) (otherRoutes: string[]) =
    let urls =
        Array.append otherRoutes (Pages.load host.root |> Array.map _.Route)
        |> Array.distinct
        |> Array.sort
        |> Array.map (fun route -> $"  <url><loc>{escapeXml (Pages.origin + host.``base`` + route)}</loc></url>\n")
        |> String.concat ""

    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n"
    + "<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n"
    + urls
    + "</urlset>\n"
