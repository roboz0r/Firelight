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

// The Templates guide's bindings: Ev.value sets the name as you type; Clear empties the box.
let private nameField =
    testTask "the name field greets what you type, and Clear empties it" {
        let page = pageWith "my-name-field"

        do!
            Browser.withPage
                true
                page.Path
                (fun opened ->
                    task {
                        let field = opened.Page.Locator("my-name-field").First
                        let input = field.Locator("input")
                        do! input.FillAsync("Rob")
                        do! Expect(field.Locator("p")).ToHaveTextAsync("Hello, Rob.")
                        do! field.Locator("button").ClickAsync()
                        do! Expect(input).ToHaveValueAsync("")
                        do! Expect(field.Locator("p")).ToHaveTextAsync("Hello, whoever you are.")
                        noProblems opened
                    }
                )
    }

// The Component libraries guide's Web Awesome demo, whose module imports the theme's CSS with
// `?inline`: the build loads it through Vite, so it is prerendered (Web Awesome's switches too)
// and shows without JavaScript; once hydrated, the switches drive the F# value.
let private webAwesomeSwitches =
    testTask "the Web Awesome switches are prerendered, and drive the F# value once hydrated" {
        let page = pageWith "my-switch-bindings"

        do!
            Browser.withPage
                false
                page.Path
                (fun opened ->
                    task {
                        let demo = opened.Page.Locator("my-switch-bindings").First
                        do! Expect(demo.Locator("wa-switch")).ToHaveCountAsync(2)
                        do! Expect(demo.Locator("p")).ToHaveTextAsync("The F# value is off.")

                        let! prerendered =
                            demo.Locator("wa-switch").EvaluateAllAsync<bool>("(all) => all.every((s) => !!s.shadowRoot)")

                        if not prerendered then
                            failtest $"{page.Path}: without JavaScript, a <wa-switch> has no prerendered shadow root."
                    }
                )

        do!
            Browser.withPage
                true
                page.Path
                (fun opened ->
                    task {
                        let demo = opened.Page.Locator("my-switch-bindings").First
                        let switches = demo.Locator("wa-switch")
                        do! switches.First.ClickAsync()
                        do! Expect(demo.Locator("p")).ToHaveTextAsync("The F# value is on.")
                        do! Expect(switches.Nth(1)).ToHaveJSPropertyAsync("checked", true)
                        do! demo.Locator("button").ClickAsync()
                        do! Expect(demo.Locator("p")).ToHaveTextAsync("The F# value is off.")
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

        // Browsers without URLPattern get urlpattern-polyfill through importPolyfill's dynamic import.
        testTask "without native URLPattern, the polyfill loads and the links route" {
            let page = pageWith "fl-route-explorer"
            let root = page.Path

            do!
                Browser.withPageAndScript
                    (Some "delete globalThis.URLPattern;")
                    true
                    page.Path
                    (fun opened ->
                        task {
                            do! renders opened root "the list of examples"

                            let! kind =
                                opened.Page.EvaluateAsync<string>(
                                    "() => typeof URLPattern !== 'function' ? 'missing' : /\\[native code\\]/.test(String(URLPattern)) ? 'native' : 'polyfill'"
                                )

                            Expect.equal kind "polyfill" $"{root}: URLPattern after deleting the native one"

                            Expect.exists
                                opened.Scripts
                                (fun path -> path.Contains "urlpattern-polyfill")
                                $"{root}: the polyfill's chunk wasn't loaded"

                            do!
                                opened.Page
                                    .Locator("fl-route-explorer")
                                    .GetByRole(
                                        AriaRole.Link,
                                        LocatorGetByRoleOptions(Name = "/users/42", Exact = true)
                                    )
                                    .ClickAsync()

                            do! renders opened (root + "users/42") "the profile of user 42"
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

// ContextRoot keeps the consumer's request until <my-late-provider> is defined, then sends it again.
let private lateProvider =
    testTask "a ContextRoot gives a consumer the value of a provider defined after it" {
        let page = pageWith "my-late-consumer"

        do!
            Browser.withPage
                true
                page.Path
                (fun opened ->
                    task {
                        let consumer = opened.Page.Locator("my-late-consumer")
                        do! Expect(consumer).ToHaveTextAsync("Waiting for a provider…")

                        let! definedEarly =
                            opened.Page.EvaluateAsync<bool>("() => customElements.get('my-late-provider') !== undefined")

                        Expect.isFalse definedEarly $"{page.Path}: <my-late-provider> was defined before the click"
                        do! opened.Page.Locator("my-provider-loader button").ClickAsync()
                        do! Expect(consumer).ToHaveTextAsync("Hello from the provider")
                        noProblems opened
                    }
                )
    }

// The probe listens to window resizes with Ev.listen while connected; its remover must stop that.
let private listenRemoves =
    testTask "Ev.listen's remover stops the listener, and connecting again listens again" {
        let page = pageWith "my-prerender-probe"

        do!
            Browser.withPage
                true
                page.Path
                (fun opened ->
                    task {
                        let width () =
                            opened.Page.EvaluateAsync<float>("() => window.probe.width")

                        let waitForWidth (w: int) =
                            opened.Page.WaitForFunctionAsync($"() => window.probe.width === {w}")
                            :> Threading.Tasks.Task

                        do! opened.Page.SetViewportSizeAsync(900, 700)
                        let! _ = opened.Page.EvaluateAsync("() => { window.probe = document.querySelector('my-prerender-probe'); }")
                        do! waitForWidth 900

                        // Disconnected: a resize must not reach the old listener.
                        let! _ = opened.Page.EvaluateAsync("() => { window.probe.remove(); }")
                        do! opened.Page.SetViewportSizeAsync(700, 700)
                        do! opened.Page.WaitForFunctionAsync("() => window.innerWidth === 700") :> Threading.Tasks.Task
                        // Give a stray listener a chance to run before checking.
                        let! _ = opened.Page.EvaluateAsync("() => new Promise(r => requestAnimationFrame(() => setTimeout(r, 100)))")
                        let! widthWhileRemoved = width ()
                        Expect.equal widthWhileRemoved 900.0 $"{page.Path}: the removed probe still heard a resize"

                        // Connected again: it listens again.
                        let! _ = opened.Page.EvaluateAsync("() => { document.body.append(window.probe); }")
                        do! waitForWidth 700
                        do! opened.Page.SetViewportSizeAsync(800, 700)
                        do! waitForWidth 800
                        noProblems opened
                    }
                )
    }

// PropertyDeclaration<float>() reads step="0.5" as a number; the tags' converter parses and reflects.
let private propertyTypes =
    testTask "a float property reads its attribute as a number, and a converter parses and reflects" {
        let page = pageWith "my-tags"

        do!
            Browser.withPage
                true
                page.Path
                (fun opened ->
                    task {
                        let quantity = opened.Page.Locator("my-quantity").First
                        do! quantity.Locator("button").ClickAsync()
                        do! quantity.Locator("button").ClickAsync()
                        // As text, 0 + "0.5" + "0.5" would be "00.50.5".
                        do! Expect(quantity.Locator("output")).ToHaveTextAsync("1")

                        let tags = opened.Page.Locator("my-tags").First
                        do! Expect(tags.Locator(".tag")).ToHaveTextAsync([| "lit"; "fable"; "fsharp" |])
                        do! tags.Locator("button").ClickAsync()
                        do! Expect(tags).ToHaveAttributeAsync("tags", "lit, fable, fsharp, tag4")

                        // Removing the attribute gives fromAttribute None: an empty list.
                        let! _ = tags.EvaluateAsync("el => el.removeAttribute('tags')")
                        do! Expect(tags.Locator(".tag")).ToHaveCountAsync(0)
                        noProblems opened
                    }
                )
    }

// The stopwatch controller inherits ReactiveControllerBase: it only ticks (re-renders its host
// without a click) if Lit's call to hostConnected reaches its override.
let private stopwatch =
    testTask "a ReactiveControllerBase controller's overrides run: the stopwatch ticks on its own" {
        let page = pageWith "my-stopwatch"

        do!
            Browser.withPage
                true
                page.Path
                (fun opened ->
                    task {
                        let watch = opened.Page.Locator("my-stopwatch").First
                        let time = watch.Locator(".time")
                        do! Expect(time).ToHaveTextAsync("0.0 s")
                        do! watch.GetByRole(AriaRole.Button, LocatorGetByRoleOptions(Name = "Start")).ClickAsync()
                        do! Expect(time).Not.ToHaveTextAsync(Regex "^0\\.0 s$")
                        let! first = time.TextContentAsync()
                        do! Expect(time).Not.ToHaveTextAsync(first)
                        noProblems opened
                    }
                )
    }

let all =
    testList "Demos" [ counter; rating; tutorial; nameField; typedEvents; webAwesomeSwitches; routing; lateProvider; listenRemoves; propertyTypes; stopwatch ]
