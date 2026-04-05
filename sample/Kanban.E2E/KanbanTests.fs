module Kanban.E2E.KanbanTests

open Expecto
open Microsoft.Playwright

let private newPage (ctx: Server.TestContext) =
    task {
        let! page = ctx.Browser.NewPageAsync()
        let! _ = page.GotoAsync(ctx.BaseUrl)
        // Wait for the custom element to render
        let! _ =
            page.WaitForSelectorAsync("kanban-app", PageWaitForSelectorOptions(State = WaitForSelectorState.Attached))
        // Give Lit a moment to render shadow DOM content
        do! page.WaitForTimeoutAsync(500.0f)
        return page
    }

/// Run a test with a fresh browser + page, cleaning up afterward.
let private withPage (f: IPage -> System.Threading.Tasks.Task<unit>) =
    task {
        let! ctx = Server.createContext ()

        try
            let! page = newPage ctx
            do! f page
        finally
            ctx.Browser.CloseAsync().GetAwaiter().GetResult()
    }

[<Tests>]
let pageLoadTests =
    testList "Page load" [
        testTask "page has correct title" {
            do!
                withPage (fun (page: IPage) ->
                    task {
                        let! title = page.TitleAsync()
                        Expect.equal title "Kanban Board" "Page title should be 'Kanban Board'"
                    }
                )
        }

        testTask "header renders with app name" {
            do!
                withPage (fun (page: IPage) ->
                    task {
                        let header = page.Locator("h1")
                        let! text = header.TextContentAsync()
                        Expect.equal (text.Trim()) "Kanban Board" "Header should say 'Kanban Board'"
                    }
                )
        }
    ]

[<Tests>]
let columnTests =
    testList "Columns" [
        testTask "renders three default columns" {
            do!
                withPage (fun (page: IPage) ->
                    task {
                        let columns = page.Locator("h2")
                        let! count = columns.CountAsync()
                        Expect.equal count 3 "Should have 3 column headers"
                    }
                )
        }

        testTask "columns have correct titles" {
            do!
                withPage (fun (page: IPage) ->
                    task {
                        let! titles = page.Locator("h2").AllTextContentsAsync()
                        let titles = titles |> Seq.map (fun s -> s.Trim()) |> Seq.toList
                        Expect.equal titles [ "To Do"; "In Progress"; "Done" ] "Column titles should match defaults"
                    }
                )
        }

        testTask "add column button is visible" {
            do!
                withPage (fun (page: IPage) ->
                    task {
                        let addBtn = page.GetByText("+ Add column")
                        let! visible = addBtn.IsVisibleAsync()
                        Expect.isTrue visible "Add column button should be visible"
                    }
                )
        }

        testTask "can add a new column" {
            do!
                withPage (fun (page: IPage) ->
                    task {
                        do! page.GetByText("+ Add column").ClickAsync()

                        let input = page.GetByPlaceholder("Column title...")
                        do! input.FillAsync("Review")
                        do! input.PressAsync("Enter")

                        let! titles = page.Locator("h2").AllTextContentsAsync()
                        let titles = titles |> Seq.map (fun s -> s.Trim()) |> Seq.toList
                        Expect.contains titles "Review" "New column should appear"
                        Expect.equal titles.Length 4 "Should now have 4 columns"
                    }
                )
        }
    ]

[<Tests>]
let cardTests =
    testList "Cards" [
        testTask "renders sample cards" {
            do!
                withPage (fun (page: IPage) ->
                    task {
                        let cards = page.Locator("[data-card-id]")
                        let! count = cards.CountAsync()
                        Expect.equal count 4 "Should have 4 sample cards"
                    }
                )
        }

        testTask "card shows title and priority badge" {
            do!
                withPage (fun (page: IPage) ->
                    task {
                        let card = page.Locator("[data-card-id='sample-1']")
                        let! title = card.Locator("h3").TextContentAsync()
                        Expect.equal (title.Trim()) "Set up project structure" "Card title should match"

                        let badge = card.Locator("span")
                        let! badgeText = badge.TextContentAsync()
                        Expect.equal (badgeText.Trim()) "High" "Priority badge should say High"
                    }
                )
        }

        testTask "can add a card to a column" {
            do!
                withPage (fun (page: IPage) ->
                    task {
                        let addButtons = page.GetByText("+ Add card")
                        do! addButtons.First.ClickAsync()

                        let input = page.GetByPlaceholder("Card title...")
                        do! input.FillAsync("New test card")
                        do! input.PressAsync("Enter")

                        let! visible = page.GetByText("New test card").IsVisibleAsync()
                        Expect.isTrue visible "New card should be visible"

                        let cards = page.Locator("[data-card-id]")
                        let! count = cards.CountAsync()
                        Expect.equal count 5 "Should now have 5 cards"
                    }
                )
        }

        testTask "can delete a card" {
            do!
                withPage (fun (page: IPage) ->
                    task {
                        let! initialCount = page.Locator("[data-card-id]").CountAsync()

                        let delButtons = page.GetByText("Del", PageGetByTextOptions(Exact = true))
                        do! delButtons.First.ClickAsync()

                        do! page.WaitForTimeoutAsync(300.0f)
                        let! newCount = page.Locator("[data-card-id]").CountAsync()
                        Expect.equal newCount (initialCount - 1) "Should have one fewer card"
                    }
                )
        }
    ]

[<Tests>]
let editModalTests =
    testList "Edit modal" [
        testTask "clicking Edit opens modal" {
            do!
                withPage (fun (page: IPage) ->
                    task {
                        let editButtons = page.GetByText("Edit", PageGetByTextOptions(Exact = true))
                        do! editButtons.First.ClickAsync()

                        let! visible = page.GetByText("Edit Card").IsVisibleAsync()
                        Expect.isTrue visible "Edit modal should be visible"
                    }
                )
        }

        testTask "can edit card title and save" {
            do!
                withPage (fun (page: IPage) ->
                    task {
                        let card = page.Locator("[data-card-id='sample-3']")
                        do! card.GetByText("Edit", LocatorGetByTextOptions(Exact = true)).ClickAsync()

                        let titleInput = page.Locator("input[placeholder='Title']")
                        do! titleInput.ClearAsync()
                        do! titleInput.FillAsync("Updated card title")

                        do! page.GetByText("Save", PageGetByTextOptions(Exact = true)).ClickAsync()

                        do! page.WaitForTimeoutAsync(300.0f)
                        let! visible = page.GetByText("Updated card title").IsVisibleAsync()
                        Expect.isTrue visible "Card should show updated title"
                    }
                )
        }

        testTask "cancel closes modal without saving" {
            do!
                withPage (fun (page: IPage) ->
                    task {
                        let editButtons = page.GetByText("Edit", PageGetByTextOptions(Exact = true))
                        do! editButtons.First.ClickAsync()

                        let titleInput = page.Locator("input[placeholder='Title']")
                        let! originalTitle = titleInput.InputValueAsync()

                        do! titleInput.ClearAsync()
                        do! titleInput.FillAsync("Should not save")
                        do! page.GetByText("Cancel").Last.ClickAsync()

                        do! page.WaitForTimeoutAsync(300.0f)
                        let! shouldNotExist = page.GetByText("Should not save").IsVisibleAsync()
                        Expect.isFalse shouldNotExist "Cancelled changes should not appear"
                        let! stillThere = page.GetByText(originalTitle).IsVisibleAsync()
                        Expect.isTrue stillThere "Original title should still be visible"
                    }
                )
        }

        testTask "can change priority in edit modal" {
            do!
                withPage (fun (page: IPage) ->
                    task {
                        let card = page.Locator("[data-card-id='sample-3']")
                        do! card.GetByText("Edit", LocatorGetByTextOptions(Exact = true)).ClickAsync()

                        let modal = page.Locator(".fixed")
                        do! modal.GetByText("High", LocatorGetByTextOptions(Exact = true)).ClickAsync()

                        do! page.GetByText("Save", PageGetByTextOptions(Exact = true)).ClickAsync()
                        do! page.WaitForTimeoutAsync(300.0f)

                        let updatedCard = page.Locator("[data-card-id='sample-3']")
                        let badge = updatedCard.Locator("span")
                        let! badgeText = badge.TextContentAsync()
                        Expect.equal (badgeText.Trim()) "High" "Priority should now be High"
                    }
                )
        }
    ]

let all =
    testList "Kanban E2E" [ pageLoadTests; columnTests; cardTests; editModalTests ]
