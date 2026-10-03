/// Accessibility of the rating demo: its stars are named buttons that show the rating, an
/// unselected star is visible against the demo box, and a keyboard user can see which one has focus.
module Site.E2E.RatingTests

open System
open System.IO
open Expecto
open Microsoft.Playwright
open type Microsoft.Playwright.Assertions
open Site.E2E.Site

let private ratingPage () =
    pages
    |> List.filter (fun p -> p.File <> "404.html")
    |> List.tryFind (fun p -> File.ReadAllText(Path.Combine(Server.distDir, p.File)).Contains "<my-rating")
    |> Option.defaultWith (fun () -> failtest $"No page in {Server.distDir} has a <my-rating> demo.")

let private noProblems (opened: Browser.OpenPage) =
    if opened.Problems.Count > 0 then
        failtest
            $"""{opened.Page.Url}: problems during the test:
  {String.Join("\n  ", opened.Problems)}"""

let private names =
    testTask "the rating's stars are buttons named \"n out of 5\", and the current one is pressed" {
        let page = ratingPage ()

        do!
            Browser.withPage
                true
                page.Path
                (fun opened ->
                    task {
                        let rating = opened.Page.Locator("my-rating").First
                        do! Expect(rating.GetByRole(AriaRole.Button)).ToHaveCountAsync(5)

                        let star n =
                            rating.GetByRole(AriaRole.Button, LocatorGetByRoleOptions(Name = $"{n} out of 5", Exact = true))

                        for n in 1..5 do
                            // The page's markup sets value="3".
                            do! Expect(star n).ToHaveAttributeAsync("aria-pressed", (if n = 3 then "true" else "false"))

                        do! (star 5).ClickAsync()
                        do! Expect(star 5).ToHaveAttributeAsync("aria-pressed", "true")
                        do! Expect(star 3).ToHaveAttributeAsync("aria-pressed", "false")
                        noProblems opened
                    }
                )
    }

/// For a star: whether it matches :focus-visible, its outline's style and width, and the contrast
/// of its outline and of its text against the first opaque background behind it (the demo box).
let private measure =
    """el => {
      const rgb = (c) => c.match(/[\d.]+/g).map(Number);
      const luminance = (c) => rgb(c).slice(0, 3)
        .map((v) => (v /= 255) <= 0.03928 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4)
        .reduce((sum, v, i) => sum + v * [0.2126, 0.7152, 0.0722][i], 0);
      const contrast = (a, b) => {
        const [x, y] = [luminance(a), luminance(b)].sort((p, q) => q - p);
        return (x + 0.05) / (y + 0.05);
      };
      let behind = el;
      while (getComputedStyle(behind).backgroundColor === "rgba(0, 0, 0, 0)")
        behind = behind.parentElement ?? behind.getRootNode().host;
      const background = getComputedStyle(behind).backgroundColor;
      const s = getComputedStyle(el);
      return [String(el.matches(":focus-visible")), s.outlineStyle, s.outlineWidth,
              String(contrast(s.outlineColor, background)), String(contrast(s.color, background))];
    }"""

let private focusRing (scheme: ColorScheme) =
    testTask $"in the {scheme} theme, stars contrast 3:1 with the demo box, and a star focused from the keyboard has a visible outline" {
        let page = ratingPage ()

        do!
            Browser.withPage
                true
                page.Path
                (fun opened ->
                    task {
                        do! opened.Page.EmulateMediaAsync(PageEmulateMediaOptions(ColorScheme = scheme))
                        let stars = opened.Page.Locator("my-rating").First.GetByRole(AriaRole.Button)

                        let star n =
                            task {
                                let! found = stars.Nth(n - 1).EvaluateAsync<string[]>(measure)

                                return
                                    {|
                                        FocusVisible = found[0] = "true"
                                        Style = found[1]
                                        Width = found[2]
                                        OutlineContrast = float found[3]
                                        TextContrast = float found[4]
                                    |}
                            }

                        // The page's markup sets value="3", so the 5th star is unselected.
                        let! unselected = star 5
                        Expect.isGreaterThanOrEqual unselected.TextContrast 3.0 "an unselected star's contrast"
                        let! before = star 3
                        Expect.equal before.Style "none" "an unfocused star has no outline"

                        // Focus the 2nd star, then Tab to the 3rd, so the focus comes from the keyboard.
                        do! stars.Nth(1).FocusAsync()
                        do! opened.Page.Keyboard.PressAsync("Tab")
                        do! Expect(stars.Nth 2).ToBeFocusedAsync()

                        let! focused = star 3
                        Expect.isTrue focused.FocusVisible "the star matches :focus-visible"
                        Expect.notEqual focused.Style "none" "the focused star's outline style"
                        Expect.notEqual focused.Width "0px" "the focused star's outline width"
                        Expect.isGreaterThanOrEqual focused.OutlineContrast 3.0 "the focused star's outline contrast"
                        noProblems opened
                    }
                )
    }

let all =
    testList "Rating accessibility" [ names; focusRing ColorScheme.Light; focusRing ColorScheme.Dark ]
