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
/// cards.
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

/// The logo beside the header's wordmark: images/logo-small.svg (keep them the same), inline so it
/// paints with the page. Flat colours and no ids, so it can appear on a page that also inlines the
/// full logo, and hidden from assistive technology because the wordmark names the link.
let private headerMark =
    LitSsr.markup
        """<svg class="brand-mark" viewBox="0 0 64 64" aria-hidden="true" focusable="false"><path fill="none" stroke="#c92a2a" stroke-width="9" stroke-linecap="round" stroke-linejoin="round" d="M11 27 5 40 11 53M53 27 59 40 53 53"/><path fill="#f76707" d="M32 60C22.51 60 16 52 16 41.2 16 29.4 22.83 22.8 26.67 14.8 28.59 10.6 28.91 7.3 28.59 4 34.24 7.3 38.51 12.5 40.43 18.6 41.92 16.2 43.09 13.4 43.41 10.6 46.83 15.8 48 23.8 48 34.6 48 49.7 41.92 60 32 60Z"/><path fill="#ffc145" d="M32 60C27.41 60 24.43 56 24.43 50.6 24.43 44.4 28.16 40.4 30.08 34.1 35.2 38.1 39.57 43.6 39.57 50.6 39.57 56 36.59 60 32 60Z"/></svg>"""

/// The site header, on every page.
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
    <a class="brand" href={``base``}>{headerMark}Firelight</a>
    <nav>
      <a href="{``base``}#examples">Examples</a>{sectionLinks}
      <a href="https://github.com/roboz0r/Firelight">GitHub</a>
    </nav>
  </header>"""

/// The site footer, on every page.
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

/// images/logo.svg, inline so it paints with the page. Decorative: the header names the site.
let private heroMark =
    LitSsr.markup
        """<svg class="hero-mark" viewBox="0 0 64 64" aria-hidden="true" focusable="false">
        <defs>
          <linearGradient id="firelight-coal" x1="0" y1="52" x2="0" y2="28" gradientUnits="userSpaceOnUse">
            <stop offset="0" stop-color="#c92a2a"/>
            <stop offset="1" stop-color="#d9480f"/>
          </linearGradient>
          <linearGradient id="firelight-flame" x1="0" y1="1" x2="0" y2="0">
            <stop offset="0" stop-color="#ff8a3d"/>
            <stop offset="1" stop-color="#f76707"/>
          </linearGradient>
        </defs>
        <path fill="none" stroke="url(#firelight-coal)" stroke-width="8" stroke-linecap="round" stroke-linejoin="round" d="M10.5 29 4.8 40 10.5 51M53.5 29 59.2 40 53.5 51"/>
        <path fill="url(#firelight-flame)" d="M32 60C23.1 60 17 52 17 41.2 17 29.4 23.4 22.8 27 14.8 28.8 10.6 29.1 7.3 28.8 4 34.1 7.3 38.1 12.5 39.9 18.6 41.3 16.2 42.4 13.4 42.7 10.6 45.9 15.8 47 23.8 47 34.6 47 49.7 41.3 60 32 60Z"/>
        <path fill="#ffc145" d="M32 60C27.7 60 24.9 56 24.9 50.6 24.9 44.4 28.4 40.4 30.2 34.1 35 38.1 39.1 43.6 39.1 50.6 39.1 56 36.3 60 32 60Z"/>
      </svg>"""

// The homepage's intro (`layout: home`): the logo, heading and lead, with the links as buttons
// (the first one primary).
let private hero (m: Model) =
    let lead =
        m.Lead
        |> Option.map (fun lead -> LitSsr.html $"""<p class="lead">{LitSsr.markup lead}</p>""")

    let actions =
        m.Page.Meta.Links
        |> List.mapi (fun i link ->
            let buttonClass = if i = 0 then "button primary" else "button"
            LitSsr.html $"""<a class={buttonClass} href={withBase m.Base link.Href}>{link.Text}</a>"""
        )

    LitSsr.html
        $"""<section class="hero">
      {heroMark}
      <h1>{m.Page.Meta.Title}</h1>
      {optional lead}
      <p class="actions">{actions}</p>
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

let private escapeHtml (text: string) =
    text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;")

/// The package table (`::: package-table`, on the homepage) as HTML: each page in the packages
/// section with its `summary`, rendered as inline Markdown by `renderSummary`.
let packageTable (``base``: string) (renderSummary: string -> string) (site: Page seq) =
    let rows =
        inSection "packages" site
        |> List.map (fun p ->
            let summary =
                p.Meta.Summary
                |> Option.defaultWith (fun () ->
                    failwith $"{p.Source}: package pages need a 'summary' for the package table."
                )

            $"""    <tr><td><a href="{escapeHtml (``base`` + p.Route)}"><code>{escapeHtml p.Meta.Title}</code></a></td><td>{renderSummary summary}</td></tr>"""
            + "\n"
        )

    "<table>\n  <thead><tr><th>Package</th><th>What it does</th></tr></thead>\n  <tbody>\n"
    + String.concat "" rows
    + "  </tbody>\n</table>\n"

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

    // The page as Markdown (Agents), for agents and anything else that reads text. The href has
    // the base path already, which Vite would add again in dev: vite-ignore keeps it as it is (and
    // Vite removes the attribute).
    let markdownVersion =
        if m.Page.Source = notFoundSource then
            None
        else
            let href = withBase m.Base ("/" + m.Page.Route + "index.md")
            Some(LitSsr.html $"""<link rel="alternate" type="text/markdown" href={href} vite-ignore>""")

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
  {optional markdownVersion}
  <link rel="stylesheet" href="/site.css">
  {scripts m.Demos}
</head>
<body>
  {header m.Base}
  <main>
    {if meta.Home then hero m else intro m}
    {optional toc}
    {LitSsr.markup m.Content}
    {optional (pager m)}
    {optional pageList}
  </main>{optional (fallbacks m.Fallbacks)}
  {footer}
</body>
</html>
"""
