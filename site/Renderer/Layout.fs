/// The page around the Markdown: head, header, intro, table of contents and footer, as
/// server-only Firelight templates.
module Site.Renderer.Layout

open Firelight
open Site.Renderer.Markdown
open Site.Renderer.Pages

/// A single-page app carried by 404.html: addresses under `Route` show it instead of "page not
/// found". Its `<main>` is spliced in after rendering, at `<!--firelight-spa:n-->`.
type Fallback =
    {
        /// The app's URL with the base path: `/Firelight/client-side-routing/`.
        Route: string
        Title: string
        Demos: Demo list
    }

let fallbackPlaceholder =
    System.Text.RegularExpressions.Regex "<!--firelight-spa:(\\d+)-->"

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
        /// The Markdown before the first h2, rendered. It goes in the intro section.
        Intro: string
        /// Every page on the site, for navigation.
        Site: Page list
        /// On 404.html, the single-page apps it stands in for.
        Fallbacks: Fallback list
    }

let private optional (value: TemplateResult option) : obj =
    match value with
    | Some template -> box template
    | None -> box Lit.nothing

/// The logo files in public/, as root-relative URLs. images/render.mjs makes them from the logos in
/// images/. `SocialImage` (Open Graph and Twitter cards) is 1200×630.
let brand =
    {|
        Favicon = "/favicon.svg"
        FaviconPng = "/favicon-32.png"
        AppleTouchIcon = "/apple-touch-icon.png"
        SocialImage = "/og-default.png"
        SocialImageAlt = "Firelight: Web Components for F#"
    |}

/// What a page tells search engines and link previews.
type Head =
    {
        Title: string
        Description: string
        /// The page's URL relative to the base path (`packages/firelight/`), for the canonical
        /// link. `None` for pages served at many addresses, such as 404.html.
        Route: string option
    }

/// Head tags besides `<title>` and the description: canonical URL, favicon, Open Graph and Twitter
/// cards. Hand-written pages include them with `<!-- firelight:head -->`.
let headMeta (``base``: string) (h: Head) =
    let absolute (path: string) = origin + withBase ``base`` path
    let image = absolute brand.SocialImage

    let canonical =
        match h.Route with
        | Some route ->
            let url = absolute ("/" + route)

            LitSsr.html
                $"""<link rel="canonical" href={url}>
  <meta property="og:url" content={url}>"""
        | None -> LitSsr.html $"""<meta name="robots" content="noindex">"""

    LitSsr.html
        $"""{canonical}
  <link rel="icon" href={brand.FaviconPng} type="image/png" sizes="32x32">
  <link rel="icon" href={brand.Favicon} type="image/svg+xml">
  <link rel="apple-touch-icon" href={brand.AppleTouchIcon}>
  <meta property="og:type" content="website">
  <meta property="og:site_name" content="Firelight">
  <meta property="og:title" content={h.Title}>
  <meta property="og:description" content={h.Description}>
  <meta property="og:image" content={image}>
  <meta property="og:image:width" content="1200">
  <meta property="og:image:height" content="630">
  <meta property="og:image:alt" content={brand.SocialImageAlt}>
  <meta name="twitter:card" content="summary_large_image">
  <meta name="twitter:title" content={h.Title}>
  <meta name="twitter:description" content={h.Description}>
  <meta name="twitter:image" content={image}>
  <meta name="twitter:image:alt" content={brand.SocialImageAlt}>"""

/// The site header, on every page (hand-written pages include it with `<!-- firelight:header -->`).
/// Its section links come from `Pages.sections`.
let header (``base``: string) =
    let sectionLinks =
        Pages.sections
        |> List.map (fun s ->
            LitSsr.html
                $"""
      <a href={withBase ``base`` s.Href}>{s.Name}</a>"""
        )

    LitSsr.html
        $"""<header class="site-header">
    <a class="brand" href={``base``}>Firelight</a>
    <nav>
      <a href="{``base``}#examples">Examples</a>{sectionLinks}
      <a href="https://github.com/roboz0r/Firelight">GitHub</a>
    </nav>
  </header>"""

/// The site footer, on every page (`<!-- firelight:footer -->` in hand-written pages).
let footer =
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

// The page's section; none for 404.html.
let private sectionOf (page: Page) =
    sections |> List.tryFind (fun s -> s.Id = page.Meta.Section)

/// The page's `<title>`.
let title (meta: Frontmatter) =
    match meta.PageTitle, meta.Tagline with
    | Some pageTitle, _ -> pageTitle
    | None, Some tagline -> $"{meta.Title}: {tagline}"
    | None, None -> $"{meta.Title} · Firelight"

// The heading, with the eyebrow link above it, the lead and links below it, and any Markdown before
// the first h2.
let private intro (m: Model) =
    let meta = m.Page.Meta
    let section = sectionOf m.Page

    let eyebrow =
        meta.Eyebrow
        |> Option.orElse (section |> Option.map (fun s -> { Text = s.Name; Href = s.Href }))
        |> Option.map (fun link ->
            LitSsr.html $"""<p class="eyebrow"><a href={withBase m.Base link.Href}>{link.Text}</a></p>"""
        )

    let lead =
        m.Lead
        |> Option.map (fun lead -> LitSsr.html $"""<p class="lead">{LitSsr.markup lead}</p>""")

    let introClass =
        if meta.Section = "packages" then
            "package-intro"
        else
            "page-intro"

    LitSsr.html
        $"""<section class={introClass}>
      {optional eyebrow}
      <h1>{meta.Title}</h1>
      {optional lead}
      {optional (links m.Base meta.Links)}
      {LitSsr.markup m.Intro}
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

/// The homepage's package table (`<!-- firelight:package-table -->`): each page in the packages
/// section with its `summary`, rendered as inline Markdown by `renderSummary`.
let packageTable (``base``: string) (renderSummary: string -> string) (site: Page seq) =
    let rows =
        inSection "packages" site
        |> List.map (fun p ->
            let summary =
                p.Meta.Summary
                |> Option.defaultWith (fun () ->
                    failwith $"{p.Source}: package pages need a 'summary' for the homepage table."
                )

            LitSsr.html
                $"""
          <tr><td><a href="{``base``}{p.Route}"><code>{p.Meta.Title}</code></a></td><td>{LitSsr.markup (renderSummary summary)}</td></tr>"""
        )

    LitSsr.html
        $"""<table>
        <thead><tr><th>Package</th><th>What it does</th></tr></thead>
        <tbody>{rows}
        </tbody>
      </table>"""

// Previous and next pages in the same section.
let private pager (m: Model) =
    match neighbours m.Site m.Page with
    | None, None -> None
    | previous, next ->
        let link rel label (page: Page option) =
            page
            |> Option.map (fun p ->
                LitSsr.html
                    $"""<a class={rel} rel={rel} href="{m.Base}{p.Route}"><span>{label}</span> {p.Meta.Title}</a>"""
            )
            |> optional

        Some(
            LitSsr.html
                $"""<nav class="pager" aria-label="Previous and next pages">
      {link "prev" "Previous" previous}
      {link "next" "Next" next}
    </nav>"""
        )

// The script that swaps an app's page in for "page not found" (see `fallbacks`). Classic, not a
// module, so it runs as soon as it is parsed.
let private swapScript =
    """<script>
(() => {
  const app = [...document.querySelectorAll("template[data-spa]")].find((t) => location.pathname.startsWith(t.dataset.spa));
  if (!app) return;
  document.documentElement.dataset.spa = app.dataset.spa;
  document.title = app.dataset.title;
  // Moved, not cloned: cloning would drop any prerendered (declarative) shadow roots inside.
  document.querySelector("main").replaceWith(app.content);
})();
</script>"""

// 404.html stands in for single-page apps (`spa: true`) on GitHub Pages, which serves it for any
// unknown address. Each app's <main> waits in a <template>. If the address is under the app's
// route, `swapScript` swaps it in before the page is first painted, and a module script then loads
// the app's demos. Other addresses keep "page not found" and load nothing more.
let private fallbacks (fallbacks: Fallback list) =
    match fallbacks with
    | [] -> None
    | fallbacks ->
        let templates =
            fallbacks
            |> List.mapi (fun i app ->
                LitSsr.html
                    $"""
  <template data-spa={app.Route} data-title={app.Title}>{LitSsr.markup $"<!--firelight-spa:{i}-->"}</template>"""
            )

        let imports =
            fallbacks
            |> List.map (fun app ->
                let modules =
                    app.Demos
                    |> List.map _.Module
                    |> List.distinct
                    |> List.map (fun src -> $"import({Fable.Core.JS.JSON.stringify src});")
                    |> String.concat " "

                // As in `scripts`: hydration support first, if anything was prerendered.
                let load =
                    if app.Demos |> List.exists _.Prerender then
                        "import(\"@lit-labs/ssr-client/lit-element-hydrate-support.js\").then(() => { "
                        + modules
                        + " });"
                    else
                        modules

                "if (app === " + Fable.Core.JS.JSON.stringify app.Route + ") { " + load + " }"
            )
            |> String.concat "\nelse "

        let loader =
            "<script type=\"module\">\nconst app = document.documentElement.dataset.spa;\n"
            + imports
            + "\n</script>"

        Some(
            LitSsr.html
                $"""{templates}
  {LitSsr.markup swapScript}
  {LitSsr.markup loader}
"""
        )

let page (m: Model) =
    let meta = m.Page.Meta
    let title = title meta

    let headTags =
        headMeta
            m.Base
            {
                Title = title
                Description = meta.Description
                Route =
                    if m.Page.Source = notFoundSource then
                        None
                    else
                        Some m.Page.Route
            }

    let toc =
        if meta.Toc && not m.Headings.IsEmpty then
            Some(toc m.Headings)
        else
            None

    // Every page in the section ("All packages"), for sections that list them.
    let pageList =
        sectionOf m.Page
        |> Option.bind (fun section -> section.PageList |> Option.map (fun heading -> section, heading))
        |> Option.map (fun (section, heading) ->
            let items =
                inSection meta.Section m.Site
                |> List.map (fun p ->
                    LitSsr.html
                        $"""
        <li><a href="{m.Base}{p.Route}">{p.Meta.Title}</a></li>"""
                )

            LitSsr.html
                $"""<nav class="package-nav" aria-label={section.Name}>
      <h2>{heading}</h2>
      <ul>{items}
      </ul>
    </nav>"""
        )

    LitSsr.html
        $"""<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>{title}</title>
  <meta name="description" content={meta.Description}>
  {headTags}
  <link rel="stylesheet" href="/site.css">
  {scripts m.Demos}
</head>
<body>
  {header m.Base}
  <main>
    {intro m}
    {optional toc}
    {LitSsr.markup m.Content}
    {optional (pager m)}
    {optional pageList}
  </main>{optional (fallbacks m.Fallbacks)}
  {footer}
</body>
</html>
"""
