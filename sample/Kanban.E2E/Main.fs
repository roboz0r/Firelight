module Kanban.E2E.Main

open Expecto

[<EntryPoint>]
let main argv =
    // Setup: build app and start server
    Server.setup().GetAwaiter().GetResult()

    try
        // Run all E2E tests
        runTestsWithCLIArgs [] argv KanbanTests.all
    finally
        // Teardown: stop server and Playwright
        Server.teardown().GetAwaiter().GetResult()
