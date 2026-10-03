/// The search page's search box and results: `<fl-search>`. The index is Pagefind's, built over
/// dist/ by `npm run build:search`; its JavaScript loads with the first search, so the page ships
/// only this component until then. `?q=` in the address searches straight away.
module Site.Components.Search

open Fable.Core
open Fable.Core.JsInterop
open Browser
open Browser.Types
open Firelight
open type Firelight.Lit

// The parts of Pagefind's JavaScript API used here (https://pagefind.app/docs/api/).
type private ResultData =
    abstract url: string
    abstract excerpt: string
    abstract meta: {| title: string |}

type private Result =
    abstract data: unit -> JS.Promise<ResultData>

type private Results =
    abstract results: Result[]

type private Pagefind =
    abstract options: obj -> JS.Promise<unit>
    abstract search: string -> JS.Promise<Results>

[<Emit("import(/* @vite-ignore */ $0)")>]
let private importUrl (url: string) : JS.Promise<Pagefind> = jsNative

// Vite's base path. Read only in the browser: when the page is prerendered, Node has no import.meta.env.
let private basePath () : string =
    emitJsExpr () "import.meta.env.BASE_URL"

// The address's ?q=, and the address with `query` as its ?q= (none if it's blank).
[<Emit("new URLSearchParams(location.search).get('q') ?? ''")>]
let private queryInAddress () : string = jsNative

[<Emit("(u => { $0.trim() ? u.searchParams.set('q', $0) : u.searchParams.delete('q'); return u.href; })(new URL(location.href))")>]
let private addressWith (query: string) : string = jsNative

// Pagefind, loaded once, by the first search.
let mutable private pagefind: Async<Pagefind> option = None

let private loadPagefind () =
    match pagefind with
    | Some loading -> loading
    | None ->
        let loading =
            async {
                let! loaded = importUrl (basePath () + "pagefind/pagefind.js") |> Async.AwaitPromise
                do! loaded.options {| baseUrl = basePath () |} |> Async.AwaitPromise
                return loaded
            }
            |> Async.StartAsPromise
            |> Async.AwaitPromise

        pagefind <- Some loading
        loading

type private Hit =
    {
        Url: string
        Title: string
        /// Pagefind's excerpt: escaped text, with the matches in `<mark>`.
        Excerpt: string
    }

type private State =
    | Waiting
    | Searching
    | Found of Hit list
    /// No index: `npm run dev`, or a build without `npm run build:search`.
    | NoIndex

[<AttachMembers>]
type Search() =
    inherit LitElement()

    let mutable query = ""
    let mutable state = Waiting
    // Results only count for the latest search.
    let mutable latest = 0

    static member styles =
        css
            $$"""
        :host { display: block; }
        label { display: block; margin-bottom: 0.5rem; font-weight: 600; }
        input {
            box-sizing: border-box; width: 100%; padding: 0.6rem 0.8rem;
            font: inherit; color: inherit; background: var(--bg);
            border: 1px solid var(--border); border-radius: var(--radius);
        }
        input:focus-visible { outline: 2px solid var(--accent); outline-offset: 1px; }
        .status { color: var(--muted); }
        ol { margin: 0; padding: 0; list-style: none; }
        li { padding: 0.75rem 0; border-bottom: 1px solid var(--border); }
        li a { font-weight: 600; color: var(--accent); }
        li p { margin: 0.25rem 0 0; color: var(--muted); }
        mark { background: none; color: var(--fg); font-weight: 600; }
        """

    member private this.Search(text: string) =
        query <- text
        latest <- latest + 1
        let request = latest

        if text.Trim() = "" then
            state <- Waiting
            this.requestUpdate ()
        else
            state <- Searching
            this.requestUpdate ()

            async {
                try
                    let! pagefind = loadPagefind ()
                    let! results = pagefind.search text |> Async.AwaitPromise

                    // Every match: the site is small enough to list them all.
                    let! data =
                        results.results
                        |> Array.map (fun r -> r.data () |> Async.AwaitPromise)
                        |> Async.Parallel

                    if request = latest then
                        state <-
                            Found [
                                for d in data ->
                                    {
                                        Url = d.url
                                        Title = d.meta.title
                                        Excerpt = d.excerpt
                                    }
                            ]
                with _ ->
                    pagefind <- None

                    if request = latest then
                        state <- NoIndex

                if request = latest then
                    this.requestUpdate ()
            }
            |> Async.StartImmediate

    member private this.OnInput(e: Event) =
        let text = (e.target :?> HTMLInputElement).value
        // The address keeps the search, so it can be shared or come back to.
        window.history.replaceState (null, "", addressWith text)
        this.Search text

    // After the first render, which matches the prerendered one. (In a new task, as an update
    // requested during this one is wasted work, which Lit warns about.)
    override this.firstUpdated(_) =
        match queryInAddress () with
        | "" -> ()
        | text -> JS.setTimeout (fun () -> this.Search text) 0 |> ignore

    override this.render() =
        // `nothing` rather than "" before the first search: hydrating a prerendered empty string
        // leaves no text node, and Lit would then write the next status into the part's end marker.
        let status: obj =
            match state with
            | Waiting -> Lit.nothing
            | Searching -> "Searching…"
            | Found [] -> $"Nothing found for “{query.Trim()}”."
            | Found [ _ ] -> "1 page found."
            | Found hits -> $"{hits.Length} pages found."
            | NoIndex -> "Search is available in the built site: npm run build, then npm run preview."

        let hits =
            match state with
            | Found hits ->
                hits
                |> List.map (fun hit ->
                    html $"""<li><a href={hit.Url}>{hit.Title}</a><p>{unsafeHTML hit.Excerpt}</p></li>"""
                )
            | _ -> []

        html
            $"""
        <form role="search" @submit={fun (e: Event) -> e.preventDefault ()}>
            <label for="query">Search the docs</label>
            <input id="query" type="search" autocomplete="off" autofocus .value={query} @input={fun e -> this.OnInput e}>
        </form>
        <p class="status" role="status">{status}</p>
        <ol>{hits}</ol>"""

defineElement<Search> "fl-search"
