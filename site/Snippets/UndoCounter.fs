module Snippets.UndoCounter

open Fable.Core
open Firelight
open Firelight.Elmish
open type Firelight.Lit

// The whole state is one immutable value, so undo is just keeping the earlier values.
type Model = { Count: int; History: int list }

type Msg =
    | Increment
    | Decrement
    | Undo

let init () = { Count = 0; History = [] }

let update msg model =
    match msg with
    | Increment ->
        {
            Count = model.Count + 1
            History = model.Count :: model.History
        }
    | Decrement ->
        {
            Count = model.Count - 1
            History = model.Count :: model.History
        }
    | Undo ->
        match model.History with
        | previous :: older -> { Count = previous; History = older }
        | [] -> model

[<AttachMembers>]
type UndoCounter() as this =
    inherit LitElement()

    let elmish = ElmishController.simple this init update

    static member styles =
        css
            $$"""
        :host { display: inline-flex; align-items: center; gap: 0.75rem; }
        button { font: inherit; padding: 0.4rem 0.9rem; cursor: pointer; }
        output { min-width: 2ch; text-align: center; font-weight: 600; }
        """

    override _.render() =
        let model = elmish.model

        html
            $"""
        <button @click={fun _ -> elmish.dispatch Decrement}>−</button>
        <output>{model.Count}</output>
        <button @click={fun _ -> elmish.dispatch Increment}>+</button>
        <button ?disabled={model.History.IsEmpty} @click={fun _ -> elmish.dispatch Undo}>Undo</button>"""

defineElement<UndoCounter> "my-undo-counter"
