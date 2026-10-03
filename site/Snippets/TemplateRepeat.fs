module Snippets.TemplateRepeat

open Fable.Core
open Firelight
open type Firelight.Lit

let private everyone = [ "Ann"; "Bob"; "Cara" ]

// The note box isn't bound to anything: what you type lives only in the DOM.
let private row (name: string) =
    html $"""<li><label>{name} <input size="8"></label></li>"""

[<AttachMembers>]
type RepeatComparison() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [ "people", PropertyDeclaration<string list>(state = true) ]

    static member styles =
        css
            $$"""
        :host { display: grid; gap: 1rem; }
        .lists { display: flex; flex-wrap: wrap; gap: 2rem; }
        p { margin: 0 0 0.4rem; font-weight: 600; }
        ul { margin: 0; padding: 0; list-style: none; display: grid; gap: 0.4rem; }
        button, input { font: inherit; }
        """

    member val people = everyone with get, set

    override this.render() =
        html
            $"""
        <div>
            <button ?disabled={this.people.IsEmpty} @click={fun _ -> this.people <- List.tail this.people}>
                Remove the first person
            </button>
            <button @click={fun _ -> this.people <- everyone}>Reset</button>
        </div>
        <div class="lists">
            <div>
                <p>List.map</p>
                <ul>{this.people |> List.map row}</ul>
            </div>
            <div>
                <p>repeat</p>
                <ul>{repeat (this.people, (fun name _ -> name), (fun name _ -> row name))}</ul>
            </div>
        </div>"""

defineElement<RepeatComparison> "my-repeat-comparison"
