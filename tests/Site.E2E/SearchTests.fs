/// Search: Pagefind's index (`npm run build:search`) and the search page's <fl-search>.
module Site.E2E.SearchTests

open System
open System.IO
open System.Text.Json
open System.Text.RegularExpressions
open Expecto
open Microsoft.Playwright
open type Microsoft.Playwright.Assertions
open Site.E2E.Site

let private searchPage =
    lazy
        (pages
         |> List.tryFind (fun p -> p.File = "search/index.html")
         |> Option.defaultWith (fun () -> failtest $"No search page in {Server.distDir} (content/search.md)."))

let private noProblems (opened: Browser.OpenPage) =
    if opened.Problems.Count > 0 then
        failtest
            $"""{opened.Page.Url}: problems during the test:
  {String.Join("\n  ", opened.Problems)}"""

[<CLIMutable>]
type IndexedPage = { Url: string; Content: string }

let private pagefindRequests (opened: Browser.OpenPage) =
    opened.Scripts |> Seq.filter _.Contains("/pagefind/") |> List.ofSeq

let all () =
    testList "Search" [
        testTask "the index holds every listed page, and not their navigation or demos" {
            let entry = Path.Combine(Server.distDir, "pagefind", "pagefind-entry.json")

            if not (File.Exists entry) then
                failtest $"No Pagefind index at {entry}. Run `npm run build` in site/ (build:search)."

            // Every indexed page, with the text Pagefind took from it (`search(null)` is all of them).
            let! (indexed: IndexedPage[]) =
                Browser.withPage
                    true
                    searchPage.Value.Path
                    (fun opened ->
                        opened.Page.EvaluateAsync<IndexedPage[]>(
                            """async (base) => {
                              const pagefind = await import(base + "pagefind/pagefind.js");
                              await pagefind.options({ baseUrl: base });
                              const all = await pagefind.search(null);
                              const pages = await Promise.all(all.results.map((r) => r.data()));
                              return pages.map((p) => ({ url: p.url, content: p.content }));
                            }""",
                            Server.basePath
                        )
                    )

            let listed = pages |> List.filter (isUnlisted >> not) |> List.map _.Path |> set

            let urls = indexed |> Array.map _.Url |> set

            if urls <> listed then
                failtest
                    $"""The index and the sitemap differ. Not indexed: {String.Join(", ", listed - urls)}. Not in the sitemap: {String.Join(", ", urls - listed)}."""

            // Text that is only in the parts left out (`data-pagefind-ignore`): the table of contents,
            // the list of packages, a pager link and a demo's HTML, as in the HTML and as indexed
            // text. Each must be on some page, or this checks nothing.
            for html, text, part in
                [
                    "<p>On this page</p>", "On this page", "table of contents"
                    "<h2>All packages</h2>", "All packages", "list of packages"
                    "<span>Previous</span> Firelight.Context", "Previous Firelight.Context", "pager"
                    "<p id=\"rating-message\">Pick a rating.</p>", "Pick a rating.", "demo"
                ] do
                let onSomePage =
                    pages
                    |> List.filter (isUnlisted >> not)
                    |> List.exists (fun page -> File.ReadAllText(Path.Combine(Server.distDir, page.File)).Contains html)

                if not onSomePage then
                    failtest $"No page has {html} (the {part}) any more; pick other text for this check."

                for page in indexed do
                    if page.Content.Contains text then
                        failtest $"Pagefind indexed the {part} on {page.Url} (\"{text}\")."
        }

        testTask "searching for \"repeat\" finds the Templates guide" {
            do!
                Browser.withPage
                    true
                    searchPage.Value.Path
                    (fun opened ->
                        task {
                            if not (pagefindRequests opened).IsEmpty then
                                failtest "The search page loaded Pagefind before anything was searched for."

                            let search = opened.Page.Locator("fl-search")
                            do! search.GetByLabel("Search the docs").FillAsync("repeat")

                            let result = search.Locator($"""li a[href="{Server.basePath}guides/templates/"]""")

                            do! Expect(result).ToHaveTextAsync("Templates")
                            do! Expect(search.GetByRole(AriaRole.Status)).ToHaveTextAsync(Regex "pages? found")

                            if (pagefindRequests opened).IsEmpty then
                                failtest "Searching didn't load Pagefind's JavaScript."

                            // The address keeps the search.
                            if not (opened.Page.Url.EndsWith "?q=repeat") then
                                failtest $"The address is {opened.Page.Url}, without ?q=repeat."

                            noProblems opened
                        }
                    )
        }

        testTask "?q= in the address searches when the page opens" {
            do!
                Browser.withPage
                    true
                    (searchPage.Value.Path + "?q=context")
                    (fun opened ->
                        task {
                            let search = opened.Page.Locator("fl-search")
                            do! Expect(search.GetByLabel("Search the docs")).ToHaveValueAsync("context")

                            do!
                                Expect(search.Locator($"""li a[href="{Server.basePath}packages/context/"]"""))
                                    .ToBeVisibleAsync()

                            noProblems opened
                        }
                    )
        }

        testTask "every page's header links to the search page" {
            let link = $"""<a href="{searchPage.Value.Path}">Search</a>"""

            let missing =
                pages
                |> List.filter (fun p -> not (isDemoApp p))
                |> List.filter (fun p -> not (File.ReadAllText(Path.Combine(Server.distDir, p.File)).Contains link))

            if not missing.IsEmpty then
                failtest $"""No {link} on {String.Join(", ", missing |> List.map _.Path)}"""
        }
    ]
