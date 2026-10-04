module Snippets.ProductTask

open Fable.Core
open Firelight
open Firelight.Task
open type Firelight.Lit

// Stands in for a real request, such as a fetch to your API.
let fetchProduct (id: string) : JS.Promise<string> =
    promise {
        do! Promise.sleep 800

        if id = "3" then
            failwith "Product 3 is out of stock"

        return $"Product {id} is in stock"
    }

let private status =
    StatusRenderer(
        initial = (fun () -> html $"<p>Pick a product.</p>"),
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
    // from pending to complete or error. With no product, initialState puts the task
    // back in its initial state, without a request. (args always holds one id, but a
    // match must cover every length, so the other lengths go there too.)
    let product =
        LitTask(
            this,
            TaskConfig(
                TaskFunction(fun (args: string[]) _ ->
                    match args with
                    // U2.Case2, not !^: nothing here says if the task's result is the string or the promise.
                    | [| id |] when id <> "" -> U2.Case2(fetchProduct id)
                    | _ -> initialState),
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
        <div>{[ "1"; "2"; "3" ] |> List.map this.ProductButton} <button @click={fun _ -> this.Show ""}>Clear</button></div>
        {product.render status |> Option.defaultValue (html $"")}"""

defineElement<ProductView> "my-product-view"
