module Snippets.ConfirmDialog

open Fable.Core
open Browser.Types
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type ConfirmDelete() =
    inherit LitElement()

    let dialog = createRef<HTMLDialogElement> ()

    static member properties =
        PropertyDeclarations.create [ "answer", PropertyDeclaration<string>(state = true) ]

    static member styles =
        css
            $$"""
        :host { display: grid; gap: 0.5rem; justify-items: start; }
        button { font: inherit; padding: 0.3rem 0.9rem; }
        p { margin: 0; }
        dialog { border: 1px solid var(--border); border-radius: 0.5rem; background: var(--bg); color: var(--fg);
                 padding: 1rem 1.25rem; max-width: 20rem; }
        dialog::backdrop { background: rgb(0 0 0 / 0.4); }
        dialog h2 { margin: 0 0 0.5rem; font-size: 1.1rem; }
        .actions { display: flex; gap: 0.5rem; justify-content: end; margin-top: 1rem; }
        """

    member val answer = "" with get, set

    member this.Ask() =
        dialog.value
        |> Option.iter (fun d ->
            // Escape closes without a button, and leaves returnValue as it was: clear it first.
            d.returnValue <- ""
            d.showModal ()
        )

    member this.Closed() =
        dialog.value
        |> Option.iter (fun d ->
            this.answer <-
                match d.returnValue with
                | "delete" -> "Deleted report.pdf."
                | _ -> "Kept report.pdf."
        )

    override this.render() =
        html
            $"""
        <button @click={fun _ -> this.Ask()}>Delete report.pdf…</button>
        <p role="status">{if this.answer <> "" then html $"{this.answer}" else nothing}</p>
        <dialog {ref dialog} aria-labelledby="confirm-title" @close={fun _ -> this.Closed()}>
            <form method="dialog">
                <h2 id="confirm-title">Delete report.pdf?</h2>
                <p>You can't undo this.</p>
                <div class="actions">
                    <button value="keep" autofocus>Keep it</button>
                    <button value="delete">Delete</button>
                </div>
            </form>
        </dialog>"""

defineElement<ConfirmDelete> "my-confirm-delete"
