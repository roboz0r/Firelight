/// Serves the built site (site/dist) with `vite preview`, which applies the site's own preview
/// middleware (directory URLs and deep links into the routing demo, as on GitHub Pages).
module Site.E2E.Server

open System
open System.Diagnostics
open System.IO
open System.Net.Http
open System.Threading.Tasks
open Microsoft.Playwright

let siteDir =
    Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "site"))

let distDir = Path.Combine(siteDir, "dist")

/// Not Vite's default preview port (4173), so this can run next to another preview.
let port = 4180
let origin = $"http://localhost:{port}"

/// Vite's `base`: every page is under it, as on GitHub Pages.
let basePath = "/Firelight/"

let url (path: string) = origin + path

/// Runs `node` directly rather than through npx: with npx's wrapper processes (cmd.exe on Windows)
/// in between, killing the process tree can leave the server running.
let private startProcess (fileName: string) (args: string) (workingDir: string) =
    let psi = ProcessStartInfo(fileName, args)
    psi.WorkingDirectory <- workingDir
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    psi.CreateNoWindow <- true

    let proc =
        match Process.Start psi with
        | null -> failwith $"Failed to start {fileName} {args}"
        | proc -> proc

    // Drain the output so the server never blocks on a full pipe; keep it for error messages.
    let output = Collections.Concurrent.ConcurrentQueue<string>()

    let keep (e: DataReceivedEventArgs) =
        if not (isNull e.Data) then
            output.Enqueue e.Data

    proc.OutputDataReceived.Add keep
    proc.ErrorDataReceived.Add keep
    proc.BeginOutputReadLine()
    proc.BeginErrorReadLine()
    proc, output

let private killProcessTree (proc: Process) =
    try
        if not proc.HasExited then
            proc.Kill(entireProcessTree = true)
            proc.WaitForExit(5000) |> ignore
    with _ ->
        ()

    proc.Dispose()

let mutable private server: Process option = None
let mutable private playwright: IPlaywright option = None
let mutable private browser: IBrowser option = None

/// The shared browser. Each test opens its own context, so tests don't share storage.
let browserInstance () =
    match browser with
    | Some b -> b
    | None -> failwith "The browser isn't running: call Server.setup () first."

let private waitForServer (proc: Process) (output: Collections.Concurrent.ConcurrentQueue<string>) =
    task {
        use client = new HttpClient(Timeout = TimeSpan.FromSeconds 2.0)
        let deadline = DateTime.UtcNow.AddSeconds 30.0
        let mutable ready = false

        while not ready do
            if proc.HasExited || DateTime.UtcNow > deadline then
                let log = String.Join("\n", output)
                failwith $"vite preview didn't start on {origin} (is the port in use?). Its output:\n{log}"

            try
                let! response = client.GetAsync(url basePath)
                ready <- response.IsSuccessStatusCode
            with _ ->
                ()

            if not ready then
                do! Task.Delay 250
    }

/// Starts `vite preview` on the existing build and launches Chromium.
let setup () =
    task {
        if not (File.Exists(Path.Combine(distDir, "index.html"))) then
            failwith $"No built site in {distDir}. Run `npm ci && npm run build` in site/ first."

        let vite = Path.Combine(siteDir, "node_modules", "vite", "bin", "vite.js")

        if not (File.Exists vite) then
            failwith $"Vite isn't installed in {siteDir}. Run `npm ci` there first."

        let proc, output =
            startProcess "node" $"\"{vite}\" preview --port {port} --strictPort" siteDir

        server <- Some proc
        do! waitForServer proc output

        let! pw = Playwright.CreateAsync()
        playwright <- Some pw
        let! b = pw.Chromium.LaunchAsync(BrowserTypeLaunchOptions(Headless = true))
        browser <- Some b
    }

let teardown () =
    task {
        match browser with
        | Some b -> do! b.CloseAsync()
        | None -> ()

        browser <- None
        playwright |> Option.iter _.Dispose()
        playwright <- None
        server |> Option.iter killProcessTree
        server <- None
    }
