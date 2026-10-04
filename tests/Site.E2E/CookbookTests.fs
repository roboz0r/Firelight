/// Interaction tests for the cookbook's demos (site/content/cookbook). Each demo is found by its
/// tag in the built HTML, so these keep working when pages move.
module Site.E2E.CookbookTests

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

/// Opens the page with the `tag` demo and runs `f` with the page and the demo's first element.
let private withDemo (tag: string) (f: Browser.OpenPage -> ILocator -> Threading.Tasks.Task<unit>) =
    Browser.withPage true (pageWith tag).Path (fun opened -> f opened (opened.Page.Locator(tag).First))

let private signupForm =
    testTask "form validation: errors on submit, focus on the first, thanks once fixed" {
        do!
            withDemo
                "my-signup-form"
                (fun opened form ->
                    task {
                        let button = form.GetByRole(AriaRole.Button, LocatorGetByRoleOptions(Name = "Sign up"))
                        let name = form.GetByLabel("Name")
                        let email = form.GetByLabel("Email")
                        do! Expect(form.Locator(".error")).ToHaveCountAsync(0)
                        do! button.ClickAsync()
                        do! Expect(form.Locator(".error")).ToHaveCountAsync(2)
                        do! Expect(name).ToBeFocusedAsync()
                        do! Expect(name).ToHaveAttributeAsync("aria-describedby", "name-error")
                        do! Expect(name).ToHaveAccessibleDescriptionAsync("Enter your name.")
                        do! name.FillAsync("Ada")
                        do! Expect(form.Locator(".error")).ToHaveCountAsync(1)
                        do! email.FillAsync("ada@")
                        do! button.ClickAsync()
                        do! Expect(email).ToBeFocusedAsync()
                        do! Expect(form.Locator(".error")).ToHaveCountAsync(1)
                        do! Expect(name).Not.ToHaveAttributeAsync("aria-invalid", "true")
                        do! email.FillAsync("ada@example.com")
                        do! button.ClickAsync()
                        do! Expect(form.GetByRole(AriaRole.Status)).ToHaveTextAsync("Thanks, Ada. Check your inbox.")
                        do! Expect(name).ToHaveValueAsync("")
                        do! Expect(form.Locator(".error")).ToHaveCountAsync(0)
                        noProblems opened
                    }
                )
    }

let private formSwitch =
    testTask "custom form control: the label and Space toggle it, and the page's form submits and resets it" {
        do!
            withDemo
                "my-form-switch"
                (fun opened control ->
                    task {
                        let page = opened.Page
                        let isChecked () = control.EvaluateAsync<bool>("el => el.checked")
                        let result = page.Locator("#switch-result")
                        let submit = page.GetByRole(AriaRole.Button, PageGetByRoleOptions(Name = "Submit"))

                        do! submit.ClickAsync()
                        do! Expect(result).ToHaveTextAsync("The form would send nothing.")

                        // A change listener sees the new value in the form.
                        let! sent =
                            control.EvaluateAsync<string>(
                                "el => new Promise(resolve => { el.addEventListener('change', () => resolve([...new FormData(el.closest('form'))].join()), { once: true }); el.click(); el.click(); })"
                            )

                        Expect.equal sent "updates,on" "FormData in the change listener"

                        do! page.GetByText("Email me product updates").ClickAsync()
                        let! on = isChecked ()
                        Expect.isTrue on "clicking the label turns the switch on"
                        do! submit.ClickAsync()
                        do! Expect(result).ToHaveTextAsync("The form would send updates=on.")

                        do! control.FocusAsync()
                        do! page.Keyboard.PressAsync(" ")
                        let! on = isChecked ()
                        Expect.isFalse on "Space turns the switch off"

                        do! control.ClickAsync()
                        do! page.GetByRole(AriaRole.Button, PageGetByRoleOptions(Name = "Reset")).ClickAsync()
                        let! on = isChecked ()
                        Expect.isFalse on "Reset turns the switch off"

                        // ElementInternals' role and state are only in the browser's accessibility tree.
                        let! cdp = opened.Context.NewCDPSessionAsync(page)
                        let! tree = cdp.SendAsync("Accessibility.getFullAXTree")

                        let switches =
                            [
                                for node in tree.Value.GetProperty("nodes").EnumerateArray() do
                                    let mutable role = Unchecked.defaultof<Text.Json.JsonElement>
                                    let mutable name = Unchecked.defaultof<Text.Json.JsonElement>

                                    if
                                        node.TryGetProperty("role", &role)
                                        && role.GetProperty("value").GetString() = "switch"
                                        && node.TryGetProperty("name", &name)
                                    then
                                        name.GetProperty("value").GetString()
                            ]

                        Expect.equal switches [ "Email me product updates" ] "switches in the accessibility tree, by name"
                        noProblems opened
                    }
                )
    }

let private debouncedSearch =
    testTask "search as you type: one request for a word typed quickly" {
        do!
            withDemo
                "my-debounced-search"
                (fun opened search ->
                    task {
                        let box = search.GetByRole(AriaRole.Searchbox, LocatorGetByRoleOptions(Name = "Search fruit"))
                        do! box.PressSequentiallyAsync("berry", LocatorPressSequentiallyOptions(Delay = 50.0f))
                        do! Expect(search.GetByRole(AriaRole.Status)).ToHaveTextAsync("8 found. Requests so far: 1.")
                        do! Expect(search.Locator("li")).ToHaveCountAsync(8)
                        do! box.FillAsync("")
                        do! Expect(search.GetByRole(AriaRole.Status)).ToHaveTextAsync("Type to search 30 fruits.")
                        noProblems opened
                    }
                )
    }

let private fetchJson =
    testTask "fetch JSON: the books load, a missing file shows the error, and unticking loads them again" {
        do!
            withDemo
                "my-book-list"
                (fun opened books ->
                    task {
                        do! Expect(books.Locator("li")).ToHaveCountAsync(5)
                        do! books.GetByLabel("Ask for a file that isn't there").CheckAsync()

                        do!
                            Expect(books.GetByRole(AriaRole.Alert))
                                .ToHaveTextAsync("The books didn't load. The server answered 404.")

                        do! books.GetByLabel("Ask for a file that isn't there").UncheckAsync()
                        do! Expect(books.Locator("li")).ToHaveCountAsync(5)

                        let again = books.GetByRole(AriaRole.Button, LocatorGetByRoleOptions(Name = "Load again"))
                        do! again.FocusAsync()
                        do! again.PressAsync("Enter")
                        do! Expect(again).ToBeFocusedAsync()
                        do! Expect(books.Locator("li")).ToHaveCountAsync(5)

                        // Moved while loading: the request is aborted and made again.
                        let! _ =
                            books.EvaluateAsync(
                                "el => { el.shadowRoot.querySelector('button').click(); const p = el.parentNode; p.removeChild(el); p.appendChild(el); }"
                            )

                        do! opened.Page.WaitForTimeoutAsync(500.0f)
                        do! Expect(books.GetByRole(AriaRole.Alert)).ToHaveCountAsync(0)
                        do! Expect(books.Locator("li")).ToHaveCountAsync(5)
                    // The 404 is logged as a console error, on purpose, so no noProblems here.
                    }
                )
    }

let all =
    testList "Cookbook" [ signupForm; formSwitch; debouncedSearch; fetchJson ]
