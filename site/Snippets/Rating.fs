module Snippets.Rating

open Fable.Core
open Fable.Core.JsInterop
open Browser.Types
open Firelight
open type Firelight.Lit

let private star (isOn: bool) (select: unit -> unit) =
    html $"""<button class={if isOn then "on" else ""} @click={fun _ -> select ()}>★</button>"""

/// <my-rating value="3"></my-rating>
/// Clicking a star sets `value` and raises a "rating-changed" event for the page to handle.
[<AttachMembers>]
type Rating() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [
            "value", PropertyDeclaration<int>(``type`` = jsConstructor<Number>, reflect = true)
        ]

    static member styles =
        css
            $$"""
        button { all: unset; cursor: pointer; font-size: 2rem; color: var(--border); }
        button.on { color: var(--accent); }
        """

    member val value = 0 with get, set

    member this.Select(stars: int) =
        this.value <- stars
        this.dispatchEvent (Event.customEvent ("rating-changed", stars)) |> ignore

    override this.render() =
        html $"""{[ for n in 1..5 -> star (n <= this.value) (fun () -> this.Select n) ]}"""

defineElement<Rating> "my-rating"
