/// The site on a phone: at 375 px wide, every header link is on screen and can be tapped.
module Site.E2E.MobileTests

open System
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

/// Where the header links go: the logo, then each section and the rest (Layout.header).
let private expected =
    [
        Server.basePath
        Server.basePath + "#examples"
        Server.basePath + "start/"
        Server.basePath + "#packages"
        Server.basePath + "guides/"
        Server.basePath + "#demos"
        Server.basePath + "search/"
        "https://github.com/roboz0r/Firelight"
    ]

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

            if hrefs <> expected then
                failtest
                    $"""{page.Path}: the header links to {String.Join(", ", hrefs)}; expected {String.Join(", ", expected)}."""

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
    pages
    |> List.filter (isDemoApp >> not)
    |> List.map headerLinks
    |> testList "Mobile"
