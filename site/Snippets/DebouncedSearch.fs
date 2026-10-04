module Snippets.DebouncedSearch

open Fable.Core
open Firelight
open Firelight.Task
open type Firelight.Lit

let private fruits =
    [ "Apple"; "Apricot"; "Banana"; "Blackberry"; "Blackcurrant"; "Blueberry"; "Cherry"; "Cranberry"
      "Damson"; "Date"; "Elderberry"; "Fig"; "Gooseberry"; "Grape"; "Greengage"; "Kiwi"; "Lemon"
      "Lime"; "Mango"; "Melon"; "Mulberry"; "Nectarine"; "Orange"; "Peach"; "Pear"; "Plum"
      "Quince"; "Raspberry"; "Redcurrant"; "Strawberry" ]

let mutable private requests = 0

// Stands in for a request to your search API, such as `fetch` with the task's signal.
let private search (query: string) : JS.Promise<string list> =
    requests <- requests + 1

    async {
        do! Async.Sleep 200
        return fruits |> List.filter (fun f -> f.ToLower().Contains(query.ToLower()))
    }
    |> Async.StartAsPromise

[<AttachMembers>]
type DebouncedSearch() as this =
    inherit LitElement()

    let matches =
        LitTask(
            this,
            TaskConfig(
                TaskFunction(fun (args: string[]) options ->
                    let query = args[0].Trim()

                    if query = "" then
                        U2.Case1 []
                    else
                        async {
                            // Each keystroke starts a new run and aborts the one before,
                            // so only a query left alone for 300 ms gets past this line.
                            do! Async.Sleep 300
                            options.signal.throwIfAborted ()
                            return! search query |> Async.AwaitPromise
                        }
                        |> Async.StartAsPromise
                        |> U2.Case2
                ),
                args = fun () -> [| this.query |]
            )
        )

    static member properties =
        PropertyDeclarations.create [ "query", PropertyDeclaration<string>(state = true) ]

    static member styles =
        css
            $$"""
        :host { display: grid; gap: 0.5rem; justify-items: start; }
        input { font: inherit; padding: 0.3rem 0.5rem; width: 16rem; max-width: 100%; }
        p, ul { margin: 0; }
        """

    member val query = "" with get, set

    override this.render() =
        // The last results stay on screen while the next search runs.
        let found = matches.value |> Option.defaultValue []

        let message =
            match matches.status with
            | _ when this.query.Trim() = "" -> $"Type to search {fruits.Length} fruits."
            | TaskStatus.PENDING -> "Searching…"
            | TaskStatus.ERROR -> "The search failed."
            | _ -> $"{found.Length} found. Requests so far: {requests}."

        html
            $"""
        <input type="search" aria-label="Search fruit" .value={this.query}
            @input={Ev.value (fun q -> this.query <- q)}>
        <p role="status">{message}</p>
        <ul>{found |> List.map (fun f -> html $"<li>{f}</li>")}</ul>"""

defineElement<DebouncedSearch> "my-debounced-search"
