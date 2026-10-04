module Snippets.PropertyTypes

open Fable.Core
open Firelight
open type Firelight.Lit

let private styles =
    css
        $$"""
    :host { display: flex; flex-wrap: wrap; align-items: center; gap: 0.75rem; }
    button { font: inherit; padding: 0.2rem 0.6rem; }
    .tag { padding: 0.1rem 0.6rem; border-radius: 1rem; background: var(--code-bg, #eee); }
    """

/// <my-quantity step="0.5"></my-quantity>
[<AttachMembers>]
type Quantity() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [
            // A float, so Lit converts the attribute's text to a number.
            "step", PropertyDeclaration<float>()
            "total", PropertyDeclaration<float>(state = true)
        ]

    static member styles = styles

    member val step = 1.0 with get, set
    member val total = 0.0 with get, set

    override this.render() =
        html
            $"""
        <button @click={fun _ -> this.total <- this.total + this.step}>Add {this.step}</button>
        <span>Total: <output>{this.total}</output></span>"""

/// Splits "a, b, c" into a list, and joins it back when the property is reflected.
let private commaSeparated =
    AttributeConverter<string list>(
        fromAttribute =
            (fun text ->
                match text with
                | Some text -> text.Split ',' |> Array.map _.Trim() |> Array.filter ((<>) "") |> List.ofArray
                | None -> []),
        toAttribute = fun tags -> Some(String.concat ", " tags)
    )

let private tagChip (tag: string) = html $"""<span class="tag">{tag}</span>"""

/// <my-tags tags="lit, fable, fsharp"></my-tags>
[<AttachMembers>]
type Tags() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [ "tags", PropertyDeclaration<string list>(converter = commaSeparated, reflect = true) ]

    static member styles = styles

    member val tags: string list = [] with get, set

    member this.AddTag() =
        this.tags <- this.tags @ [ $"tag{this.tags.Length + 1}" ]

    override this.render() =
        html
            $"""
        {this.tags |> List.map tagChip}
        <button @click={fun _ -> this.AddTag()}>Add a tag</button>"""

defineElement<Quantity> "my-quantity"
defineElement<Tags> "my-tags"
