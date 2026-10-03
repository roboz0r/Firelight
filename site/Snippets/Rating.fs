module Snippets.Rating

open Fable.Core
open Fable.Core.JsInterop
open Browser.Types
open Firelight
open type Firelight.Lit

// Each star is a button named "n out of 5"; the one matching the rating is pressed.
let private star (n: int) (value: int) (select: unit -> unit) =
    html
        $"""<button class={if n <= value then "on" else ""} aria-label="{n} out of 5"
                aria-pressed={n = value} @click={fun _ -> select ()}>{if n <= value then "★" else "☆"}</button>"""

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
        button { all: unset; cursor: pointer; font-size: 2rem; color: var(--muted); border-radius: 0.25rem; }
        button.on { color: var(--accent); }
        button:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }
        """

    member val value = 0 with get, set

    member this.Select(stars: int) =
        this.value <- stars
        this.dispatchEvent (Event.customEvent ("rating-changed", stars)) |> ignore

    override this.render() =
        html $"""{[ for n in 1..5 -> star n this.value (fun () -> this.Select n) ]}"""

defineElement<Rating> "my-rating"
