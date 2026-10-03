/// The page around the Markdown: head, header, intro, table of contents and footer, as
/// server-only Firelight templates.
module Site.Renderer.Layout

open Firelight
open Site.Renderer.Markdown
open Site.Renderer.Pages

type Model =
    {
        Page: Page
        /// Vite's base path, such as `/Firelight/`.
        Base: string
        /// The rendered Markdown body. Trusted: it is the site's own content.
        Content: string
        Headings: Heading list
        Demos: Demo list
        /// The frontmatter lead, rendered as inline Markdown.
        Lead: string option
    }

let private optional (value: TemplateResult option) : obj =
    match value with
    | Some template -> box template
    | None -> box Lit.nothing

// Kept in step with partials/header.html and partials/footer.html until every page uses this layout.
let private header (``base``: string) =
    LitSsr.html
        $"""<header class="site-header">
    <a class="brand" href={``base``}>Firelight</a>
    <nav>
      <a href="{``base``}#examples">Examples</a>
      <a href="{``base``}#packages">Packages</a>
      <a href="{``base``}#demos">Demos</a>
      <a href="https://github.com/roboz0r/Firelight">GitHub</a>
    </nav>
  </header>"""

let private footer =
    LitSsr.html
        $"""<footer class="site-footer">
    <p>Firelight is MIT licensed. <a href="https://github.com/roboz0r/Firelight">Source on GitHub</a>.</p>
  </footer>"""

// The demos' modules. With prerendered demos, Lit SSR's hydration support must be installed before
// LitElement is defined, or components render a second copy into their prerendered shadow roots
// (silently). Vite bundles all of a page's module scripts into one entry, and its shared chunks
// put LitElement next to lit-html, which hydration support imports, so `<script src>` tags after
// it are not enough: the demos are imported dynamically, after hydration support has run. For the
// same reason, page scripts on such pages must not import Lit statically.
let private scripts (demos: Demo list) =
    let modules = demos |> List.map _.Module |> List.distinct

    if demos |> List.exists _.Prerender then
        let imports =
            modules |> List.map (fun src -> $"import(\"{src}\");") |> String.concat "\n"

        LitSsr.markup
            $"<script type=\"module\">\nimport \"@lit-labs/ssr-client/lit-element-hydrate-support.js\";\n{imports}\n</script>"
        |> box
    else
        modules
        |> List.map (fun src -> LitSsr.html $"""<script type="module" src={src}></script>""")
        |> box

let private links (``base``: string) (links: Link list) =
    match links with
    | [] -> None
    | links ->
        let items =
            links
            |> List.map (fun link -> LitSsr.html $"""<li><a href={withBase ``base`` link.Href}>{link.Text}</a></li>""")

        Some(LitSsr.html $"""<ul class="package-links">{items}</ul>""")

let private intro (m: Model) =
    let lead =
        m.Lead
        |> Option.map (fun lead -> LitSsr.html $"""<p class="lead">{LitSsr.markup lead}</p>""")

    match m.Page.Meta.Section with
    | "packages" ->
        LitSsr.html
            $"""<section class="package-intro">
      <p class="eyebrow"><a href="{m.Base}#packages">Packages</a></p>
      <h1>{m.Page.Meta.Title}</h1>
      {optional lead}
      {optional (links m.Base m.Page.Meta.Links)}
    </section>"""
    | _ ->
        LitSsr.html
            $"""<section class="page-intro">
      <h1>{m.Page.Meta.Title}</h1>
      {optional lead}
    </section>"""

let private toc (headings: Heading list) =
    let items =
        headings
        |> List.map (fun h ->
            LitSsr.html $"""<li class="toc-h{h.Level}"><a href="#{h.Id}">{LitSsr.markup h.Html}</a></li>"""
        )

    LitSsr.html
        $"""<nav class="toc" aria-label="On this page">
      <p>On this page</p>
      <ul>{items}</ul>
    </nav>"""

let page (m: Model) =
    let meta = m.Page.Meta

    let title =
        match meta.Tagline with
        | Some tagline -> $"{meta.Title}: {tagline}"
        | None -> $"{meta.Title} · Firelight"

    let toc =
        if meta.Toc && not m.Headings.IsEmpty then
            Some(toc m.Headings)
        else
            None

    // The package list is still a partial (filled in by the includes plugin in vite.config.js);
    // Phase 1 step 4 generates it from frontmatter.
    let packageNav =
        if meta.Section = "packages" then
            Some(LitSsr.html $"""<!-- include partials/package-nav.html -->""")
        else
            None

    LitSsr.html
        $"""<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>{title}</title>
  <meta name="description" content={meta.Description}>
  <link rel="stylesheet" href="/site.css">
  {scripts m.Demos}
</head>
<body>
  {header m.Base}
  <main>
    {intro m}
    {optional toc}
    {LitSsr.markup m.Content}
    {optional packageNav}
  </main>
  {footer}
</body>
</html>
"""
