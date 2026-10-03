/// The pages of the built site, found from site/dist rather than a hard-coded list, so pages
/// added or renamed by the build are checked without changing these tests.
module Site.E2E.Site

open System
open System.IO
open System.Xml.Linq

type Page =
    {
        /// The URL path, under the base: /Firelight/packages/firelight/
        Path: string
        /// The file in dist/: packages/firelight/index.html
        File: string
    }

let private relative (file: string) =
    Path.GetRelativePath(Server.distDir, file).Replace('\\', '/')

let private pageOfFile (file: string) =
    let path =
        if file = "index.html" then
            ""
        elif file.EndsWith "/index.html" then
            file.Substring(0, file.Length - "index.html".Length)
        else
            file

    {
        Path = Server.basePath + path
        File = file
    }

let private fileOfPath (path: string) =
    let rest = path.Substring(Server.basePath.Length)

    if rest = "" || rest.EndsWith "/" then rest + "index.html"
    elif Path.HasExtension rest then rest
    else rest + "/index.html"

/// Every HTML file in dist/.
let htmlPages =
    Directory.EnumerateFiles(Server.distDir, "*.html", SearchOption.AllDirectories)
    |> Seq.map (relative >> pageOfFile)
    |> Seq.sortBy _.Path
    |> Seq.toList

let sitemapFile = Path.Combine(Server.distDir, "sitemap.xml")

/// The pages listed in dist/sitemap.xml, if the build writes one.
let sitemapPages =
    if File.Exists sitemapFile then
        XDocument.Load(sitemapFile).Descendants()
        |> Seq.filter (fun e -> e.Name.LocalName = "loc")
        |> Seq.map (fun loc ->
            let path = Uri(loc.Value.Trim()).AbsolutePath

            if not (path.StartsWith Server.basePath) then
                failwith $"sitemap.xml lists {loc.Value}, which is outside {Server.basePath}."

            { Path = path; File = fileOfPath path }
        )
        |> Seq.toList
        |> Some
    else
        None

/// Deployed pages that a sitemap leaves out: the not-found page and the demo apps.
let private isUnlisted (page: Page) =
    page.File = "404.html" || page.File.StartsWith "demos/"

/// The pages to check: the sitemap's, when there is one, otherwise every HTML file; plus the
/// not-found page and the demo apps either way.
let pages =
    match sitemapPages with
    | Some listed -> listed @ (htmlPages |> List.filter isUnlisted)
    | None -> htmlPages
    |> List.distinctBy _.Path

let source =
    match sitemapPages with
    | Some _ -> "sitemap.xml"
    | None -> "the HTML files in dist/"

/// HTML pages in dist/ that the sitemap should list but doesn't.
let missingFromSitemap () =
    match sitemapPages with
    | Some listed ->
        let files = listed |> List.map _.File |> set

        htmlPages
        |> List.filter (fun p -> not (isUnlisted p) && not (files.Contains p.File))
    | None -> []

let isDemoApp (page: Page) = page.File.StartsWith "demos/"
