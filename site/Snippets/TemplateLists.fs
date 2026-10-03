module Snippets.TemplateLists

open Fable.Core
open Firelight
open type Firelight.Lit

// A template is a function: it takes what it shows and returns html.
let private itemView (remove: int -> unit) (index: int) (fruit: string) =
    html $"""<li>{fruit} <button @click={fun _ -> remove index}>Remove</button></li>"""

[<AttachMembers>]
type Basket() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [ "fruits", PropertyDeclaration<string list>(state = true) ]

    static member styles =
        css
            $$"""
        :host { display: grid; gap: 0.75rem; justify-items: start; }
        button { font: inherit; padding: 0.2rem 0.6rem; }
        ul { margin: 0; padding-left: 1.25rem; }
        p { margin: 0; }
        """

    member val fruits = [ "Apple"; "Pear" ] with get, set

    member this.Remove(index: int) =
        this.fruits <- List.removeAt index this.fruits

    member this.Add(fruit: string) = this.fruits <- this.fruits @ [ fruit ]

    override this.render() =
        let contents =
            match this.fruits with
            | [] -> html $"<p>The basket is empty.</p>"
            | fruits -> html $"<ul>{fruits |> List.mapi (itemView this.Remove)}</ul>"

        let emptyButton =
            if not this.fruits.IsEmpty then
                html $"""<button @click={fun _ -> this.fruits <- []}>Empty the basket</button>"""
            else
                nothing

        html
            $"""
        <div>
            <button @click={fun _ -> this.Add "Apple"}>Add an apple</button>
            <button @click={fun _ -> this.Add "Pear"}>Add a pear</button>
        </div>
        {contents}
        {emptyButton}"""

defineElement<Basket> "my-basket"
