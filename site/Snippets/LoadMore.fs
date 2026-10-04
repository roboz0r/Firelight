module Snippets.LoadMore

open Fable.Core
open Browser.Types
open Firelight
open Firelight.Observers
open type Firelight.Lit

let private pageSize = 10
let private total = 50

// Stands in for a request for one page of results, such as /api/items?page=2.
let private fetchPage (page: int) =
    promise {
        do! Promise.sleep 400
        return [ for i in page * pageSize + 1 .. min total ((page + 1) * pageSize) -> $"Item {i}" ]
    }

[<AttachMembers>]
type LoadMoreList() as this =
    inherit LitElement()

    let endOfList = createRef<HTMLElement> ()

    // `target = null`: observe nothing until firstUpdated hands it the end of the list.
    let watcher =
        IntersectionController<bool>(
            this,
            IntersectionControllerConfig(
                target = null,
                callback =
                    IntersectionValueCallback(fun entries _ ->
                        let inView = entries |> Array.exists (fun e -> e.isIntersecting)

                        if inView then
                            this.LoadMore()

                        inView
                    )
            )
        )

    static member properties =
        PropertyDeclarations.create [
            "items", PropertyDeclaration<string list>(state = true)
            "loading", PropertyDeclaration<bool>(state = true)
        ]

    static member styles =
        css
            $$"""
        :host { display: grid; gap: 0.5rem; justify-items: start; }
        .scroller { height: 12rem; width: 16rem; overflow-y: auto; border: 1px solid var(--border);
                    border-radius: 0.5rem; padding: 0.5rem 1rem; }
        ul { margin: 0; padding-left: 1.25rem; }
        li { padding: 0.25rem 0; }
        .scroller:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }
        .end { height: 1px; }
        button { font: inherit; padding: 0.3rem 0.9rem; }
        p { margin: 0; }
        """

    member val items = [ for i in 1..pageSize -> $"Item {i}" ] with get, set
    member val loading = false with get, set

    member this.HasMore = this.items.Length < total

    member this.LoadMore() =
        if not this.loading && this.HasMore then
            this.loading <- true

            promise {
                let! more = fetchPage (this.items.Length / pageSize)
                this.items <- this.items @ more
                this.loading <- false
                let! _ = this.updateComplete

                // An observer reports changes. If the end is still in view, nothing changed, so
                // observe it afresh: the observer then reports where it is now. Not if the
                // component has left the page meanwhile, which stopped the observer.
                endOfList.value
                |> Option.iter (fun el ->
                    watcher.unobserve el

                    if this.HasMore && this.isConnected then
                        watcher.observe el
                )
            }
            |> Promise.start

    override _.firstUpdated _ =
        endOfList.value |> Option.iter watcher.observe

    override this.render() =
        let label =
            if not this.HasMore then $"All {total} loaded"
            elif this.loading then "Loading…"
            else "Load more"

        // The button stays, outside the box, so it keeps the focus as the list grows and ends.
        html
            $"""
        <div class="scroller" tabindex="0" role="region" aria-label="Items">
            <ul>{this.items |> List.map (fun item -> html $"<li>{item}</li>")}</ul>
            <div class="end" {ref endOfList}></div>
        </div>
        <button aria-disabled={not this.HasMore} @click={fun _ -> this.LoadMore()}>{label}</button>
        <p role="status">Showing {this.items.Length} of {total}.</p>"""

defineElement<LoadMoreList> "my-load-more"
