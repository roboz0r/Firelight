/// The sticky header (site.css): on wide screens it stays at the top of the window, over the page,
/// and in-page links stop below it; on narrow screens, where it wraps to over 9rem, it scrolls away.
module Site.E2E.HeaderTests

open System
open System.IO
open System.Threading.Tasks
open Expecto
open Microsoft.Playwright
open Site.E2E.Site

[<CLIMutable>]
type Box =
    {
        Text: string
        Top: float
        Bottom: float
        Left: float
        Right: float
        /// The element at the centre of the box is the element (or inside it): nothing covers it.
        OnTop: bool
    }

[<CLIMutable>]
type Scrolled =
    {
        ScrollY: float
        ViewportWidth: float
        /// Whether the page was too short to scroll that far, so a spacer was added to its end.
        Padded: bool
        Bar: Box
        BarBackground: string
        PageBackground: string
        Links: Box[]
        /// The theme button (null if the page has none).
        Toggle: Box
    }

[<CLIMutable>]
type LinkOverDemo =
    {
        Text: string
        /// Nothing covers the link's centre.
        OnTop: bool
        /// The demo is under the link's centre (in the stack of elements there).
        DemoUnder: bool
    }

[<CLIMutable>]
type Target =
    {
        Id: string
        ScrollY: float
        ViewportHeight: float
        Heading: Box
        Bar: Box
    }

let private wide = 1280
let private narrow = 375
let private height = 800

let private pageAt (file: string) =
    pages
    |> List.tryFind (fun p -> p.File = file)
    |> Option.defaultWith (fun () -> failtest $"No {file} in {Server.distDir}.")

/// The Templates guide: a long page with a table of contents.
let private templates = lazy (pageAt "guides/templates/index.html")

// A JavaScript function that measures an element's box and whether something else covers its
// centre.
let private boxOf =
    """
    const boxOf = (el) => {
      const r = el.getBoundingClientRect();
      const hit = document.elementFromPoint(r.left + r.width / 2, r.top + r.height / 2);
      return { text: el.textContent.trim().slice(0, 40), top: r.top, bottom: r.bottom, left: r.left, right: r.right,
               onTop: !!hit && (hit === el || el.contains(hit)) };
    };
    """

/// Scrolls `y` px down (adding a spacer to a page too short for that) and measures the header.
let private scrollAndMeasure (tab: IPage) (y: int) =
    tab.EvaluateAsync<Scrolled>(
        $$"""
        (y) => {
          {{boxOf}}
          const padded = document.documentElement.scrollHeight < y + innerHeight;
          if (padded) {
            const spacer = document.createElement("div");
            spacer.style.height = `${y + innerHeight}px`;
            document.querySelector("main").append(spacer);
          }
          window.scrollTo(0, y);
          const bar = document.querySelector(".site-header-bar");
          return {
            scrollY: scrollY,
            viewportWidth: document.documentElement.clientWidth,
            padded,
            bar: boxOf(bar),
            barBackground: getComputedStyle(bar).backgroundColor,
            pageBackground: getComputedStyle(document.body).backgroundColor,
            links: [...bar.querySelectorAll("a")].map(boxOf),
            toggle: bar.querySelector("#theme-toggle") && boxOf(bar.querySelector("#theme-toggle")),
          };
        }
        """,
        y
    )

/// Opens `path` in a fresh context `width` px wide, in the given colour scheme, once the network
/// is idle.
let private withViewport (width: int) (scheme: ColorScheme) (path: string) (f: IPage -> Task<'T>) =
    task {
        let! (context: IBrowserContext) =
            Server
                .browserInstance()
                .NewContextAsync(
                    BrowserNewContextOptions(
                        ViewportSize = ViewportSize(Width = width, Height = height),
                        ColorScheme = scheme
                    )
                )

        try
            let! (tab: IPage) = context.NewPageAsync()

            let! _ =
                tab.GotoAsync(Server.url path, PageGotoOptions(WaitUntil = WaitUntilState.NetworkIdle))

            return! f tab
        finally
            context.CloseAsync().GetAwaiter().GetResult()
    }

let private scrollBy = 2000

let private schemeName (scheme: ColorScheme) =
    if scheme = ColorScheme.Dark then "dark" else "light"

/// At 1280 px, scrolled 2000 px, the header is at the top of the window, its background spans the
/// window and hides the page, and its links can be clicked.
let private staysOnTop (scheme: ColorScheme) (page: Page) =
    testTask $"{page.Path}: at {wide} px ({schemeName scheme}), scrolled {scrollBy} px, the header is on top" {
        do!
            withViewport
                wide
                scheme
                page.Path
                (fun tab ->
                    task {
                        let! s = scrollAndMeasure tab scrollBy

                        let where =
                            $"{page.Path} at {wide} px ({schemeName scheme}), scrolled {s.ScrollY} px"

                        if s.ScrollY < float scrollBy - 1.0 then
                            failtest $"{where}: the page didn't scroll {scrollBy} px."

                        if page = templates.Value && s.Padded then
                            failtest
                                $"{page.Path} is no longer {scrollBy} px longer than the window; pick a longer page."

                        if abs s.Bar.Top > 0.5 then
                            failtest
                                $"{where}: the header's top is at {s.Bar.Top} px, not at the top of the window. Is .site-header-bar still sticky?"

                        if s.Bar.Left > 0.5 || s.Bar.Right < s.ViewportWidth - 0.5 then
                            failtest
                                $"{where}: the header's background spans {s.Bar.Left} to {s.Bar.Right} px, not the window's {s.ViewportWidth} px."

                        if s.BarBackground <> s.PageBackground || s.BarBackground.StartsWith "rgba" then
                            failtest
                                $"{where}: the header's background is {s.BarBackground}, not the page's opaque {s.PageBackground}, so the page shows through it."

                        if s.Links.Length < 2 then
                            failtest $"{where}: the header has {s.Links.Length} links."

                        if isNull (box s.Toggle) then
                            failtest $"{where}: the header has no theme button."

                        let problems =
                            [
                                for name, link in
                                    [
                                        for l in s.Links do
                                            yield $"\"{l.Text}\"", l
                                        yield "the theme button", s.Toggle
                                    ] do
                                    if link.Top < 0.0 || link.Bottom > s.Bar.Bottom then
                                        $"{name} is at {link.Top} to {link.Bottom} px, outside the header"
                                    elif link.Right - link.Left < 1.0 then
                                        $"{name} has no size"
                                    elif not link.OnTop then
                                        $"{name} is covered by something else"
                            ]

                        if not problems.IsEmpty then
                            failtest $"""{where}: {String.Join("; ", problems)}"""
                    }
                )
    }

/// A demo whose shadow DOM positions its contents (the virtualizer's rows are absolutely
/// positioned) would paint over a sticky header with no z-index. Scrolled so the demo is under the
/// header's links, the links must still be on top.
let private demoUnderHeader =
    testTask $"at {wide} px, the header's links stay on top of a positioned demo scrolled under them" {
        let tag = "my-big-list"
        let page = pageAt "packages/virtualizer/index.html"

        do!
            Browser.withPage
                true
                page.Path
                (fun opened ->
                    task {
                        let tab = opened.Page
                        do! tab.SetViewportSizeAsync(wide, height)

                        let! (links: LinkOverDemo[]) =
                            tab.EvaluateAsync<LinkOverDemo[]>(
                                $$"""
                                (tag) => {
                                  {{boxOf}}
                                  const demo = document.querySelector(tag);
                                  if (!demo) return null;
                                  // The demo's top 40 px above the window, so it spans the header.
                                  window.scrollTo(0, scrollY + demo.getBoundingClientRect().top + 40);
                                  return [...document.querySelectorAll(".site-header-bar a")].map((a) => {
                                    const { text, onTop, left, right, top, bottom } = boxOf(a);
                                    const under = document.elementsFromPoint((left + right) / 2, (top + bottom) / 2);
                                    return { text, onTop, demoUnder: under.includes(demo) };
                                  });
                                }
                                """,
                                tag
                            )

                        if isNull links then
                            failtest $"{page.Path} has no <{tag}> demo any more; pick another positioned demo."

                        let overlapping = links |> Array.filter _.DemoUnder

                        if overlapping.Length < 2 then
                            failtest
                                $"{page.Path}: the <{tag}> demo is under {overlapping.Length} header links, so this checks nothing."

                        let covered = overlapping |> Array.filter (fun l -> not l.OnTop) |> Array.map _.Text

                        if covered.Length > 0 then
                            failtest
                                $"""{page.Path}: the <{tag}> demo covers the header links {String.Join(", ", covered)}. Is .site-header-bar's z-index still set?"""
                    }
                )
    }

/// At 375 px the wrapped header is too tall to keep: it scrolls away with the page.
let private scrollsAway (page: Page) =
    testTask $"{page.Path}: at {narrow} px, scrolled {scrollBy} px, the header has scrolled away" {
        do!
            withViewport
                narrow
                ColorScheme.Light
                page.Path
                (fun tab ->
                    task {
                        let! s = scrollAndMeasure tab scrollBy

                        if s.ScrollY < float scrollBy - 1.0 then
                            failtest $"{page.Path} at {narrow} px: the page didn't scroll {scrollBy} px."

                        if s.Bar.Bottom > 0.0 then
                            failtest
                                $"{page.Path} at {narrow} px, scrolled {s.ScrollY} px: the header is still on screen ({s.Bar.Top} to {s.Bar.Bottom} px). It is sticky only on wide screens."
                    }
                )
    }

/// The narrowest width with a sticky header: just above the breakpoint in site.css (61rem = 976px).
let private narrowestSticky = 977

/// At the narrowest sticky width the header links still fit on one row beside the logo, so the
/// sticky header is no taller than --header-height (which the anchor offset assumes). A new header
/// link that makes them wrap fails this: raise the breakpoint in site.css and here.
let private oneRowWhenSticky =
    testTask $"at {narrowestSticky} px the sticky header's links fit on one row" {
        do!
            withViewport
                narrowestSticky
                ColorScheme.Light
                templates.Value.Path
                (fun tab ->
                    task {
                        let! s = scrollAndMeasure tab scrollBy

                        let! headerHeight =
                            tab.EvaluateAsync<float>(
                                "() => parseFloat(getComputedStyle(document.documentElement).getPropertyValue('--header-height')) * parseFloat(getComputedStyle(document.documentElement).fontSize)"
                            )

                        if s.Bar.Top <> 0.0 then
                            failtest
                                $"At {narrowestSticky} px the header isn't sticky (its top is at {s.Bar.Top} px after scrolling). Is the breakpoint in site.css still 61rem?"

                        let rows =
                            s.Links
                            |> Array.filter (fun l -> l.Text <> "Firelight") // the logo, beside the nav
                            |> Array.map (fun l -> Math.Round l.Top)
                            |> Array.distinct

                        if s.ScrollY < float scrollBy - 1.0 then
                            failtest $"At {narrowestSticky} px the page didn't scroll {scrollBy} px."

                        let navLinks = s.Links |> Array.filter (fun l -> l.Text <> "Firelight")

                        if
                            navLinks.Length < 2
                            || navLinks |> Array.exists (fun l -> l.Right - l.Left <= 0.0 || not l.OnTop)
                        then
                            failtest $"At {narrowestSticky} px the header's nav links are missing, empty or covered."

                        if rows.Length <> 1 || s.Bar.Bottom - s.Bar.Top > headerHeight + 1.0 then
                            failtest
                                $"At {narrowestSticky} px the header links wrap onto {rows.Length} rows and the header is {s.Bar.Bottom - s.Bar.Top} px tall, over --header-height ({headerHeight} px). Raise the header breakpoint in site.css and narrowestSticky here."

                        // The theme button sits on the links' row, after the last of them.
                        let last = navLinks |> Array.maxBy _.Right
                        let t = s.Toggle

                        if
                            isNull (box t)
                            || not t.OnTop
                            || t.Left < last.Right
                            || t.Right > float narrowestSticky
                            || t.Top > last.Bottom
                            || t.Bottom < last.Top
                        then
                            failtest
                                $"At {narrowestSticky} px the theme button isn't on the links' row, after them, on screen and uncovered: {t}."
                    }
                )
    }

/// Measures the element with id `id` and the header, waiting for any smooth scrolling to stop.
let private measureTarget (tab: IPage) (id: string) =
    tab.EvaluateAsync<Target>(
        $$"""
        async (id) => {
          {{boxOf}}
          let last = -1;
          for (let still = 0; still < 5; ) {
            await new Promise((resolve) => setTimeout(resolve, 50));
            if (scrollY === last) still++; else { still = 0; last = scrollY; }
          }
          const target = document.getElementById(id);
          return {
            id,
            scrollY,
            viewportHeight: innerHeight,
            heading: target ? boxOf(target) : null,
            bar: boxOf(document.querySelector(".site-header-bar")),
          };
        }
        """,
        id
    )

/// The jump to `id` left it on screen, below the header: `how` says how it got there.
let private checkTarget (how: string) (t: Target) =
    if isNull (box t.Heading) then
        failtest $"{how}: no element has the id {t.Id}."

    if t.ScrollY < 1.0 then
        failtest $"{how}: the page didn't scroll, so this checks nothing; pick a heading further down."

    if t.Heading.Top < t.Bar.Bottom - 0.5 then
        failtest
            $"{how}: #{t.Id}'s top is at {t.Heading.Top} px, under the header (which ends at {t.Bar.Bottom} px). Is html's scroll-padding-top still set?"

    if t.Heading.Bottom > t.ViewportHeight then
        failtest $"{how}: #{t.Id} ends at {t.Heading.Bottom} px, below the window ({t.ViewportHeight} px)."

/// The ids the page's table of contents links to.
let private tocIds (page: Page) =
    let html = File.ReadAllText(Path.Combine(Server.distDir, page.File))
    let toc = html.Substring(html.IndexOf "<nav class=\"toc\"")
    let toc = toc.Substring(0, toc.IndexOf "</nav>")

    [
        for m in Text.RegularExpressions.Regex.Matches(toc, "href=\"#([^\"]+)\"") -> m.Groups[1].Value
    ]

/// Opening a `#heading` address scrolls the heading to just below the header.
let private hashOnLoad =
    testTask $"at {wide} px, a #heading address shows the heading below the header" {
        let page = templates.Value
        // Every heading in the table of contents, and the homepage's sections (the header's
        // Examples link goes to one).
        let targets =
            [
                for id in tocIds page -> page.Path, id
                for id in [ "examples"; "packages"; "demos" ] -> Server.basePath, id
            ]

        if (tocIds page).Length < 3 then
            failtest $"{page.Path}'s table of contents has {(tocIds page).Length} entries."

        do!
            withViewport
                wide
                ColorScheme.Light
                page.Path
                (fun tab ->
                    task {
                        for path, id in targets do
                            // A fresh load each time: from the same page, only the hash would change.
                            let! _ = tab.GotoAsync "about:blank"

                            let! _ =
                                tab.GotoAsync(
                                    Server.url (path + "#" + id),
                                    PageGotoOptions(WaitUntil = WaitUntilState.NetworkIdle)
                                )

                            let! t = measureTarget tab id
                            checkTarget $"Opening {path}#{id} at {wide} px" t
                    }
                )
    }

/// Clicking a table-of-contents link scrolls its heading to just below the header.
let private tocClicks =
    testTask $"at {wide} px, a table-of-contents link scrolls its heading below the header" {
        let page = templates.Value

        do!
            withViewport
                wide
                ColorScheme.Light
                page.Path
                (fun tab ->
                    task {
                        for id in tocIds page do
                            let! _ = tab.EvaluateAsync "() => window.scrollTo(0, 0)"
                            do! tab.Locator($""".toc a[href="#{id}"]""").ClickAsync()
                            let! t = measureTarget tab id
                            checkTarget $"Clicking \"#{id}\" in {page.Path}'s table of contents" t
                    }
                )
    }

/// Firelight.Router's RouterController scrolls smoothly to `#hash` links (scrollIntoView), which
/// must stop below the header too. The routing page has no such link, so the test adds one.
let private routerHashLink =
    testTask $"at {wide} px, the router's smooth scrolling to a #hash link stops below the header" {
        let page = pageAt "client-side-routing/index.html"
        let id = "how-it-works"

        do!
            Browser.withPage
                true
                page.Path
                (fun opened ->
                    task {
                        let tab = opened.Page
                        do! tab.SetViewportSizeAsync(wide, height)

                        let! _ =
                            tab.EvaluateAsync(
                                """(id) => {
                                  const a = document.createElement("a");
                                  a.id = "header-test-link";
                                  a.href = "#" + id;
                                  a.textContent = "Jump";
                                  document.querySelector("main").prepend(a);
                                  // After the router's listener on document: did it handle the click?
                                  window.addEventListener("click", (e) => { window.routerHandled = e.defaultPrevented; }, { once: true });
                                }""",
                                id
                            )

                        do! tab.Locator("#header-test-link").ClickAsync()
                        let! t = measureTarget tab id
                        let! handled = tab.EvaluateAsync<bool> "() => window.routerHandled === true"

                        if not handled then
                            failtest
                                $"{page.Path}: the router didn't handle the click on a #{id} link (no preventDefault), so this doesn't test its scrolling."

                        checkTarget $"The router scrolling to #{id} on {page.Path}" t

                        if opened.Problems.Count > 0 then
                            failtest $"""{page.Path}: {String.Join("\n  ", opened.Problems)}"""
                    }
                )
    }

/// At 375 px the header has scrolled away, so a `#heading` address puts the heading at the top.
let private hashOnLoadNarrow =
    testTask $"at {narrow} px, a #heading address shows the heading at the top of the window" {
        let page = templates.Value
        let id = tocIds page |> List.item 2

        do!
            withViewport
                narrow
                ColorScheme.Light
                (page.Path + "#" + id)
                (fun tab ->
                    task {
                        let! t = measureTarget tab id
                        checkTarget $"Opening {page.Path}#{id} at {narrow} px" t

                        if t.Heading.Top > 1.0 then
                            failtest
                                $"Opening {page.Path}#{id} at {narrow} px: the heading is at {t.Heading.Top} px, below an offset for a header that isn't sticky there."
                    }
                )
    }

let all () =
    // Every page with the site's header (not the demo apps), and the layouts that differ: the
    // homepage (layout: home) and the not-found page.
    let withHeader = pages |> List.filter (isDemoApp >> not)

    let layouts = [ templates.Value; pageAt "index.html"; pageAt "404.html" ]

    testList "Header" [
        testList "sticky" [
            yield! withHeader |> List.map (staysOnTop ColorScheme.Light)
            yield! layouts |> List.map (staysOnTop ColorScheme.Dark)
            demoUnderHeader
            oneRowWhenSticky
        ]
        testList "narrow" (layouts |> List.map scrollsAway)
        testList "anchors" [ hashOnLoad; tocClicks; routerHashLink; hashOnLoadNarrow ]
    ]
