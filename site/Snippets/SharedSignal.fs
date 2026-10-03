module Snippets.SharedSignal

open Fable.Core
open Firelight
open Firelight.Signals
open type LitSignals

// State outside any component. Every component that reads it stays in sync.
let clicks = signal 0
let doubled = computed (fun () -> clicks.get () * 2)

[<AttachMembers>]
type ClickButton() =
    inherit LitElement()

    static member styles =
        Lit.css $$"""button { font: inherit; padding: 0.4rem 0.9rem; cursor: pointer; }"""

    override _.render() =
        html $"""<button @click={fun _ -> clicks.set (clicks.get () + 1)}>Click me</button>"""

// A separate component, with no parent, property or event connecting it to the button.
// Interpolated signals update just their part of the template.
[<AttachMembers>]
type ClickTotal() =
    inherit LitElement()

    override _.render() =
        html $"""<p>{clicks} clicks, doubled is {doubled}</p>"""

defineElement<ClickButton> "my-click-button"
defineElement<ClickTotal> "my-click-total"
