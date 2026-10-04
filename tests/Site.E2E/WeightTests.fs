/// Checks the build's per-page JavaScript report (site/build/page-weights.json, written by
/// site/page-weights.mjs) against the files in dist/ and what the browser actually loads.
module Site.E2E.WeightTests

open System
open System.IO
open System.IO.Compression
open System.Text.Json
open Expecto
open Site.E2E.Site

[<CLIMutable>]
type Inline =
    {
        Scripts: int
        Js: int
        JsGzip: int
        /// Each inline script's id, "" for none.
        Ids: string[]
    }

[<CLIMutable>]
type PageWeight =
    {
        Js: int
        JsGzip: int
        Files: string[]
        Inline: Inline
    }

let reportFile = Path.Combine(Server.siteDir, "build", "page-weights.json")

let private report =
    lazy
        (if not (File.Exists reportFile) then
             failtest $"No page weight report at {reportFile}. Run `npm run build` in site/ first."

         JsonSerializer.Deserialize<Collections.Generic.Dictionary<string, PageWeight>>(
             File.ReadAllText reportFile,
             JsonSerializerOptions(PropertyNameCaseInsensitive = true)
         ))

let private weightOf (page: Page) =
    match report.Value.TryGetValue page.File with
    | true, weight -> weight
    | _ -> failtest $"{page.Path}: {reportFile} has no entry for {page.File}."

let private html (page: Page) =
    File.ReadAllText(Path.Combine(Server.distDir, page.File))

/// A demo is a `.demo` box, as `::: example` and `::: demo` render them.
let private hasDemos (page: Page) = (html page).Contains "class=\"demo"

let private gzipBytes (bytes: byte[]) =
    use output = new MemoryStream()

    do
        use gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen = true)
        gzip.Write(bytes)

    output.Length

let private gzipSize (file: string) = gzipBytes (File.ReadAllBytes file)

/// The header's theme switch (Renderer/Layout.fs): the one script every page carries, inline in
/// <head>. Pages without demos ship it and nothing else.
let themeScriptId = "theme-script"

/// The theme script's budget, gzipped. It is about 500 bytes.
let themeScriptBudget = 1000L

[<CLIMutable>]
type HeadScript =
    {
        Id: string
        Body: string
        /// It comes before every stylesheet and every other script in the document.
        First: bool
        InHead: bool
    }

// Every <script> in the built HTML, in order, with whether it precedes the first stylesheet.
let private scriptsIn (html: string) =
    let withoutComments = Text.RegularExpressions.Regex.Replace(html, "<!--[\\s\\S]*?-->", "")
    let headEnd = withoutComments.IndexOf "</head>"

    let firstStylesheet =
        Text.RegularExpressions.Regex.Match(withoutComments, "<link[^>]*rel=\"?stylesheet")

    [
        for m in
            Text.RegularExpressions.Regex.Matches(
                withoutComments,
                "<script\\b([^>]*)>([\\s\\S]*?)</script\\s*>",
                Text.RegularExpressions.RegexOptions.IgnoreCase
            ) do
            let id = Text.RegularExpressions.Regex.Match(m.Groups[1].Value, "\\bid=\"([^\"]*)\"")

            {
                Id = (if id.Success then id.Groups[1].Value else "")
                Body = m.Groups[2].Value
                First = not firstStylesheet.Success || m.Index < firstStylesheet.Index
                InHead = m.Index < headEnd
            }
    ]

let private themeScriptTest (page: Page) =
    test "the theme script is first in <head>, before the stylesheets, and within its budget" {
        let scripts = scriptsIn (html page)

        match scripts |> List.filter (fun s -> s.Id = themeScriptId) with
        | [ script ] ->
            if not (scripts.Head = script && script.InHead && script.First) then
                failtest
                    $"{page.Path}: the theme script isn't the first script in <head>, before the stylesheets, so a stored theme could flash."

            let size = gzipBytes (Text.Encoding.UTF8.GetBytes script.Body)

            if size > themeScriptBudget then
                failtest
                    $"{page.Path}: the theme script is {size} bytes gzipped, over its {themeScriptBudget}-byte budget."
        | found ->
            failtest $"{page.Path}: has {found.Length} scripts with id=\"{themeScriptId}\"; expected one."
    }

let private pageTests (page: Page) =
    testList page.Path [
        test "the report's totals match the files in dist" {
            let weight = weightOf page
            let files = weight.Files |> Array.map (fun f -> Path.Combine(Server.distDir, f))

            for file in files do
                if not (File.Exists file) then
                    failtest $"{page.Path}: page-weights.json counts {file}, which doesn't exist."

            let raw =
                (files |> Array.sumBy (fun f -> FileInfo(f).Length)) + int64 weight.Inline.Js

            if int64 weight.Js <> raw then
                failtest
                    $"{page.Path}: page-weights.json says {weight.Js} bytes of JavaScript; the files add up to {raw}."

            // .NET's gzip isn't byte-for-byte Node's, so allow a few percent.
            let gzipped = (files |> Array.sumBy gzipSize) + int64 weight.Inline.JsGzip

            if abs (int64 weight.JsGzip - gzipped) > max 64L (gzipped / 20L) then
                failtest
                    $"{page.Path}: page-weights.json says {weight.JsGzip} bytes gzipped; the files gzip to about {gzipped}."
        }

        testTask "the report counts every script the page loads" {
            let weight = weightOf page

            do!
                Browser.withPage
                    true
                    page.Path
                    (fun opened ->
                        task {
                            let loaded =
                                opened.Scripts
                                |> Seq.map (fun path -> path.Substring(Server.basePath.Length))
                                |> Seq.distinct
                                |> Seq.toList

                            let uncounted = loaded |> List.filter (fun f -> not (Array.contains f weight.Files))

                            if not uncounted.IsEmpty then
                                failtest
                                    $"""{page.Path}: loaded scripts that page-weights.json doesn't count for {page.File}: {String.Join(", ", uncounted)}"""

                            // The page's entry scripts always load, so counted files mean some loaded.
                            if loaded.IsEmpty && weight.Files.Length > 0 then
                                failtest
                                    $"""{page.Path}: loaded no script files, but page-weights.json counts {String.Join(", ", weight.Files)}."""

                            let! inlineScripts =
                                opened.Page.EvaluateAsync<int>(
                                    """() => [...document.querySelectorAll("script:not([src])")]
                                        .filter((s) => { const t = (s.type || "").toLowerCase(); return (!t || t === "module" || t.includes("javascript")) && s.text.trim(); })
                                        .length"""
                                )

                            if inlineScripts <> weight.Inline.Scripts then
                                failtest
                                    $"{page.Path}: has {inlineScripts} inline scripts, but page-weights.json counts {weight.Inline.Scripts}."
                        }
                    )
        }

        // Only the theme script, by its id: any other script, inline or a file, fails this.
        test "a page without demos ships no JavaScript but the theme script" {
            if not (hasDemos page) then
                let weight = weightOf page

                let others =
                    [
                        yield! weight.Files

                        for id in weight.Inline.Ids do
                            if id <> themeScriptId then
                                if id = "" then "an inline script" else $"an inline script with id=\"{id}\""
                    ]

                if not others.IsEmpty then
                    failtest
                        $"""{page.Path} has no demos but ships JavaScript besides the theme script ({weight.JsGzip} bytes gzipped in all): {String.Join(", ", others)}."""

                if weight.Inline.Ids <> [| themeScriptId |] then
                    failtest $"{page.Path}: expected the theme script once; page-weights.json lists {weight.Inline.Ids}."

                if int64 weight.JsGzip > themeScriptBudget then
                    failtest
                        $"{page.Path} has no demos but ships {weight.JsGzip} bytes of gzipped JavaScript, over the theme script's {themeScriptBudget}-byte budget."
        }

        themeScriptTest page
    ]

let all () =
    let sitePages = pages |> List.filter (isDemoApp >> not)

    testList "Page weights" [
        test "the report covers every page" {
            let reported = report.Value.Keys |> set

            let missing = sitePages |> List.filter (fun p -> not (reported.Contains p.File))

            if not missing.IsEmpty then
                failtest $"""{reportFile} has no entry for: {String.Join(", ", missing |> List.map _.File)}"""
        }

        yield! sitePages |> List.map pageTests
    ]

/// Pages the zero-JavaScript check applies to, for the run's summary.
let zeroJavaScriptPages () =
    pages |> List.filter (fun p -> not (isDemoApp p) && not (hasDemos p))
