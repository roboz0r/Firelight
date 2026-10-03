module Snippets.BigList

open Fable.Core
open Firelight
open Firelight.Virtualizer
open type Firelight.Lit

let rows = [| for i in 1..10_000 -> $"Row {i}" |]

let private row = RenderItem<string>(fun text _ -> html $"<li>{text}</li>")

/// Ten thousand rows, but only the ones in view exist in the DOM.
[<AttachMembers>]
type BigList() =
    inherit LitElement()

    static member styles =
        css
            $$"""
        :host { display: block; width: 100%; }
        ul {
            height: 16rem; overflow: auto; margin: 0; padding: 0; list-style: none;
            border: 1px solid var(--border); border-radius: 0.5rem; background: var(--bg);
        }
        li { width: 100%; padding: 0.4rem 0.75rem; border-bottom: 1px solid var(--border); }
        """

    override _.render() =
        html
            $"""<ul tabindex="0" aria-label="Ten thousand rows">{Virtualizer.virtualize (VirtualizeConfig(rows, row, scroller = true))}</ul>"""

defineElement<BigList> "my-big-list"
