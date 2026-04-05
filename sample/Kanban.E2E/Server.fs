module Kanban.E2E.Server

open System
open System.Diagnostics
open System.IO
open System.Net.Http
open System.Threading.Tasks
open Microsoft.Playwright

/// Absolute path to the Kanban sample app directory.
let private kanbanDir = Path.Combine(__SOURCE_DIRECTORY__, "..", "Kanban")

let private baseUrl = "http://localhost:4173"

type TestContext =
    {
        Browser: IBrowser
        BaseUrl: string
    }

    interface IAsyncDisposable with
        member this.DisposeAsync() = ValueTask(this.Browser.CloseAsync())

/// On Windows, commands like npm/npx must be invoked via cmd.exe
/// when UseShellExecute is false.
let private startProcess (fileName: string) (args: string) (workingDir: string) =
    let isWindows =
        Runtime.InteropServices.RuntimeInformation.IsOSPlatform(Runtime.InteropServices.OSPlatform.Windows)

    let psi =
        if isWindows then
            ProcessStartInfo("cmd.exe", $"/c {fileName} {args}")
        else
            ProcessStartInfo(fileName, args)

    psi.WorkingDirectory <- workingDir
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    psi.CreateNoWindow <- true
    let proc = Process.Start(psi)

    if isNull proc then
        failwithf "Failed to start process: %s %s" fileName args

    proc

let private waitForServer (url: string) =
    task {
        use client = new HttpClient()
        client.Timeout <- TimeSpan.FromSeconds(2.0)
        let mutable ready = false
        let mutable attempts = 0

        while not ready && attempts < 30 do
            try
                let! resp = client.GetAsync(url)

                if resp.IsSuccessStatusCode then
                    ready <- true
            with _ ->
                attempts <- attempts + 1
                do! Task.Delay(500)

        if not ready then
            failwithf "Server at %s did not become ready after 15 seconds" url
    }

let private killProcessTree (proc: Process) =
    try
        if not proc.HasExited then
            proc.Kill(entireProcessTree = true)
            proc.WaitForExit(5000) |> ignore
    with _ ->
        ()

    proc.Dispose()

let mutable private viteProcess: Process option = None
let mutable private playwright: IPlaywright option = None

/// Build the Kanban app and start the Vite preview server.
/// Call this once before all tests.
let setup () =
    task {
        printfn "Building Kanban app..."
        let build = startProcess "npm" "run build" kanbanDir
        build.WaitForExit()

        if build.ExitCode <> 0 then
            let err = build.StandardError.ReadToEnd()
            failwithf "npm run build failed (exit %d):\n%s" build.ExitCode err

        build.Dispose()

        printfn "Starting Vite preview server..."
        let proc = startProcess "npx" "vite preview --port 4173 --strictPort" kanbanDir
        viteProcess <- Some proc

        do! waitForServer baseUrl
        printfn "Server ready at %s" baseUrl

        let! pw = Playwright.CreateAsync()
        playwright <- Some pw
    }

/// Create a new browser context for a test run.
let createContext () =
    task {
        let pw =
            match playwright with
            | Some pw -> pw
            | None -> failwith "Playwright not initialized — call Server.setup() first"

        let! browser = pw.Chromium.LaunchAsync(BrowserTypeLaunchOptions(Headless = true))
        return { Browser = browser; BaseUrl = baseUrl }
    }

/// Tear down the preview server and Playwright.
let teardown () =
    task {
        match playwright with
        | Some pw -> pw.Dispose()
        | None -> ()

        playwright <- None

        match viteProcess with
        | Some proc -> killProcessTree proc
        | None -> ()

        viteProcess <- None
    }
