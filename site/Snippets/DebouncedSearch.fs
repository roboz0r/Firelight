module Snippets.DebouncedSearch

open System
open Fable.Core
open Fable.Core.JsInterop
open Fetch
open Firelight
open Firelight.Task
open type Firelight.Lit

let mutable private requests = 0

// Stands in for your search API: it fetches a file of every fruit, next to this page, and filters
// it here, where your server would do the filtering. The signal cancels the request.
let private search (query: string) (signal: AbortSignal) =
    requests <- requests + 1

    promise {
        let url = $"fruits.json?q={Uri.EscapeDataString query}"
        let! response = fetch url [ Signal signal ]
        let! fruits = response.json<string[]> ()
        return fruits |> Array.filter (fun f -> f.ToLower().Contains(query.ToLower())) |> List.ofArray
    }

[<AttachMembers>]
type DebouncedSearch() as this =
    inherit LitElement()

    let matches =
        LitTask(
            this,
            TaskConfig(
                TaskFunction(fun (args: string[]) options ->
                    match args with
                    | [| text |] when text.Trim() <> "" ->
                        !^(promise {
                            // Each keystroke starts a new run and aborts the one before,
                            // so only a query left alone for 300 ms gets past this line.
                            do! Promise.sleep 300
                            options.signal.throwIfAborted ()
                            return! search (text.Trim()) options.signal
                        })
                    // An empty box, and any other args: no results, no request.
                    | _ -> !^[]
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
            | _ when this.query.Trim() = "" -> "Type to search for a fruit."
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
