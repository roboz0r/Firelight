module Snippets.SwatchPicker

open Fable.Core
open Fable.Core.JsInterop
open Browser.Types
open Firelight
open type Firelight.Lit

let private swatch (color: string) (selected: bool) (pick: unit -> unit) =
    html $"""<button style="background: {color}" aria-label={color} aria-pressed={selected} @click={fun _ -> pick ()}></button>"""

/// The child: shows the colours its parent gives it, and raises color-picked when one is clicked.
[<AttachMembers>]
type SwatchPicker() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [
            "colors", PropertyDeclaration<string list>(attribute = false)
            "selected", PropertyDeclaration<string>()
        ]

    static member styles =
        css
            $$"""
        :host { display: flex; gap: 0.5rem; }
        button { width: 2rem; height: 2rem; border: 2px solid transparent; border-radius: 50%; cursor: pointer; }
        button[aria-pressed="true"] { outline: 3px solid var(--fg); outline-offset: 2px; }
        """

    member val colors: string list = [] with get, set
    member val selected = "" with get, set

    member this.Pick(color: string) =
        this.selected <- color
        this.dispatch (Event.customEvent ("color-picked", color))

    override this.render() =
        html $"""{[ for color in this.colors -> swatch color (color = this.selected) (fun () -> this.Pick color) ]}"""

/// The parent: owns the colour, passes it down as properties and listens for the child's event.
[<AttachMembers>]
type SwatchParent() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [ "color", PropertyDeclaration<string>(state = true) ]

    static member styles =
        css
            $$"""
        :host { display: grid; gap: 0.75rem; justify-items: start; }
        p { margin: 0; }
        button { font: inherit; padding: 0.3rem 0.6rem; }
        """

    member val color = "teal" with get, set

    override this.render() =
        html
            $"""
        <p>The parent's colour is <strong>{this.color}</strong>.</p>
        <my-swatch-picker
            .colors={[ "teal"; "orange"; "purple"; "crimson" ]}
            .selected={this.color}
            @color-picked={Ev.custom<string> (fun e -> e.detail |> Option.iter (fun c -> this.color <- c))}>
        </my-swatch-picker>
        <button @click={fun _ -> this.color <- "teal"}>Reset to teal</button>"""

defineElement<SwatchPicker> "my-swatch-picker"
defineElement<SwatchParent> "my-swatch-parent"
