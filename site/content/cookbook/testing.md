---
title: Testing components
tagline: plain F# tests for the logic, Playwright for the component
description: "Test Firelight components: the logic in plain F# with Expecto, with no browser, and the rendered component in Chromium with Playwright for .NET, finding elements inside shadow roots by role and label."
section: cookbook
order: 150
summary: "Test the logic in plain F#, and the component in a browser with Playwright"
lead: "Test a component's logic as plain F#, with no browser, and the component itself in a real browser with Playwright, from the same .NET test project style."
links:
  - text: "Playwright for .NET"
    href: https://playwright.dev/dotnet/
  - text: Expecto
    href: https://github.com/haf/expecto
---

This recipe has no demo: its code is tests. They test the [Form validation](/cookbook/form-validation/)
recipe's sign-up form. This site runs a longer version of the browser test on that demo, in
[`tests/Site.E2E`](https://github.com/roboz0r/Firelight/tree/main/tests/Site.E2E), along with one
for every other recipe here.

## The logic, without a browser

Keep the decisions in functions that don't touch Lit or the DOM, in a file of their own: the
model, `update`, `validate`. They're ordinary F#, so they compile for .NET as well as for
[Fable](https://fable.io/), and a test runs them in milliseconds:

```fsharp fragment
module Signup

open System.Text.RegularExpressions

type Signup = { Name: string; Email: string }

let validate (form: Signup) =
    [
        if form.Name.Trim() = "" then
            "name", "Enter your name."
        if not (Regex.IsMatch(form.Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) then
            "email", "Enter an email address, such as ada@example.com."
    ]
```

```fsharp fragment
open Expecto
open Signup

let tests =
    testList "validate" [
        test "an empty form has both errors, in order" {
            let errors = validate { Name = ""; Email = "" }
            Expect.equal (List.map fst errors) [ "name"; "email" ] "fields with errors"
        }
        test "an address needs a domain" {
            let errors = validate { Name = "Ada"; Email = "ada@" }
            Expect.equal (List.map fst errors) [ "email" ] "fields with errors"
        }
    ]
```

The component's file then opens `Signup` and does only the rendering. The
[Kanban sample's tests](https://github.com/roboz0r/Firelight/tree/main/sample/Kanban.Tests) test
its Elmish `update` this way, with Pyxpecto, which runs the same tests under .NET and, compiled by
Fable, in Node.

## The component, in a browser

What the user sees, the focus, and the accessible names need a browser. Playwright for .NET drives
Chromium, Firefox or WebKit from an F# test:

```sh
dotnet add package Microsoft.Playwright
dotnet build
pwsh bin/Debug/net10.0/playwright.ps1 install chromium
```

Serve the built app, as `vite preview` does, and point the test at it:

```fsharp fragment
open Expecto
open Microsoft.Playwright
open type Microsoft.Playwright.Assertions

let signupForm =
    testTask "the sign-up form shows its errors, focuses the first, then thanks" {
        let! (playwright: IPlaywright) = Playwright.CreateAsync()

        try
            use! (browser: IBrowser) = playwright.Chromium.LaunchAsync()
            let! (page: IPage) = browser.NewPageAsync()
            let! _ = page.GotoAsync "http://localhost:4173/"

            let form = page.Locator "my-signup-form"
            let name = form.GetByLabel "Name"
            let signUp = form.GetByRole(AriaRole.Button, LocatorGetByRoleOptions(Name = "Sign up"))

            do! signUp.ClickAsync()
            do! Expect(name).ToBeFocusedAsync()
            do! Expect(name).ToHaveAccessibleDescriptionAsync "Enter your name."
            do! name.FillAsync "Ada"
            do! form.GetByLabel("Email").FillAsync "ada@example.com"
            do! signUp.ClickAsync()
            do! Expect(form.GetByRole AriaRole.Status).ToHaveTextAsync "Thanks, Ada. Check your inbox."
        finally
            playwright.Dispose()
    }
```

- **Shadow DOM.** Playwright's locators look inside open shadow roots, so `GetByLabel "Name"` finds
  the `<input>` in the component's shadow root. Lit's shadow roots are open unless you say
  otherwise. XPath locators don't look inside.
- **Find things as the user does.** `GetByRole` and `GetByLabel` find elements by their role and
  accessible name, so the test also checks that the button has a name and the field a label.
  `ToHaveAccessibleDescriptionAsync` checks the `aria-describedby` link.
- **No sleeps.** Lit renders asynchronously, after the click. Every `Expect` retries until it
  passes or times out, five seconds by default, so the test waits exactly as long as the render
  takes.
- **More checks for free.** In the same test, listen to `page.Console` for errors, and run axe with
  `Deque.AxeCore.Playwright`'s `page.RunAxe ()` for accessibility rules. This site's tests do both
  on every page.

`use!` closes the browser when the test ends, whether it passes or fails, and `finally` stops
Playwright's driver. Expecto's `testTask` takes `use` and `use!` only for `IAsyncDisposable`,
which the browser is and Playwright isn't. In a suite, start Playwright and the browser once and share them,
as `Site.E2E` does, and give each test a new page.

## Related

- [Prerendering components at build time](/guides/prerendering/), which has its own test: with
  JavaScript off, a prerendered component should still show its first render.
