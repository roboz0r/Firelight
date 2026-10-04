module Snippets.EventCancel

open Fable.Core
open Browser.Types
open Firelight
open type Firelight.Lit

/// A note that asks before it closes: it raises a cancelable "note-closing" event, with its heading
/// as the detail, and closes only if no listener cancels it.
[<AttachMembers>]
type ClosableNote() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [
            "heading", PropertyDeclaration<string>()
            "closed", PropertyDeclaration<bool>(state = true)
        ]

    static member styles =
        css
            $$"""
        :host { display: block; }
        .note { display: flex; gap: 0.75rem; align-items: center; padding: 0.4rem 0.75rem; border: 1px solid var(--border); border-radius: 0.5rem; }
        button { font: inherit; padding: 0.2rem 0.6rem; }
        """

    member val heading = "Note" with get, set
    member val closed = false with get, set

    member this.Close() =
        let closing =
            Event.customEvent ("note-closing", this.heading, cancelable = true)
        // false when a listener called preventDefault.
        if this.dispatchEvent closing then
            this.closed <- true

    override this.render() =
        if this.closed then
            html $"""<button @click={fun _ -> this.closed <- false}>Reopen {this.heading}</button>"""
        else
            html
                $"""
            <div class="note">
                <strong>{this.heading}</strong>
                <button @click={fun _ -> this.Close()}>Close</button>
            </div>"""

/// Cancels "note-closing" while the box is ticked.
[<AttachMembers>]
type NoteBoard() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [
            "keepOpen", PropertyDeclaration<bool>(state = true)
            "message", PropertyDeclaration<string>(state = true)
        ]

    static member styles =
        css
            $$"""
        :host { display: grid; gap: 0.75rem; justify-items: start; }
        p { margin: 0; }
        """

    member val keepOpen = false with get, set
    member val message = "Nothing has closed yet." with get, set

    override this.render() =
        let onClosing =
            Ev.custom<string> (fun e ->
                let heading = e.detail |> Option.defaultValue "A note"

                if this.keepOpen then
                    e.preventDefault ()
                    this.message <- $"{heading} stays open."
                else
                    this.message <- $"{heading} closed.")

        html
            $"""
        <label><input type="checkbox" .checked={this.keepOpen}
            @change={Ev.checked' (fun on -> this.keepOpen <- on)}> Keep notes open</label>
        <my-closable-note heading="Shopping" @note-closing={onClosing}></my-closable-note>
        <p role="status">{this.message}</p>"""

defineElement<ClosableNote> "my-closable-note"
defineElement<NoteBoard> "my-note-board"
