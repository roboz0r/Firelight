/// The site as Markdown, for agents (site/Renderer/Agents.fs): every page in the sitemap has an
/// `index.md` that its HTML links to, and the links in llms.txt and llms-full.txt resolve.
module Site.E2E.AgentTests

open System
open System.Net.Http
open System.Text.RegularExpressions
open Expecto
open Site.E2E.Site

/// Where the site is published: links in the Markdown are absolute, to here.
let private published = "https://roboz0r.github.io"

let private http = new HttpClient()

let private get (path: string) =
    task {
        use! response = http.GetAsync(Server.url path)
        let! body = response.Content.ReadAsStringAsync()
        return int response.StatusCode, body
    }

let private markdownPages = pages |> List.filter (isUnlisted >> not)

// Lines outside fenced code blocks.
let private prose (markdown: string) =
    let mutable fence: string option = None

    [
        for line in markdown.Replace("\r\n", "\n").Split('\n') do
            let trimmed = line.TrimStart()
            let marker = Regex.Match(trimmed, "^(`{3,}|~{3,})").Value

            match fence with
            | None when marker <> "" -> fence <- Some marker
            | Some opening when marker.StartsWith opening && trimmed.Trim() = marker -> fence <- None
            | None -> yield line
            | Some _ -> ()
    ]

/// The site's own addresses linked to, in Markdown or raw HTML, as paths (and #anchors) on the
/// local server, in order.
let private siteLinks (markdown: string) =
    let site = Regex.Escape(published + Server.basePath)

    Regex.Matches(markdown, $"\\]\\(<?({site}[^)\\s>]*)|href=[\"']({site}[^\"']*)")
    |> Seq.map (fun m ->
        let uri =
            Uri(
                if m.Groups[1].Success then
                    m.Groups[1].Value
                else
                    m.Groups[2].Value
            )

        uri.PathAndQuery + uri.Fragment
    )
    |> Seq.distinct
    |> List.ofSeq

/// Links that don't resolve on the local server: an HTTP error, or an #anchor with no such id.
let private brokenLinks (links: string list) =
    task {
        let broken = Collections.Generic.List<string>()

        for link in links do
            let path, anchor =
                match link.IndexOf '#' with
                | -1 -> link, ""
                | i -> link.Substring(0, i), Uri.UnescapeDataString(link.Substring(i + 1))

            let! (status: int), (body: string) = get path

            if status <> 200 then
                broken.Add $"{link}: HTTP {status}"
            elif
                anchor <> ""
                && not (Regex.IsMatch(body, $"""\sid=["']?{Regex.Escape anchor}["'\s>]"""))
            then
                broken.Add $"{link}: no element with id \"{anchor}\""

        return List.ofSeq broken
    }

// A page's Markdown after its frontmatter and heading, which llms-full.txt repeats.
let private bodyOf (markdown: string) =
    let heading = markdown.IndexOf "\n# "
    let afterHeading = markdown.IndexOf("\n\n", heading + 1)
    markdown.Substring(afterHeading + 2)

let private pageTests (page: Page) =
    testTask $"{page.Path} has a Markdown version, linked from its head" {
        let markdownPath = page.Path + "index.md"
        let! (_: int), (html: string) = get page.Path

        let alternates =
            Regex.Matches(html, "<link rel=\"alternate\" type=\"text/markdown\" href=\"([^\"]*)\"\\s*>")
            |> Seq.map _.Groups[1].Value
            |> List.ofSeq

        if alternates <> [ markdownPath ] then
            failtest
                $"{page.Path}: expected one <link rel=\"alternate\" type=\"text/markdown\" href=\"{markdownPath}\">, found {alternates}."

        let! (status: int), (markdown: string) = get markdownPath

        if status <> 200 then
            failtest $"{markdownPath}: HTTP {status}."

        if not (markdown.StartsWith "---\ntitle: ") then
            failtest $"{markdownPath} doesn't start with frontmatter holding the title."

        let problems =
            [
                for line in prose markdown do
                    if Regex.IsMatch(line, "\\]\\(/|href=\"/") then
                        $"a root-relative link (should be absolute): {line.Trim()}"

                    if line.StartsWith ":::" then
                        $"a container left as it was: {line.Trim()}"

                    if line.Contains "data-demo-size" then
                        $"a demo size left as a placeholder: {line.Trim()}"
            ]

        if not problems.IsEmpty then
            failtest
                $"""{markdownPath}:
  {String.Join("\n  ", problems)}"""
    }

let private indexes =
    testList "llms.txt" [
        testTask "links every page's Markdown version, and each link resolves" {
            let! (status: int), (index: string) = get (Server.basePath + "llms.txt")

            if status <> 200 then
                failtest $"llms.txt: HTTP {status}."

            let links = siteLinks index

            let missing =
                markdownPages
                |> List.filter (fun p -> not (List.contains (p.Path + "index.md") links))

            if not missing.IsEmpty then
                failtest
                    $"""llms.txt doesn't link: {String.Join(", ", missing |> List.map (fun p -> p.Path + "index.md"))}"""

            let! (broken: string list) = brokenLinks links

            if not broken.IsEmpty then
                failtest $"""llms.txt has broken links: {String.Join(", ", broken)}"""
        }

        testTask "llms-full.txt has every page, in llms.txt's order, and its links within the site resolve" {
            let! (_: int), (index: string) = get (Server.basePath + "llms.txt")
            let! (status: int), (full: string) = get (Server.basePath + "llms-full.txt")

            if status <> 200 then
                failtest $"llms-full.txt: HTTP {status}."

            // Each page's own Markdown (after its heading), where llms.txt lists it.
            let mutable last = -1

            for link in siteLinks index |> List.filter _.EndsWith("index.md") do
                let! (_: int), (markdown: string) = get link
                let body = bodyOf markdown

                if body.Trim().Length < 40 then
                    failtest $"{link} has almost nothing after its heading."

                let at = full.IndexOf body

                if at < 0 then
                    failtest $"llms-full.txt doesn't have the content of {link}."

                if at < last then
                    failtest $"llms-full.txt has {link} earlier than the pages before it in llms.txt."

                last <- at

            let! (broken: string list) = brokenLinks (siteLinks full)

            if not broken.IsEmpty then
                failtest $"""llms-full.txt has broken links: {String.Join(", ", broken)}"""
        }
    ]

let all () =
    testList "Markdown for agents" [ indexes; yield! markdownPages |> List.map pageTests ]
