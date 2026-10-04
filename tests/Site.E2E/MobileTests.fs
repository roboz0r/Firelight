/// The site on a phone: at 375 px wide, every header link is on screen and can be tapped.
module Site.E2E.MobileTests

open System
open System.IO
open System.Net
open System.Text.RegularExpressions
open Expecto
open Microsoft.Playwright
open Site.E2E.Site

[<CLIMutable>]
type LinkBox =
    {
        Text: string
        Href: string
        Left: float
        Top: float
        Right: float
        Bottom: float
        /// The element at the link's centre is the link (or inside it): nothing covers it.
        OnTop: bool
    }

let private width = 375
let private height = 800

/// The links (text, href) in the site header of a built page, from its HTML, with entities decoded
/// as the browser decodes them.
let private builtHeaderLinks (file: string) =
    let html = File.ReadAllText(Path.Combine(Server.distDir, file))
    let header = Regex.Match(html, "<header class=\"site-header\"[\\s\\S]*?</header>")

    if not header.Success then
        failtest $"{file} has no <header class=\"site-header\">."

    Regex.Matches(header.Value, "<a\\s[^>]*?href=\"([^\"]*)\"[^>]*>([\\s\\S]*?)</a>")
    |> Seq.map (fun m ->
        let text = Regex.Replace(m.Groups[2].Value, "<!--[\\s\\S]*?-->|<[^>]+>", "")
        WebUtility.HtmlDecode(text).Trim(), WebUtility.HtmlDecode m.Groups[1].Value
    )
    |> List.ofSeq

/// Where the header links go, as the homepage's header has them: every other page's header must
/// match it (see `homepageHeader` for what keeps the homepage's own header honest).
let private expected = lazy (builtHeaderLinks "index.html" |> List.map snd)

/// The header's links that aren't sections (Layout.header): they stay the same as sections come
/// and go.
let private fixedLinks =
    [
        Server.basePath
        Server.basePath + "#examples"
        Server.basePath + "search/"
        "https://github.com/roboz0r/Firelight"
    ]

/// The homepage's header has the fixed links, and a link for each section of the site by the name
/// llms.txt gives it (one `## Section` per section with pages). Without this, a link missing from
/// every page, the homepage included, would go unnoticed.
let private homepageHeader =
    testCase "the header links every section of the site, and the links that aren't sections"
    <| fun () ->
        let llms = File.ReadAllText(Path.Combine(Server.distDir, "llms.txt"))

        let names =
            Regex.Matches(llms, "^## (.+)$", RegexOptions.Multiline)
            |> Seq.map _.Groups[1].Value.Trim()
            |> Seq.filter ((<>) "Home")
            |> List.ofSeq

        if names.Length < 2 then
            failtest $"llms.txt names {names.Length} sections; expected one per section of the site."

        let links = builtHeaderLinks "index.html"
        let texts = links |> List.map fst
        let hrefs = links |> List.map snd

        let missing =
            (names |> List.filter (fun name -> not (List.contains name texts)))
            @ (fixedLinks |> List.filter (fun href -> not (List.contains href hrefs)))

        if not missing.IsEmpty then
            let found = links |> List.map (fun (text, href) -> $"{text} ({href})")

            failtest
                $"""The homepage's header has no link for {String.Join(", ", missing)}; it links {String.Join(", ", found)}."""

let private headerLinks (page: Page) =
    testTask $"{page.Path}: every header link is visible and not covered" {
        let! (context: IBrowserContext) =
            Server
                .browserInstance()
                .NewContextAsync(BrowserNewContextOptions(ViewportSize = ViewportSize(Width = width, Height = height)))

        try
            let! (tab: IPage) = context.NewPageAsync()

            let! _ =
                tab.GotoAsync(Server.url page.Path, PageGotoOptions(WaitUntil = WaitUntilState.NetworkIdle))

            let! (links: LinkBox[]) =
                tab.EvaluateAsync<LinkBox[]>(
                    """() => [...document.querySelectorAll(".site-header a")].map((a) => {
                      const r = a.getBoundingClientRect();
                      const hit = document.elementFromPoint(r.left + r.width / 2, r.top + r.height / 2);
                      return { text: a.textContent.trim(), href: a.getAttribute("href"), left: r.left, top: r.top, right: r.right, bottom: r.bottom,
                               onTop: !!hit && (hit === a || a.contains(hit)) };
                    })"""
                )

            let hrefs = links |> Array.map _.Href |> List.ofArray

            if hrefs <> expected.Value then
                failtest
                    $"""{page.Path}: the header links to {String.Join(", ", hrefs)}; expected {String.Join(", ", expected.Value)}, as on the homepage."""

            let problems =
                [
                    for link in links do
                        if link.Right - link.Left < 1.0 || link.Bottom - link.Top < 1.0 then
                            $"\"{link.Text}\" has no size"
                        elif
                            link.Left < 0.0
                            || link.Right > float width
                            || link.Top < 0.0
                            || link.Bottom > float height
                        then
                            $"\"{link.Text}\" is not all on screen ({link.Left}, {link.Top} to {link.Right}, {link.Bottom} px)"
                        elif not link.OnTop then
                            $"\"{link.Text}\" is covered by something else"

                    for a, b in List.allPairs (List.ofArray links) (List.ofArray links) do
                        if
                            a.Text < b.Text
                            && a.Left < b.Right
                            && b.Left < a.Right
                            && a.Top < b.Bottom
                            && b.Top < a.Bottom
                        then
                            $"\"{a.Text}\" and \"{b.Text}\" overlap"
                ]

            if not problems.IsEmpty then
                failtest $"""{page.Path} at {width} px wide: {String.Join("; ", problems)}"""
        finally
            context.CloseAsync().GetAwaiter().GetResult()
    }

let all () =
    // Every page with the site's header (not the demo apps).
    homepageHeader :: (pages |> List.filter (isDemoApp >> not) |> List.map headerLinks)
    |> testList "Mobile"
