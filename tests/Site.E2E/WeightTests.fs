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
type Inline = { Scripts: int; Js: int; JsGzip: int }

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

/// A demo is a `.demo` box, as `::: example` and the hand-written pages render them.
let private hasDemos (page: Page) = (html page).Contains "class=\"demo"

/// Hand-written pages still highlight code at runtime with <fl-code>, which ships Shiki. Moving them
/// to Markdown (build-time Shiki, PLAN.md Phase 1 step 4) removes it.
let private usesRuntimeHighlighter (page: Page) = (html page).Contains "<fl-code"

let private gzipSize (file: string) =
    use output = new MemoryStream()

    do
        use gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen = true)
        gzip.Write(File.ReadAllBytes file)

    output.Length

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

        test "a page without demos ships no JavaScript" {
            if not (hasDemos page) && not (usesRuntimeHighlighter page) then
                let weight = weightOf page

                if weight.JsGzip <> 0 then
                    let sources =
                        [
                            yield! weight.Files
                            if weight.Inline.Scripts > 0 then
                                $"{weight.Inline.Scripts} inline scripts"
                        ]

                    failtest
                        $"""{page.Path} has no demos but ships {weight.JsGzip} bytes of gzipped JavaScript: {String.Join(", ", sources)}."""
        }
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

/// Pages the zero-JavaScript check applies to, for the run's summary: until the hand-written
/// pages are migrated there may be none.
let zeroJavaScriptPages () =
    pages
    |> List.filter (fun p -> not (isDemoApp p) && not (hasDemos p) && not (usesRuntimeHighlighter p))
