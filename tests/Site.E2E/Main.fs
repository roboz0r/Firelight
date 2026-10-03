/// Browser checks for the built site. This doesn't build the site; from the repository root:
///
///   cd site && npm ci && npm run build && cd ..
///   dotnet build tests/Site.E2E
///   pwsh tests/Site.E2E/bin/Debug/net10.0/playwright.ps1 install chromium   (once per Playwright version)
///   dotnet run --project tests/Site.E2E --no-build
///
/// Expecto arguments go after `--`, e.g. `-- --filter "Site.Pages./Firelight/packages/firelight/"`.
module Site.E2E.Main

open Expecto

[<EntryPoint>]
let main argv =
    printfn $"Checking {Site.pages.Length} pages, found from {Site.source}."

    let result =
        try
            // Inside the try, so a failed browser launch still stops the server.
            Server.setup().GetAwaiter().GetResult()

            testList "Site" [ PageTests.all (); DemoTests.all; RatingTests.all; WeightTests.all (); AgentTests.all () ]
            |> runTestsWithCLIArgs [] argv
        finally
            Server.teardown().GetAwaiter().GetResult()

    match WeightTests.zeroJavaScriptPages () with
    | [] -> printfn "No page is checked for shipping zero JavaScript: every page has demos."
    | zero -> printfn $"Pages checked for shipping zero JavaScript: {zero.Length}."

    // Known accessibility violations don't fail the run, so list them every time.
    for known in PageTests.knownViolations do
        match PageTests.knownViolationsSeen.TryGetValue known.Rule with
        | true, pages ->
            printfn $"Known axe violation {known.Rule} on {pages.Length} pages, not failing the run: {known.Reason}"
        | _ -> ()

    // Once every page has been checked, an entry that matched nothing is fixed: remove it.
    let fixedViolations =
        PageTests.knownViolations
        |> List.filter (fun k -> not (PageTests.knownViolationsSeen.ContainsKey k.Rule))

    if PageTests.axeRuns = Site.pages.Length && not fixedViolations.IsEmpty then
        for known in fixedViolations do
            eprintfn $"Known axe violation {known.Rule} no longer occurs: remove it from PageTests.knownViolations."

        max result 1
    else
        result
