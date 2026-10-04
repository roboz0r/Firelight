/// Interaction tests for the flagship demos. Each demo is found by its tag in the built HTML, so
/// these keep working when pages move.
module Site.E2E.DemoTests

open System
open System.IO
open System.Text.RegularExpressions
open Expecto
open Microsoft.Playwright
open type Microsoft.Playwright.Assertions
open Site.E2E.Site

/// The first page (other than 404.html) whose built HTML contains `<tag`.
let private pageWith (tag: string) =
    pages
    |> List.filter (fun p -> p.File <> "404.html")
    |> List.tryFind (fun p -> File.ReadAllText(Path.Combine(Server.distDir, p.File)).Contains $"<{tag}")
    |> Option.defaultWith (fun () -> failtest $"No page in {Server.distDir} has a <{tag}> demo.")

let private noProblems (opened: Browser.OpenPage) =
    if opened.Problems.Count > 0 then
        failtest
            $"""{opened.Page.Url}: problems during the test:
  {String.Join("\n  ", opened.Problems)}"""

let private counter =
    testTask "the counter increments when clicked" {
        let page = pageWith "my-counter"

        do!
            Browser.withPage
                true
                page.Path
                (fun opened ->
                    task {
                        let button = opened.Page.Locator("my-counter").First.Locator("button")
                        do! Expect(button).ToHaveTextAsync(Regex "Clicked 0 times")
                        do! button.ClickAsync()
                        do! button.ClickAsync()
                        do! Expect(button).ToHaveTextAsync(Regex "Clicked 2 times")
                        noProblems opened
                    }
                )
    }

let private rating =
    testTask "clicking a star sets the rating's value and the page's message" {
        let page = pageWith "my-rating"

        do!
            Browser.withPage
                true
                page.Path
                (fun opened ->
                    task {
                        let rating = opened.Page.Locator("my-rating").First
                        let stars = rating.Locator("button")
                        do! Expect(stars).ToHaveCountAsync(5)
                        do! stars.Nth(3).ClickAsync()

                        let! value = rating.EvaluateAsync<int>("el => el.value")
                        Expect.equal value 4 $"{page.Path}: <my-rating>.value after clicking the 4th star"
                        do! Expect(rating).ToHaveAttributeAsync("value", "4")
                        do! Expect(rating.Locator("button.on")).ToHaveCountAsync(4)
                        do! Expect(opened.Page.Locator("#rating-message")).ToHaveTextAsync("You picked 4 out of 5.")
                        noProblems opened
                    }
                )
    }

// The Getting started tutorial's steps: the event reaches the page script, and the list's buttons work.
let private tutorial =
    testTask "the tutorial's counter tells the page when it changes, and its step buttons add" {
        let page = pageWith "click-counter-events"

        do!
            Browser.withPage
                true
                page.Path
                (fun opened ->
                    task {
                        let events = opened.Page.Locator("click-counter-events button")
                        do! events.ClickAsync()
                        do! events.ClickAsync()
                        do! Expect(events).ToHaveTextAsync(Regex "Liked 2 times")

                        do!
                            Expect(opened.Page.Locator("#count-message"))
                                .ToHaveTextAsync("The page heard count-changed: 2.")

                        let list = opened.Page.Locator("click-counter-list")

                        do!
                            list
                                .GetByRole(AriaRole.Button, LocatorGetByRoleOptions(Name = "+10", Exact = true))
                                .ClickAsync()

                        do!
                            list
                                .GetByRole(AriaRole.Button, LocatorGetByRoleOptions(Name = "+1", Exact = true))
                                .ClickAsync()

                        do! Expect(list.Locator("span")).ToHaveTextAsync("Liked 11 times")
                        noProblems opened
                    }
                )
    }

// The Templates guide's typed handlers: Ev.value and Ev.keyboard in <my-tag-input>, and
// Ev.custom<string> in <my-tag-list>, which hears the input's tag-added event.
let private typedEvents =
    testTask "typed event handlers: Enter in the tag box adds the typed tag to the list" {
        let page = pageWith "my-tag-list"

        do!
            Browser.withPage
                true
                page.Path
                (fun opened ->
                    task {
                        let list = opened.Page.Locator("my-tag-list").First
                        let input = list.Locator("my-tag-input input")
                        do! Expect(list.Locator("li")).ToHaveTextAsync([| "fsharp"; "lit" |])
                        do! input.FillAsync("elmish")
                        do! input.PressAsync("Enter")
                        do! Expect(list.Locator("li")).ToHaveTextAsync([| "fsharp"; "lit"; "elmish" |])
                        do! Expect(input).ToHaveValueAsync("")
                        noProblems opened
                    }
                )
    }

let private routing =
    let renders (opened: Browser.OpenPage) (address: string) (description: string) =
        task {
            let result = opened.Page.Locator("fl-route-explorer .route-result dd")
            do! Expect(result.Nth(0)).ToHaveTextAsync(address)
            do! Expect(result.Nth(2)).ToHaveTextAsync(description)
        }

    testList "client-side routing" [
        testTask "links navigate without a reload, and the back button works" {
            let page = pageWith "fl-route-explorer"
            let root = page.Path

            do!
                Browser.withPage
                    true
                    page.Path
                    (fun opened ->
                        task {
                            do! renders opened root "the list of examples"
                            // A full page load would lose this.
                            let! _ = opened.Page.EvaluateAsync("() => { window.notReloaded = true; }")

                            do!
                                opened.Page
                                    .Locator("fl-route-explorer")
                                    .GetByRole(
                                        AriaRole.Link,
                                        LocatorGetByRoleOptions(Name = "/users/42", Exact = true)
                                    )
                                    .ClickAsync()

                            do! Expect(opened.Page).ToHaveURLAsync(Server.url (root + "users/42"))
                            do! renders opened (root + "users/42") "the profile of user 42"

                            do! opened.Page.GoBackAsync() :> Threading.Tasks.Task
                            do! Expect(opened.Page).ToHaveURLAsync(Server.url root)
                            do! renders opened root "the list of examples"

                            let! notReloaded =
                                opened.Page.EvaluateAsync<bool>("() => window.notReloaded === true")

                            Expect.isTrue
                                notReloaded
                                $"{root}: the page reloaded while following a route link or going back."

                            noProblems opened
                        }
                    )
        }

        testTask "a deep link opens the routing demo on that route" {
            let root = (pageWith "fl-route-explorer").Path

            do!
                Browser.withPage
                    true
                    (root + "users/7/posts/routing-in-fsharp")
                    (fun opened ->
                        task {
                            do!
                                renders
                                    opened
                                    (root + "users/7/posts/routing-in-fsharp")
                                    "post \"routing-in-fsharp\" by user 7"

                            noProblems opened
                        }
                    )
        }
    ]

let all = testList "Demos" [ counter; rating; tutorial; typedEvents; routing ]
