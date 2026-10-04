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

let private loadMore =
    testTask "infinite scroll: scrolling to the end loads the next page, until all 50 are there" {
        do!
            withDemo
                "my-load-more"
                (fun opened list ->
                    task {
                        let status = list.GetByRole(AriaRole.Status)
                        do! Expect(status).ToHaveTextAsync("Showing 10 of 50.")
                        let scroller = list.Locator(".scroller")

                        // From the keyboard: the button loads a page and keeps the focus.
                        let button = list.GetByRole(AriaRole.Button)
                        do! button.FocusAsync()
                        do! button.PressAsync("Enter")
                        do! Expect(status).ToHaveTextAsync("Showing 20 of 50.")
                        do! Expect(button).ToBeFocusedAsync()
                        do! Expect(button).ToBeInViewportAsync()

                        let scrollToEnd () =
                            task {
                                let! _ = scroller.EvaluateAsync("el => el.scrollTop = el.scrollHeight")
                                ()
                            }

                        do! scrollToEnd ()
                        do! Expect(status).ToHaveTextAsync("Showing 30 of 50.")
                        let mutable tries = 0
                        let! text = status.TextContentAsync()
                        let mutable current = text

                        while current <> "Showing 50 of 50." && tries < 20 do
                            do! scrollToEnd ()
                            do! opened.Page.WaitForTimeoutAsync(500.0f)
                            let! text = status.TextContentAsync()
                            current <- text
                            tries <- tries + 1

                        do! Expect(status).ToHaveTextAsync("Showing 50 of 50.")
                        do! Expect(list.Locator("li")).ToHaveCountAsync(50)
                        do! Expect(button).ToHaveTextAsync("All 50 loaded")
                        do! Expect(button).ToHaveAttributeAsync("aria-disabled", "true")
                        noProblems opened
                    }
                )
    }

let private packingList =
    testTask "remember state: the packing list survives a reload, and Start again resets it" {
        do!
            withDemo
                "my-packing-list"
                (fun opened list ->
                    task {
                        do! Expect(list.Locator("li")).ToHaveCountAsync(2)
                        do! list.GetByLabel("New item").FillAsync("Socks")
                        do! list.GetByRole(AriaRole.Button, LocatorGetByRoleOptions(Name = "Add")).ClickAsync()
                        do! list.GetByLabel("Passport", LocatorGetByLabelOptions(Exact = true)).CheckAsync()
                        let! _ = opened.Page.ReloadAsync()
                        do! Expect(list.Locator("li")).ToHaveCountAsync(3)
                        do! Expect(list.GetByLabel("Passport", LocatorGetByLabelOptions(Exact = true))).ToBeCheckedAsync()
                        do! Expect(list.GetByLabel("Socks", LocatorGetByLabelOptions(Exact = true))).Not.ToBeCheckedAsync()
                        do! list.GetByRole(AriaRole.Button, LocatorGetByRoleOptions(Name = "Remove Charger")).ClickAsync()
                        do! Expect(list.Locator("li")).ToHaveCountAsync(2)
                        do! list.GetByRole(AriaRole.Button, LocatorGetByRoleOptions(Name = "Start again")).ClickAsync()
                        let! _ = opened.Page.ReloadAsync()
                        do! Expect(list.Locator("li")).ToHaveCountAsync(2)
                        do! Expect(list.GetByLabel("Passport", LocatorGetByLabelOptions(Exact = true))).Not.ToBeCheckedAsync()
                        noProblems opened
                    }
                )
    }

let private themePicker =
    testTask "themes: Dark darkens the preview and is still chosen after a reload" {
        do!
            withDemo
                "my-theme-picker"
                (fun opened picker ->
                    task {
                        do! opened.Page.EmulateMediaAsync(PageEmulateMediaOptions(ColorScheme = ColorScheme.Light))
                        let preview = picker.Locator(".preview")
                        let background () = preview.EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor")
                        let! light = background ()
                        Expect.equal light "rgb(255, 253, 249)" "System, on a light device"
                        do! picker.GetByLabel("Dark").CheckAsync()
                        let! dark = background ()
                        Expect.equal dark "rgb(23, 21, 26)" "Dark"
                        let! _ = opened.Page.ReloadAsync()
                        do! Expect(picker.GetByLabel("Dark")).ToBeCheckedAsync()
                        let! dark = background ()
                        Expect.equal dark "rgb(23, 21, 26)" "Dark, after a reload"
                        do! picker.GetByLabel("System").CheckAsync()
                        noProblems opened
                    }
                )
    }

let private slotCards =
    testTask "slots: a filled footer shows, an empty one hides, and the heading falls back" {
        do!
            withDemo
                "my-slot-card"
                (fun opened first ->
                    task {
                        let second = opened.Page.Locator("my-slot-card").Nth(1)
                        do! Expect(first.Locator("footer")).ToBeVisibleAsync()
                        do! Expect(first.GetByRole(AriaRole.Button, LocatorGetByRoleOptions(Name = "Open"))).ToBeVisibleAsync()
                        do! Expect(second.Locator("footer")).ToBeHiddenAsync()
                        do! Expect(second.Locator("header")).ToHaveTextAsync("Untitled")
                        noProblems opened
                    }
                )
    }

let private dialog =
    testTask "dialog: opens with focus on the safe button, Escape cancels, Delete answers, focus returns" {
        do!
            withDemo
                "my-confirm-delete"
                (fun opened demo ->
                    task {
                        let page = opened.Page
                        let opener = demo.GetByRole(AriaRole.Button, LocatorGetByRoleOptions(Name = "Delete report.pdf…"))
                        let dialog = demo.GetByRole(AriaRole.Dialog, LocatorGetByRoleOptions(Name = "Delete report.pdf?"))
                        let status = demo.GetByRole(AriaRole.Status)

                        do! opener.ClickAsync()
                        do! Expect(dialog).ToBeVisibleAsync()
                        do! Expect(dialog.GetByRole(AriaRole.Button, LocatorGetByRoleOptions(Name = "Keep it"))).ToBeFocusedAsync()
                        do! dialog.GetByRole(AriaRole.Button, LocatorGetByRoleOptions(Name = "Delete")).ClickAsync()
                        do! Expect(dialog).ToBeHiddenAsync()
                        do! Expect(status).ToHaveTextAsync("Deleted report.pdf.")
                        do! Expect(opener).ToBeFocusedAsync()

                        do! opener.PressAsync("Enter")
                        do! Expect(dialog).ToBeVisibleAsync()
                        do! page.Keyboard.PressAsync("Escape")
                        do! Expect(dialog).ToBeHiddenAsync()
                        do! Expect(status).ToHaveTextAsync("Kept report.pdf.")
                        do! Expect(opener).ToBeFocusedAsync()
                        noProblems opened
                    }
                )
    }

let private tabs =
    testTask "tabs: arrow keys, Home and End move and select, and a panel keeps its text" {
        do!
            withDemo
                "my-settings-tabs"
                (fun opened demo ->
                    task {
                        let page = opened.Page
                        let tab (name: string) = demo.GetByRole(AriaRole.Tab, LocatorGetByRoleOptions(Name = name))
                        let panel = demo.GetByRole(AriaRole.Tabpanel)

                        do! Expect(tab "General").ToHaveAttributeAsync("aria-selected", "true")
                        do! Expect(panel).ToHaveAccessibleNameAsync("General")
                        do! tab("General").FocusAsync()
                        do! page.Keyboard.PressAsync("ArrowRight")
                        do! Expect(tab "Notes").ToBeFocusedAsync()
                        do! Expect(tab "Notes").ToHaveAttributeAsync("aria-selected", "true")
                        do! Expect(tab "General").ToHaveAttributeAsync("tabindex", "-1")
                        do! panel.GetByLabel("Notes").FillAsync("Pack the charger")
                        do! tab("Notes").FocusAsync()
                        do! page.Keyboard.PressAsync("End")
                        do! Expect(tab "About").ToBeFocusedAsync()
                        do! Expect(panel).ToHaveTextAsync("Settings, version 1.0.")
                        do! page.Keyboard.PressAsync("ArrowRight")
                        do! Expect(tab "General").ToBeFocusedAsync()
                        do! page.Keyboard.PressAsync("ArrowLeft")
                        do! page.Keyboard.PressAsync("ArrowLeft")
                        do! Expect(tab "Notes").ToBeFocusedAsync()
                        do! Expect(panel.GetByLabel("Notes")).ToHaveValueAsync("Pack the charger")
                        do! page.Keyboard.PressAsync("Home")
                        do! Expect(tab "General").ToBeFocusedAsync()
                        noProblems opened
                    }
                )
    }

let private toasts =
    testTask "toasts: raised by a component and by page JavaScript, dismissed by hand and on a timer" {
        do!
            withDemo
                "my-toaster"
                (fun opened toaster ->
                    task {
                        let page = opened.Page
                        let toastsShown = toaster.Locator(".toast")
                        do! page.Locator("my-save-button").GetByRole(AriaRole.Button, LocatorGetByRoleOptions(Name = "Save")).ClickAsync()
                        do! page.GetByRole(AriaRole.Button, PageGetByRoleOptions(Name = "Raise one from JavaScript")).ClickAsync()
                        do! Expect(toastsShown).ToHaveCountAsync(2)
                        do! Expect(toaster.GetByRole(AriaRole.Status)).ToContainTextAsync("Saved.")
                        do! Expect(toastsShown.Nth(1)).ToContainTextAsync("Hello from plain JavaScript.")
                        do! Expect(toaster.GetByRole(AriaRole.Status)).ToHaveAttributeAsync("aria-atomic", "false")

                        // Dismissed from the keyboard: the focus moves to the next toast's button,
                        // then back to where it came from.
                        let source = page.GetByRole(AriaRole.Button, PageGetByRoleOptions(Name = "Raise one from JavaScript"))
                        do! source.FocusAsync()
                        do! page.Keyboard.PressAsync("Tab")
                        let first = toastsShown.Nth(0).GetByRole(AriaRole.Button, LocatorGetByRoleOptions(Name = "Dismiss"))
                        do! Expect(first).ToBeFocusedAsync()
                        do! page.Keyboard.PressAsync("Enter")
                        do! Expect(toastsShown).ToHaveCountAsync(1)
                        let remaining = toastsShown.Nth(0).GetByRole(AriaRole.Button, LocatorGetByRoleOptions(Name = "Dismiss"))
                        do! Expect(remaining).ToBeFocusedAsync()

                        // While the focus is on it, the toast outlives its five seconds.
                        do! page.WaitForTimeoutAsync(6000.0f)
                        do! Expect(toastsShown).ToHaveCountAsync(1)
                        do! page.Keyboard.PressAsync("Enter")
                        do! Expect(toastsShown).ToHaveCountAsync(0)
                        do! Expect(source).ToBeFocusedAsync()

                        do! page.Locator("my-save-button").GetByRole(AriaRole.Button, LocatorGetByRoleOptions(Name = "Save")).ClickAsync()
                        do! Expect(toastsShown).ToHaveCountAsync(1)
                        do! Expect(toastsShown).ToHaveCountAsync(0, LocatorAssertionsToHaveCountOptions(Timeout = 7000.0f))
                        noProblems opened
                    }
                )
    }

let private shortcuts =
    testTask "keyboard shortcuts: / focuses the search box, ? toggles the help, but not while typing" {
        do!
            withDemo
                "my-shortcut-demo"
                (fun opened demo ->
                    task {
                        let page = opened.Page
                        let search = demo.GetByRole(AriaRole.Searchbox, LocatorGetByRoleOptions(Name = "Search"))
                        let help = demo.Locator("dl")
                        do! page.Locator("main h1").ClickAsync()
                        do! page.Keyboard.PressAsync("?")
                        do! Expect(help).ToBeVisibleAsync()
                        do! page.Keyboard.PressAsync("/")
                        do! Expect(search).ToBeFocusedAsync()
                        do! page.Keyboard.TypeAsync("a?/")
                        do! Expect(search).ToHaveValueAsync("a?/")
                        do! Expect(help).ToBeVisibleAsync()
                        do! page.Locator("main h1").ClickAsync()
                        do! page.Keyboard.PressAsync("?")
                        do! Expect(help).ToBeHiddenAsync()

                        // A held key's repeats don't toggle it back and forth.
                        let! _ =
                            page.EvaluateAsync(
                                "() => ['?', '?', '?'].forEach((key, i) => document.body.dispatchEvent(new KeyboardEvent('keydown', { key, repeat: i > 0, bubbles: true, composed: true })))"
                            )

                        do! Expect(help).ToBeVisibleAsync()

                        // Turned off, the keys do nothing.
                        do! demo.GetByLabel("Single-key shortcuts").UncheckAsync()
                        do! page.Locator("main h1").ClickAsync()
                        do! page.Keyboard.PressAsync("?")
                        do! Expect(help).ToBeVisibleAsync()
                        do! page.Keyboard.PressAsync("/")
                        do! Expect(search).Not.ToBeFocusedAsync()
                        do! Expect(search).Not.ToHaveAttributeAsync("aria-keyshortcuts", "/")
                        // Removed from the page, the component stops listening.
                        let! _ = demo.EvaluateAsync("el => el.remove()")
                        do! page.Keyboard.PressAsync("/")
                        noProblems opened
                    }
                )
    }

let private dragReorder =
    testTask "drag to reorder: dragging moves a step, and the buttons move it and keep the focus" {
        do!
            withDemo
                "my-reorder-list"
                (fun opened list ->
                    task {
                        let page = opened.Page
                        let steps = list.Locator("li .text")
                        let status = list.GetByRole(AriaRole.Status)

                        do!
                            Expect(steps)
                                .ToHaveTextAsync([| "Wake up"; "Make coffee"; "Read the news"; "Walk the dog"; "Start work" |])

                        do! list.Locator("li").Nth(3).DragToAsync(list.Locator("li").Nth(0))

                        do!
                            Expect(steps)
                                .ToHaveTextAsync([| "Walk the dog"; "Wake up"; "Make coffee"; "Read the news"; "Start work" |])

                        do! Expect(status).ToHaveTextAsync("Walk the dog moved to position 1 of 5.")

                        let down = list.GetByRole(AriaRole.Button, LocatorGetByRoleOptions(Name = "Move Walk the dog down"))
                        do! down.FocusAsync()
                        do! page.Keyboard.PressAsync("Enter")
                        do! page.Keyboard.PressAsync("Enter")

                        do!
                            Expect(steps)
                                .ToHaveTextAsync([| "Wake up"; "Make coffee"; "Walk the dog"; "Read the news"; "Start work" |])

                        do! Expect(down).ToBeFocusedAsync()
                        do! Expect(status).ToHaveTextAsync("Walk the dog moved to position 3 of 5.")

                        let up = list.GetByRole(AriaRole.Button, LocatorGetByRoleOptions(Name = "Move Wake up up"))
                        do! Expect(up).ToHaveAttributeAsync("aria-disabled", "true")
                        do! up.ClickAsync(LocatorClickOptions(Force = true))
                        do! Expect(steps.First).ToHaveTextAsync("Wake up")
                        noProblems opened
                    }
                )
    }

let private animatedList =
    testTask "animating list changes: adding and removing work, and the focus moves on after a removal" {
        do!
            withDemo
                "my-animated-list"
                (fun opened list ->
                    task {
                        let page = opened.Page
                        let people = list.Locator("li")
                        do! Expect(people).ToHaveCountAsync(3)
                        do! list.GetByRole(AriaRole.Button, LocatorGetByRoleOptions(Name = "Add someone")).ClickAsync()
                        do! Expect(people).ToHaveCountAsync(4)
                        do! Expect(people.First).ToContainTextAsync("Barbara")

                        let remove = list.GetByRole(AriaRole.Button, LocatorGetByRoleOptions(Name = "Remove Ada"))
                        do! remove.FocusAsync()
                        do! page.Keyboard.PressAsync("Enter")
                        // The removed item stays while it fades out, then goes.
                        do! Expect(people).ToHaveCountAsync(3)
                        do! Expect(list.GetByRole(AriaRole.Button, LocatorGetByRoleOptions(Name = "Remove Grace"))).ToBeFocusedAsync()
                        noProblems opened
                    }
                )
    }

let private reducedMotion =
    testTask "animating list changes: turning on reduced motion stops the animations" {
        do!
            withDemo
                "my-animated-list"
                (fun opened list ->
                    task {
                        let add = list.GetByRole(AriaRole.Button, LocatorGetByRoleOptions(Name = "Add someone"))
                        let running () = list.EvaluateAsync<int>("el => el.shadowRoot.getAnimations({ subtree: true }).length")
                        do! add.ClickAsync()
                        let! animating = running ()
                        Expect.isGreaterThan animating 0 "animations after Add"
                        do! opened.Page.EmulateMediaAsync(PageEmulateMediaOptions(ReducedMotion = ReducedMotion.Reduce))
                        do! opened.Page.WaitForTimeoutAsync(100.0f)
                        do! add.ClickAsync()
                        let! animating = running ()
                        Expect.equal animating 0 "animations after Add, with reduced motion"
                        noProblems opened
                    }
                )
    }

let all =
    testList "Cookbook" [ signupForm; formSwitch; debouncedSearch; fetchJson; loadMore; packingList; themePicker; slotCards; dialog; tabs; toasts; shortcuts; dragReorder; animatedList; reducedMotion ]
