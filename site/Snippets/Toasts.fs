module Snippets.Toasts

open Fable.Core
open Browser.Dom
open Browser.Types
open Firelight
open type Firelight.Lit

/// Shows a toast. Any code can call it, and JavaScript can raise the same `toast` event.
let showToast (message: string) =
    document.dispatchEvent (Event.customEvent ("toast", message)) |> ignore

type Toast = { Id: int; Message: string }

/// Shows the toasts raised anywhere on the page, each for five seconds. Put one on the page.
[<AttachMembers>]
type Toaster() =
    inherit LitElement()

    let mutable nextId = 0
    let mutable timers: Map<int, float> = Map.empty
    let mutable stopListening = ignore
    /// Where the focus was before it moved onto a toast, to put it back when the last one goes.
    let mutable cameFrom: HTMLElement option = None

    static member properties =
        PropertyDeclarations.create [ "toasts", PropertyDeclaration<Toast list>(state = true) ]

    static member styles =
        css
            $$"""
        :host { display: block; }
        .stack { display: grid; gap: 0.5rem; justify-items: start; }
        .toast { display: flex; gap: 0.75rem; align-items: center; padding: 0.5rem 0.5rem 0.5rem 1rem;
                 border-radius: 0.5rem; background: var(--fg); color: var(--bg); }
        button { font: inherit; padding: 0 0.5rem; border: 0; border-radius: 0.25rem; background: none;
                 color: inherit; cursor: pointer; }
        button:focus-visible { outline: 2px solid var(--bg); }
        """

    member val toasts: Toast list = [] with get, set

    member this.Add(message: string) =
        nextId <- nextId + 1
        this.toasts <- this.toasts @ [ { Id = nextId; Message = message } ]
        this.Schedule nextId

    member this.Schedule(id: int) =
        timers <- timers.Add(id, window.setTimeout ((fun () -> this.Expire id), 5000))

    /// Time's up, unless the focus is on a toast: then wait another five seconds.
    member this.Expire(id: int) =
        if isNull this.shadowRoot.activeElement then
            this.Remove id
        else
            this.Schedule id

    member this.Remove(id: int) =
        timers.TryFind id |> Option.iter window.clearTimeout
        timers <- timers.Remove id
        this.toasts <- this.toasts |> List.filter (fun t -> t.Id <> id)

    /// Dismissed from its button: the focus moves to the next toast, or back where it came from.
    member this.Dismiss(id: int) =
        let index = this.toasts |> List.findIndex (fun t -> t.Id = id)
        this.Remove id
        let next = this.toasts |> List.tryItem (min index (this.toasts.Length - 1))

        promise {
            let! _ = this.updateComplete

            match next with
            | Some toast -> this.query<HTMLElement> $"#dismiss-{toast.Id}" |> Option.iter _.focus()
            | None -> cameFrom |> Option.iter (fun el -> el.focus ())
        }
        |> Promise.start

    member this.FocusIn(e: FocusEvent) =
        match e.relatedTarget with
        | :? Node as from when not (this.shadowRoot.contains from) -> cameFrom <- Some(from :?> HTMLElement)
        | _ -> ()

    override this.connectedCallback() =
        base.connectedCallback ()
        stopListening <-
            Ev.listen document "toast" (Ev.custom<string> (fun e -> e.detail |> Option.iter this.Add))

    override this.disconnectedCallback() =
        base.disconnectedCallback ()
        stopListening ()
        timers |> Map.iter (fun _ handle -> window.clearTimeout handle)
        timers <- Map.empty
        this.toasts <- []

    member this.ToastView(toast: Toast) =
        html
            $"""
        <div class="toast">
            {toast.Message}
            <button id="dismiss-{toast.Id}" aria-label="Dismiss" @click={fun _ -> this.Dismiss toast.Id}>×</button>
        </div>"""

    override this.render() =
        // One live region, on the page from the start, so screen readers announce each new toast.
        // aria-atomic="false": announce the toast that was added, not the whole stack again.
        html
            $"""
        <div class="stack" role="status" aria-atomic="false" @focusin={Ev.focus this.FocusIn}>
            {repeat (this.toasts, (fun t -> t.Id), this.ToastView)}
        </div>"""

/// A component far from the toaster, which raises toasts without knowing where it is.
[<AttachMembers>]
type SaveButton() =
    inherit LitElement()

    static member styles = css $$"""button { font: inherit; padding: 0.3rem 0.9rem; }"""

    override _.render() =
        html $"""<button @click={fun _ -> showToast "Saved."}>Save</button>"""

defineElement<Toaster> "my-toaster"
defineElement<SaveButton> "my-save-button"
