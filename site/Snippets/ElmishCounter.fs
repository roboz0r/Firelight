module Snippets.ElmishCounter

open Fable.Core
open Firelight
open Firelight.Elmish
open type Firelight.Lit

type Model = { Count: int }

type Msg =
    | Increment
    | Decrement

let init () = { Count = 0 }

let update msg model =
    match msg with
    | Increment -> { model with Count = model.Count + 1 }
    | Decrement -> { model with Count = model.Count - 1 }

[<AttachMembers>]
type ElmishCounter() as this =
    inherit LitElement()

    let elmish = ElmishController.simple this init update

    static member styles =
        css
            $$"""
        :host { display: inline-flex; align-items: center; gap: 0.75rem; }
        button { font: inherit; width: 2.5rem; height: 2.5rem; cursor: pointer; }
        """

    override _.render() =
        html
            $"""
        <button @click={fun _ -> elmish.dispatch Decrement}>−</button>
        <output>{elmish.model.Count}</output>
        <button @click={fun _ -> elmish.dispatch Increment}>+</button>"""

defineElement<ElmishCounter> "my-elmish-counter"
