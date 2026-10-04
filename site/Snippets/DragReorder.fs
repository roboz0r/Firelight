module Snippets.DragReorder

open Fable.Core
open Browser.Types
open Firelight
open type Firelight.Lit

type Step = { Id: int; Text: string }

/// `item` moved to `index` in the list without it.
let moveTo (index: int) (item: Step) (steps: Step list) =
    let rest = steps |> List.filter (fun s -> s.Id <> item.Id)
    List.insertAt (max 0 (min index rest.Length)) item rest

[<AttachMembers>]
type ReorderList() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [
            "steps", PropertyDeclaration<Step list>(state = true)
            "dragging", PropertyDeclaration<Step option>(state = true)
            "message", PropertyDeclaration<string>(state = true)
        ]

    static member styles =
        css
            $$"""
        :host { display: grid; gap: 0.5rem; justify-items: start; }
        ol { margin: 0; padding: 0; list-style: none; display: grid; gap: 0.35rem; width: 18rem; }
        li { display: flex; align-items: center; gap: 0.5rem; padding: 0.35rem 0.5rem; cursor: grab;
             border: 1px solid var(--border); border-radius: 0.5rem; background: var(--bg); }
        li.dragging { opacity: 0.5; }
        .text { flex: 1; }
        button { font: inherit; padding: 0 0.45rem; }
        p { margin: 0; }
        """

    member val steps =
        [ "Wake up"; "Make coffee"; "Read the news"; "Walk the dog"; "Start work" ]
        |> List.mapi (fun i text -> { Id = i; Text = text })
        with get, set

    member val dragging: Step option = None with get, set
    member val message = "" with get, set

    member this.Move(step: Step, index: int) =
        this.steps <- moveTo index step this.steps
        let position = 1 + (this.steps |> List.findIndex (fun s -> s.Id = step.Id))
        this.message <- $"{step.Text} moved to position {position} of {this.steps.Length}."

    /// From the keyboard: move, then put the focus back on the button, as moving the item's
    /// element takes the focus off it.
    member this.Nudge(step: Step, index: int, button: string) =
        this.Move(step, index)

        async {
            let! _ = this.updateComplete |> Async.AwaitPromise
            let target = this.shadowRoot.querySelector ($"#{button}-{step.Id}") :?> HTMLElement

            if not (isNull target) then
                target.focus ()
        }
        |> Async.StartImmediate

    member this.StepView (step: Step) (index: int) =
        let dragStart =
            Ev.drag (fun e ->
                this.dragging <- Some step
                e.dataTransfer.effectAllowed <- "move"
                // Firefox only starts a drag that carries some data.
                e.dataTransfer.setData ("text/plain", step.Text) |> ignore
            )

        // Allowing a drop means cancelling dragover.
        let dragOver = Ev.drag (fun e -> if this.dragging.IsSome then e.preventDefault ())

        let drop =
            Ev.drag (fun e ->
                e.preventDefault ()
                this.dragging |> Option.iter (fun dragged -> this.Move(dragged, index))
            )

        html
            $"""
        <li draggable="true" class={if this.dragging = Some step then "dragging" else ""}
            @dragstart={dragStart} @dragover={dragOver} @drop={drop} @dragend={fun _ -> this.dragging <- None}>
            <span class="text">{step.Text}</span>
            <button id="up-{step.Id}" aria-label="Move {step.Text} up" aria-disabled={index = 0}
                @click={fun _ -> if index > 0 then this.Nudge(step, index - 1, "up")}>↑</button>
            <button id="down-{step.Id}" aria-label="Move {step.Text} down" aria-disabled={index = this.steps.Length - 1}
                @click={fun _ -> if index < this.steps.Length - 1 then this.Nudge(step, index + 1, "down")}>↓</button>
        </li>"""

    override this.render() =
        html
            $"""
        <ol aria-label="Morning routine">{repeat (this.steps, (fun s _ -> s.Id), this.StepView)}</ol>
        <p role="status">{if this.message <> "" then html $"{this.message}" else nothing}</p>"""

defineElement<ReorderList> "my-reorder-list"
