/// Checks that run on every page of the built site.
module Site.E2E.PageTests

open System
open System.IO
open Expecto
open Deque.AxeCore.Playwright
open Site.E2E.Site

let private fail (page: Page) (problem: string) (details: string seq) =
    failtest
        $"""{page.Path} ({page.File}): {problem}
  {String.Join("\n  ", details)}"""

let private loadsCleanly (page: Page) =
    testTask "loads with no console errors or warnings, failed requests or uncaught exceptions" {
        do!
            Browser.withPage
                true
                page.Path
                (fun opened ->
                    task {
                        if opened.Problems.Count > 0 then
                            fail page "problems while loading:" opened.Problems
                    }
                )
    }

let private http = new Net.Http.HttpClient()

/// Every same-site URL linked to, fetched once per run: its status and HTML.
let private fetched =
    Collections.Concurrent.ConcurrentDictionary<string, Lazy<Threading.Tasks.Task<int * string>>>()

let private fetch (url: string) =
    fetched
        .GetOrAdd(
            url,
            fun url ->
                lazy
                    (task {
                        use! response = http.GetAsync url
                        let! body = response.Content.ReadAsStringAsync()
                        return int response.StatusCode, body
                    })
        )
        .Value

let private hasAnchor (html: string) (id: string) =
    let id = Text.RegularExpressions.Regex.Escape id
    Text.RegularExpressions.Regex.IsMatch(html, $"""\s(id|name)\s*=\s*["']?{id}["'\s>]""")

// Links on the site, including in demos and shadow roots, must lead to a page that exists and,
// with a #fragment, to an element with that id. (Links to other sites are links.yml's job.)
let private linksResolve (page: Page) =
    testTask "every link within the site resolves" {
        do!
            Browser.withPage
                true
                page.Path
                (fun opened ->
                    task {
                        let! hrefs =
                            opened.Page.EvaluateAsync<string[]>(
                                """() => {
                              const hrefs = new Set();
                              const walk = (root) => {
                                for (const a of root.querySelectorAll("a[href], area[href]")) hrefs.add(a.href);
                                for (const el of root.querySelectorAll("*")) if (el.shadowRoot) walk(el.shadowRoot);
                              };
                              walk(document);
                              return [...hrefs];
                            }"""
                            )

                        let broken = Collections.Generic.List<string>()

                        for href in hrefs |> Array.filter (fun h -> h.StartsWith(Server.origin + "/")) do
                            let uri = Uri href
                            let! status, html = fetch (uri.GetLeftPart UriPartial.Query)
                            let fragment = Uri.UnescapeDataString(uri.Fragment.TrimStart '#')

                            if status >= 400 then
                                broken.Add $"{uri.PathAndQuery}{uri.Fragment}: HTTP {status}"
                            elif fragment <> "" && not (hasAnchor html fragment) then
                                broken.Add $"{uri.PathAndQuery}{uri.Fragment}: no element with id \"{fragment}\""

                        if broken.Count > 0 then
                            fail page "broken links:" broken
                    }
                )
    }

let private demosRender (page: Page) =
    testTask "every demo renders something" {
        do!
            Browser.withPage
                true
                page.Path
                (fun opened ->
                    task {
                        let! empty = Browser.emptyDemos opened.Page

                        if empty.Length > 0 then
                            fail page "empty demos:" empty
                    }
                )
    }

let private elementsRegistered (page: Page) =
    testTask "every custom element is registered" {
        do!
            Browser.withPage
                true
                page.Path
                (fun opened ->
                    task {
                        let! missing = Browser.unregisteredElements opened.Page

                        if missing.Length > 0 then
                            fail
                                page
                                "custom elements that were never defined (customElements.get is undefined):"
                                missing
                    }
                )
    }

type KnownViolation =
    {
        Rule: string
        /// Which of the rule's elements (axe's CSS selector for each) this entry covers.
        Covers: string -> bool
        Reason: string
    }

/// Violations already on the site when these checks were added, each waiting on a design decision
/// or on other work. They are reported on every run instead of failing it; anything else fails.
/// Fix them rather than add to them: a full run fails once an entry no longer occurs, so this list
/// only shrinks.
let knownViolations =
    [
        {
            Rule = "color-contrast"
            // Only Shiki's code lines. Contrast failures anywhere else fail the run.
            Covers = fun target -> target.Contains ".line"
            Reason =
                "github-light colours identifiers #e36209, 3.5:1 on its white background, below WCAG AA's 4.5:1 (and github-dark's comments, #6a737d, are 3.1:1, though axe only checks the light theme). Needs another theme or colour replacements."
        }
        {
            Rule = "valid-lang"
            Covers = fun target -> target.Contains "fl-code"
            Reason =
                "<fl-code lang=\"fsharp\"> uses the global lang attribute for the code's language. Goes away as pages move to build-time Shiki (PLAN.md Phase 1 step 4)."
        }
    ]

/// Pages on which each known violation was seen, and the number of pages axe ran on.
let knownViolationsSeen =
    Collections.Concurrent.ConcurrentDictionary<string, string list>()

let mutable axeRuns = 0

let private accessible (page: Page) =
    testTask "no serious or critical axe violations" {
        do!
            Browser.withPage
                true
                page.Path
                (fun opened ->
                    task {
                        let! result = opened.Page.RunAxe()
                        Threading.Interlocked.Increment &axeRuns |> ignore

                        let problems =
                            [
                                for v in result.Violations do
                                    if v.Impact = "serious" || v.Impact = "critical" then
                                        let known = knownViolations |> List.tryFind (fun k -> k.Rule = v.Id)

                                        let isKnown (target: string) =
                                            known |> Option.exists (fun k -> k.Covers target)

                                        let targets, knownTargets =
                                            v.Nodes
                                            |> Array.map (fun n -> string n.Target)
                                            |> Array.partition (isKnown >> not)

                                        if knownTargets.Length > 0 then
                                            knownViolationsSeen.AddOrUpdate(
                                                v.Id,
                                                [ page.Path ],
                                                (fun _ seen -> page.Path :: seen)
                                            )
                                            |> ignore

                                        if targets.Length > 0 then
                                            let shown = String.Join(", ", targets |> Seq.truncate 5)
                                            $"{v.Id} ({v.Impact}): {v.Help} at {shown} ({v.HelpUrl})"
                            ]

                        if not problems.IsEmpty then
                            fail page "accessibility violations:" problems
                    }
                )
    }

let private isDoubled (before: string) (after: string) =
    before <> "" && (after = before + before || after = before + " " + before)

// The spike's silent bug: with hydration support loaded too late, a prerendered component renders a
// second copy into its declarative shadow root (10 stars instead of 5, no console message). Load the
// page without JavaScript to see what was prerendered, then with it, and compare each component.
let private hydratesWithoutDuplicates (page: Page) =
    testTask "hydration doesn't duplicate prerendered content" {
        let! (before: Browser.ShadowSnapshot[]) =
            Browser.withPage false page.Path (fun opened -> Browser.shadowSnapshots opened.Page)

        // Guards against this check passing vacuously: if the HTML has declarative shadow roots,
        // the JavaScript-free load must have found them.
        let html = File.ReadAllText(Path.Combine(Server.distDir, page.File))

        if html.Contains "shadowrootmode=" && before.Length = 0 then
            fail page "the HTML has declarative shadow roots, but none were found with JavaScript disabled." []

        if before.Length > 0 then
            let! (after: Browser.ShadowSnapshot[]) =
                Browser.withPage true page.Path (fun opened -> Browser.shadowSnapshots opened.Page)

            let after = after |> Seq.map (fun s -> s.Key, s) |> Map.ofSeq

            let problems =
                [
                    for b in before do
                        match after.TryFind b.Key with
                        | None -> $"{b.Key} was prerendered but isn't on the page after hydration."
                        | Some a ->
                            if a.OutsideLitParts.Length > 0 then
                                let extra = String.Join(", ", a.OutsideLitParts |> Seq.truncate 10)

                                $"{b.Key} rendered content outside its prerendered lit-part markers (a second render): {extra}"

                            if isDoubled b.Text a.Text then
                                $"{b.Key} shows its prerendered text twice: \"{b.Text}\" became \"{a.Text}\"."
                            elif
                                b.Elements > 0
                                && a.Elements = 2 * b.Elements
                                && (a.Text = b.Text || isDoubled b.Text a.Text)
                            then
                                $"{b.Key} has twice its prerendered elements: {b.Elements} became {a.Elements}."
                ]

            if not problems.IsEmpty then
                fail page "prerendered components changed when they hydrated:" problems
    }

let private pageTests (page: Page) =
    testList page.Path [
        loadsCleanly page
        demosRender page
        linksResolve page
        elementsRegistered page
        accessible page
        hydratesWithoutDuplicates page
    ]

let private discovery =
    testList "Discovery" [
        test "the build has pages" { Expect.isNonEmpty pages $"No pages found in {Server.distDir}." }

        test "the sitemap lists every page" {
            let missing = missingFromSitemap ()

            if not missing.IsEmpty then
                failtest $"""sitemap.xml doesn't list: {String.Join(", ", missing |> List.map _.File)}"""
        }

        test "every sitemap URL has a page in dist" {
            for page in sitemapPages |> Option.defaultValue [] do
                if not (File.Exists(Path.Combine(Server.distDir, page.File))) then
                    failtest $"sitemap.xml lists {page.Path}, but dist/{page.File} doesn't exist."
        }
    ]

let all () =
    testList "Pages" [ discovery; yield! pages |> List.map pageTests ]
