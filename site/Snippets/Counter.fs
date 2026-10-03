module Snippets.Counter

open Fable.Core
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type Counter() =
    inherit LitElement()

    static member properties = PropertyDeclarations.create [ "count", PropertyDeclaration<int>() ]

    static member styles =
        css
            $$"""
        button { font: inherit; padding: 0.5rem 1rem; cursor: pointer; }
        """

    member val count = 0 with get, set

    override this.render() =
        html
            $"""
        <button @click={fun _ -> this.count <- this.count + 1}>
            Clicked {this.count} times
        </button>"""

defineElement<Counter> "my-counter"
