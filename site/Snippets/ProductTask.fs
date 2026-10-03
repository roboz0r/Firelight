module Snippets.ProductTask

open Fable.Core
open Firelight
open Firelight.Task
open type Firelight.Lit

// Stands in for a real request, such as a fetch to your API.
let fetchProduct (id: string) : JS.Promise<string> =
    async {
        do! Async.Sleep 800

        if id = "3" then
            failwith "Product 3 is out of stock"

        return $"Product {id} is in stock"
    }
    |> Async.StartAsPromise

let private status =
    StatusRenderer(
        pending = (fun () -> html $"<p>Loading…</p>"),
        complete = (fun (text: string) -> html $"<p>{text}</p>"),
        error = (fun error -> html $"<p class='error'>{error}</p>")
    )

[<AttachMembers>]
type ProductView() as this =
    inherit LitElement()

    // Product ids are strings: @lit/task needs a plain JS array of args, and Fable compiles int[] to an Int32Array.
    let mutable productId = "1"

    // Runs again whenever productId changes, and re-renders as the request goes
    // from pending to complete or error.
    let product =
        LitTask(
            this,
            TaskConfig(
                TaskFunction(fun (args: string[]) _ -> U2.Case2(fetchProduct args.[0])),
                args = (fun () -> [| productId |])
            )
        )

    static member styles =
        css
            $$"""
        :host { display: grid; gap: 0.5rem; justify-items: start; }
        button { font: inherit; padding: 0.4rem 0.9rem; cursor: pointer; }
        .error { color: #c92a2a; }
        """

    member this.Show(id: string) =
        productId <- id
        this.requestUpdate ()

    member this.ProductButton(id: string) =
        html $"""<button @click={fun _ -> this.Show id}>Product {id}</button>"""

    override this.render() =
        html
            $"""
        <div>{[ "1"; "2"; "3" ] |> List.map this.ProductButton}</div>
        {product.render status |> Option.defaultValue (html $"")}"""

defineElement<ProductView> "my-product-view"
