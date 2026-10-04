/// The header's theme button (Renderer/Layout.fs, site.css): System, Light and Dark, kept in
/// localStorage, applied by a head script before the first paint, and followed by the palette, the
/// code blocks and the demos.
module Site.E2E.ThemeTests

open System
open System.Threading.Tasks
open Expecto
open Microsoft.Playwright
open Deque.AxeCore.Commons
open Deque.AxeCore.Playwright
open type Microsoft.Playwright.Assertions
open Site.E2E.Site

/// Layout.themeKey.
let private key = "firelight-theme"

/// --bg, light and dark (site.css).
let private lightBg = "rgb(255, 253, 249)"
let private darkBg = "rgb(23, 21, 26)"

/// A code block's background: github-light's and github-dark's.
let private lightCode = "rgb(255, 255, 255)"
let private darkCode = "rgb(36, 41, 46)"

[<CLIMutable>]
type ThemeState =
    {
        /// <html data-theme>, or null without one.
        Theme: string
        /// The button's accessible name (aria-label).
        Label: string
        /// The stored theme, null when none, "blocked" when localStorage throws.
        Stored: string
        Background: string
        /// The first code block's background, or null on a page without code.
        Code: string
        ButtonVisible: bool
    }

let private readState =
    $$"""() => {
      const button = document.getElementById("theme-toggle");
      const code = document.querySelector("pre.shiki");
      let stored;
      try { stored = localStorage.getItem("{{key}}"); } catch { stored = "blocked"; }
      return {
        theme: document.documentElement.dataset.theme ?? null,
        label: button?.getAttribute("aria-label") ?? null,
        stored,
        background: getComputedStyle(document.body).backgroundColor,
        code: code ? getComputedStyle(code).backgroundColor : null,
        buttonVisible: !!button && button.checkVisibility(),
      };
    }"""

let private state (page: IPage) = page.EvaluateAsync<ThemeState>(readState)

let private storeScript (value: string) =
    $$"""try { localStorage.setItem("{{key}}", "{{value}}"); } catch {}"""

let private options (scheme: ColorScheme) =
    BrowserNewContextOptions(ColorScheme = scheme, ViewportSize = ViewportSize(Width = 1280, Height = 800))

let private pageAt (file: string) =
    pages
    |> List.tryFind (fun p -> p.File = file)
    |> Option.defaultWith (fun () -> failtest $"No {file} in {Server.distDir}.")

let private home = lazy (pageAt "index.html")
let private templates = lazy (pageAt "guides/templates/index.html")

let private noProblems (opened: Browser.OpenPage) =
    if opened.Problems.Count > 0 then
        failtest $"""{opened.Page.Url}: problems during the test:
  {String.Join("\n  ", opened.Problems)}"""

let private expectState (where: string) (theme: string) (background: string) (s: ThemeState) =
    let name =
        match theme with
        | "light" -> "Light"
        | "dark" -> "Dark"
        | _ -> "System"

    Expect.equal s.Theme theme $"{where}: <html data-theme>"
    Expect.equal s.Label $"Theme: {name}" $"{where}: the button's accessible name"
    Expect.equal s.Background background $"{where}: the page's background"
    Expect.isTrue s.ButtonVisible $"{where}: the button is visible"

let private button (page: IPage) = page.Locator("#theme-toggle")

let private cycles =
    testTask "the button cycles System, Light, Dark, and back to System, and the code follows" {
        do!
            Browser.withPageIn
                (options ColorScheme.Light)
                None
                templates.Value.Path
                (fun opened ->
                    task {
                        let page = opened.Page
                        let! s = state page
                        expectState "On arrival" "system" lightBg s
                        Expect.equal s.Code lightCode "On arrival: the code's background"

                        let byName (name: string) =
                            page.GetByRole(
                                AriaRole.Button,
                                PageGetByRoleOptions(Name = $"Theme: {name}", Exact = true)
                            )

                        do! Expect(byName "System").ToBeVisibleAsync()

                        do! button(page).ClickAsync()
                        let! s = state page
                        expectState "After one click" "light" lightBg s
                        Expect.equal s.Stored "light" "After one click: stored"
                        Expect.equal s.Code lightCode "After one click: the code's background"
                        do! Expect(byName "Light").ToBeVisibleAsync()

                        do! button(page).ClickAsync()
                        let! s = state page
                        expectState "After two clicks" "dark" darkBg s
                        Expect.equal s.Stored "dark" "After two clicks: stored"
                        Expect.equal s.Code darkCode "After two clicks: the code's background"

                        do! button(page).ClickAsync()
                        let! s = state page
                        expectState "After three clicks" "system" lightBg s
                        Expect.isNull s.Stored "After three clicks, System removes the stored theme"
                        noProblems opened
                    }
                )
    }

[<CLIMutable>]
type Focus =
    {
        Focused: bool
        FocusVisible: bool
        OutlineStyle: string
        OutlineWidth: float
    }

let private keyboard =
    testTask "the button is reached with Tab, works with Enter and Space, and shows a focus ring" {
        do!
            Browser.withPageIn
                (options ColorScheme.Light)
                None
                home.Value.Path
                (fun opened ->
                    task {
                        let page = opened.Page
                        // The GitHub link is the last in the header; the button comes next.
                        do! page.Locator(".site-header nav a").Last.FocusAsync()
                        do! page.Keyboard.PressAsync "Tab"

                        let! focus =
                            page.EvaluateAsync<Focus>(
                                """() => {
                                  const b = document.getElementById("theme-toggle");
                                  const style = getComputedStyle(b);
                                  return { focused: document.activeElement === b, focusVisible: b.matches(":focus-visible"),
                                           outlineStyle: style.outlineStyle, outlineWidth: parseFloat(style.outlineWidth) };
                                }"""
                            )

                        Expect.isTrue focus.Focused "Tab from the last header link focuses the theme button"
                        Expect.isTrue focus.FocusVisible "the button matches :focus-visible"

                        if focus.OutlineStyle = "none" || focus.OutlineWidth < 2.0 then
                            failtest
                                $"The focused theme button has no visible focus ring (outline {focus.OutlineStyle} {focus.OutlineWidth}px)."

                        do! page.Keyboard.PressAsync "Enter"
                        let! s = state page
                        expectState "After Enter" "light" lightBg s
                        do! page.Keyboard.PressAsync "Space"
                        let! s = state page
                        expectState "After Space" "dark" darkBg s
                        noProblems opened
                    }
                )
    }

let private persists =
    testTask "the choice applies on the next page and after a reload" {
        do!
            Browser.withPageIn
                (options ColorScheme.Light)
                None
                home.Value.Path
                (fun opened ->
                    task {
                        let page = opened.Page
                        do! button(page).ClickAsync()
                        do! button(page).ClickAsync()
                        let! s = state page
                        expectState "Dark, chosen on the homepage" "dark" darkBg s

                        do!
                            page
                                .Locator(".site-header nav a", PageLocatorOptions(HasText = "Guides"))
                                .ClickAsync()

                        do! page.WaitForURLAsync($"**{Server.basePath}guides/")
                        let! s = state page
                        expectState "On /guides/, reached by its header link" "dark" darkBg s

                        let! _ = page.ReloadAsync(PageReloadOptions(WaitUntil = WaitUntilState.NetworkIdle))
                        let! s = state page
                        expectState "On /guides/, after a reload" "dark" darkBg s
                        noProblems opened
                    }
                )
    }

let private followsSystem =
    testTask "System follows the system's setting as it changes; Light and Dark don't" {
        do!
            Browser.withPageIn
                (options ColorScheme.Light)
                None
                templates.Value.Path
                (fun opened ->
                    task {
                        let page = opened.Page
                        let emulate (scheme: ColorScheme) = page.EmulateMediaAsync(PageEmulateMediaOptions(ColorScheme = scheme))

                        let! s = state page
                        expectState "System, on a light system" "system" lightBg s
                        do! emulate ColorScheme.Dark
                        let! s = state page
                        expectState "System, after the system turns dark" "system" darkBg s
                        Expect.equal s.Code darkCode "System, dark system: the code's background"

                        do! button(page).ClickAsync()
                        let! s = state page
                        expectState "Light, on a dark system" "light" lightBg s
                        Expect.equal s.Code lightCode "Light, dark system: the code's background"

                        do! button(page).ClickAsync()
                        do! emulate ColorScheme.Light
                        let! s = state page
                        expectState "Dark, on a light system" "dark" darkBg s
                        noProblems opened
                    }
                )
    }

[<CLIMutable>]
type Frame =
    {
        Theme: string
        Scheme: string
        /// The body's background, or null if the parser hadn't reached <body>.
        Background: string
    }

[<CLIMutable>]
type FirstPaint =
    {
        /// <html data-theme> when the parser had just reached <body>, or null if never seen.
        AtBody: string
        FirstFrame: Frame
        Loaded: Frame
    }

/// Stores "dark", then records the theme three times: when the parser has just added <body> (a
/// mutation observer's first callback that sees it, which runs before any script in the body), at
/// the first animation frame (rendering, and so painting, waits for the stylesheet in <head>; the
/// first frame's callbacks run before its paint), and at DOMContentLoaded, before the page's own
/// listeners. On a fast local server the whole page is often parsed by the first frame, so only the
/// first moment catches a theme script moved to the end of <body>.
let private firstPaintScript =
    storeScript "dark"
    + """
    const frame = () => ({
      theme: document.documentElement.dataset.theme ?? null,
      scheme: getComputedStyle(document.documentElement).colorScheme,
      background: document.body ? getComputedStyle(document.body).backgroundColor : null,
    });
    window.__atBody = undefined;
    const observer = new MutationObserver(() => {
      if (!document.body) return;
      window.__atBody = document.documentElement.dataset.theme ?? null;
      observer.disconnect();
    });
    observer.observe(document, { childList: true, subtree: true });
    requestAnimationFrame(() => { window.__firstFrame = frame(); });
    document.addEventListener("DOMContentLoaded", () => { window.__loaded = frame(); }, { once: true });
    """

let private noFlash (page: Page) =
    testTask $"{page.Path}: a stored dark theme is set before the first paint, on a light system" {
        do!
            Browser.withPageIn
                (options ColorScheme.Light)
                (Some firstPaintScript)
                page.Path
                (fun opened ->
                    task {
                        let! seen =
                            opened.Page.EvaluateAsync<FirstPaint>(
                                """() => ({ atBody: window.__atBody === undefined ? "(never seen)" : window.__atBody,
                                            firstFrame: window.__firstFrame ?? null, loaded: window.__loaded ?? null })"""
                            )

                        if isNull (box seen.FirstFrame) || isNull (box seen.Loaded) || seen.AtBody = "(never seen)" then
                            failtest $"{page.Path}: the init script recorded nothing, so this checks nothing."

                        if seen.AtBody <> "dark" then
                            failtest
                                $"{page.Path}: when the parser reached <body>, <html data-theme> was {seen.AtBody}, not dark: a stored dark theme could flash light. Is the theme script still in <head>?"

                        let check (moment: string) (f: Frame) =
                            if f.Theme <> "dark" || f.Scheme <> "dark" then
                                failtest
                                    $"{page.Path}: at {moment}, <html data-theme> was {f.Theme} and color-scheme {f.Scheme}, not dark: a stored dark theme would flash light. Is the theme script still first in <head>?"

                            if not (isNull f.Background) && f.Background <> darkBg then
                                failtest $"{page.Path}: at {moment}, the background was {f.Background}, not {darkBg}."

                        check "the first frame" seen.FirstFrame
                        check "DOMContentLoaded" seen.Loaded

                        if isNull seen.Loaded.Background then
                            failtest $"{page.Path}: no <body> at DOMContentLoaded."
                    }
                )
    }

let private withoutJavaScript =
    testTask "without JavaScript there is no button, and the site follows the system" {
        for scheme, background in [ ColorScheme.Light, lightBg; ColorScheme.Dark, darkBg ] do
            do!
                Browser.withPageIn
                    (BrowserNewContextOptions(ColorScheme = scheme, JavaScriptEnabled = false))
                    None
                    templates.Value.Path
                    (fun opened ->
                        task {
                            let! s = state opened.Page
                            Expect.isNull s.Theme "No data-theme without JavaScript"
                            Expect.isFalse s.ButtonVisible "The theme button is hidden without JavaScript"
                            Expect.equal s.Background background $"The background on a {scheme} system"
                        }
                    )
    }

let private storageEdgeCases =
    testList "stored values" [
        testTask "an unknown stored value reads as System" {
            do!
                Browser.withPageIn
                    (options ColorScheme.Dark)
                    (Some(storeScript "purple"))
                    home.Value.Path
                    (fun opened ->
                        task {
                            let! s = state opened.Page
                            expectState "With \"purple\" stored, on a dark system" "system" darkBg s
                            do! button(opened.Page).ClickAsync()
                            let! s = state opened.Page
                            expectState "One click on from it" "light" lightBg s
                            noProblems opened
                        }
                    )
        }

        testTask "with localStorage blocked the page loads, follows the system, and the button still works" {
            let blocked =
                """Object.defineProperty(window, "localStorage", {
                     get() { throw new DOMException("The operation is insecure.", "SecurityError"); } });"""

            do!
                Browser.withPageIn
                    (options ColorScheme.Dark)
                    (Some blocked)
                    templates.Value.Path
                    (fun opened ->
                        task {
                            let! s = state opened.Page
                            Expect.equal s.Stored "blocked" "the init script blocked localStorage"
                            expectState "Blocked storage, dark system" "system" darkBg s
                            do! button(opened.Page).ClickAsync()
                            let! s = state opened.Page
                            expectState "Blocked storage, one click" "light" lightBg s

                            // Rereading storage (back from the back/forward cache, another tab's
                            // change) must keep the choice rather than fall back to System.
                            let! _ =
                                opened.Page.EvaluateAsync(
                                    """() => {
                                      dispatchEvent(new PageTransitionEvent("pageshow", { persisted: true }));
                                      dispatchEvent(new StorageEvent("storage", { key: null }));
                                    }"""
                                )

                            let! s = state opened.Page
                            expectState "Blocked storage, after pageshow and storage events" "light" lightBg s
                            noProblems opened
                        }
                    )
        }

        testTask "a change in another tab applies here" {
            do!
                Browser.withPageIn
                    (options ColorScheme.Light)
                    None
                    home.Value.Path
                    (fun opened ->
                        task {
                            let! other = opened.Context.NewPageAsync()

                            let! _ =
                                other.GotoAsync(
                                    Server.url templates.Value.Path,
                                    PageGotoOptions(WaitUntil = WaitUntilState.NetworkIdle)
                                )

                            do! button(opened.Page).ClickAsync()
                            do! button(opened.Page).ClickAsync()
                            do! Expect(other.Locator("html")).ToHaveAttributeAsync("data-theme", "dark")
                            let! s = state other
                            expectState "The other tab" "dark" darkBg s
                        }
                    )
        }

        testTask "the cookbook's theme demo doesn't change the site's theme" {
            do!
                Browser.withPageIn
                    (options ColorScheme.Light)
                    None
                    (pageAt "cookbook/theme-toggle/index.html").Path
                    (fun opened ->
                        task {
                            do! opened.Page.Locator("my-theme-picker").GetByLabel("Dark").CheckAsync()
                            let! _ = opened.Page.ReloadAsync(PageReloadOptions(WaitUntil = WaitUntilState.NetworkIdle))
                            let! s = state opened.Page
                            expectState "After choosing Dark in the demo and reloading" "system" lightBg s
                        }
                    )
        }
    ]

/// The Web Awesome demo's switch track, inside two shadow roots: its colours come from the palette.
let private webAwesome =
    testTask "the Web Awesome demo follows a forced theme" {
        for theme, scheme, background in [ "dark", ColorScheme.Light, darkBg; "light", ColorScheme.Dark, lightBg ] do
            do!
                Browser.withPageIn
                    (options scheme)
                    (Some(storeScript theme))
                    (pageAt "guides/component-libraries/index.html").Path
                    (fun opened ->
                        task {
                            let! track =
                                opened.Page.EvaluateAsync<string>(
                                    """() => getComputedStyle(document.querySelector("my-switch-bindings")
                                        .shadowRoot.querySelector("wa-switch").shadowRoot.querySelector(".switch")).backgroundColor"""
                                )

                            Expect.equal track background $"The switch's track, {theme} theme on a {scheme} system"
                        }
                    )
    }

/// axe in forced Light on a dark system and forced Dark on a light system, on pages with code,
/// demos, compare blocks and search results (PageTests runs axe on every page in the default scheme).
let private contrast (path: string) (ready: IPage -> Task) =
    testList path [
        for theme, scheme, background in [ "light", ColorScheme.Dark, lightBg; "dark", ColorScheme.Light, darkBg ] do
            testTask $"{theme} theme, {scheme} system: no serious or critical axe violations" {
                do!
                    Browser.withPageIn
                        (options scheme)
                        (Some(storeScript theme))
                        path
                        (fun opened ->
                            task {
                                do! ready opened.Page
                                let! s = state opened.Page
                                Expect.equal s.Theme theme "the stored theme applied"
                                Expect.equal s.Background background "the page's background"
                                let! result = opened.Page.RunAxe()

                                let problems =
                                    [
                                        for v in result.Violations do
                                            if v.Impact = "serious" || v.Impact = "critical" then
                                                let shown =
                                                    String.Join(", ", v.Nodes |> Seq.map (fun n -> string n.Target) |> Seq.truncate 5)

                                                $"{v.Id} ({v.Impact}): {v.Help} at {shown}"
                                    ]

                                if not problems.IsEmpty then
                                    failtest $"""{path} in the {theme} theme: {String.Join("\n  ", problems)}"""

                                let checkedContrast =
                                    result.Passes
                                    |> Seq.filter (fun r -> r.Id = "color-contrast")
                                    |> Seq.sumBy (fun r -> r.Nodes.Length)

                                if checkedContrast < 10 then
                                    failtest
                                        $"{path} in the {theme} theme: axe checked the contrast of {checkedContrast} elements, so this checks little."
                            }
                        )
            }
    ]

let all () =
    let noWait (_: IPage) = Task.CompletedTask

    let searchResults (page: IPage) =
        page.Locator("fl-search li a").First.WaitForAsync()

    testList "Theme" [
        cycles
        keyboard
        persists
        followsSystem
        withoutJavaScript
        storageEdgeCases
        webAwesome
        testList "no flash" (
            [ home.Value; templates.Value; pageAt "packages/firelight/index.html"; pageAt "404.html" ]
            |> List.map noFlash
        )
        testList "contrast" [
            contrast home.Value.Path noWait
            contrast templates.Value.Path noWait
            contrast (pageAt "packages/firelight/index.html").Path noWait
            contrast (pageAt "guides/component-libraries/index.html").Path noWait
            contrast (pageAt "from-lit/components/index.html").Path noWait
            contrast ((pageAt "search/index.html").Path + "?q=component") searchResults
        ]
    ]
