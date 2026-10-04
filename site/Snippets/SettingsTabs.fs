module Snippets.SettingsTabs

open Fable.Core
open Browser.Types
open Firelight
open type Firelight.Lit

let private tabs = [ "general", "General"; "notes", "Notes"; "about", "About" ]

// Each panel is its own template, so `cache` can keep each one's DOM.
let private general () =
    html $"""<label><input type="checkbox"> Send me a weekly summary</label>"""

let private notes () =
    html $"""<label for="notes">Notes</label><textarea id="notes" rows="3"></textarea>"""

let private about () =
    html $"""<p>Settings, version 1.0.</p>"""

[<AttachMembers>]
type SettingsTabs() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [ "selected", PropertyDeclaration<string>(state = true) ]

    static member styles =
        css
            $$"""
        :host { display: block; width: 20rem; max-width: 100%; }
        [role="tablist"] { display: flex; gap: 0.25rem; border-bottom: 1px solid var(--border); }
        [role="tab"] { font: inherit; padding: 0.4rem 0.9rem; border: 1px solid transparent; border-bottom: 0;
                       border-radius: 0.5rem 0.5rem 0 0; background: none; color: inherit; cursor: pointer; }
        [role="tab"][aria-selected="true"] { border-color: var(--border); background: var(--bg); margin-bottom: -1px; }
        [role="tabpanel"] { display: grid; gap: 0.5rem; padding: 1rem; border: 1px solid var(--border);
                            border-top: 0; background: var(--bg); }
        textarea { font: inherit; }
        p { margin: 0; }
        """

    member val selected = "general" with get, set

    member this.Select(id: string, moveFocus: bool) =
        this.selected <- id

        if moveFocus then
            async {
                let! _ = this.updateComplete |> Async.AwaitPromise
                (this.shadowRoot.querySelector ("#tab-" + id) :?> HTMLElement).focus ()
            }
            |> Async.StartImmediate

    /// Arrow keys, Home and End move between the tabs, as in a native tab strip.
    member this.OnKey(e: KeyboardEvent) =
        let ids = tabs |> List.map fst
        let i = ids |> List.findIndex ((=) this.selected)

        let next =
            match e.key with
            | "ArrowRight" -> Some ids[(i + 1) % ids.Length]
            | "ArrowLeft" -> Some ids[(i + ids.Length - 1) % ids.Length]
            | "Home" -> Some(List.head ids)
            | "End" -> Some(List.last ids)
            | _ -> None

        next
        |> Option.iter (fun id ->
            e.preventDefault ()
            this.Select(id, true)
        )

    member this.Tab(id: string, label: string) =
        let selected = id = this.selected

        // Only the selected tab is in the Tab order; the arrow keys reach the others.
        html
            $"""
        <button role="tab" id="tab-{id}" aria-selected={selected} aria-controls="panel"
            tabindex={if selected then 0 else -1} @click={fun _ -> this.Select(id, false)}>{label}</button>"""

    override this.render() =
        let panel =
            match this.selected with
            | "notes" -> notes ()
            | "about" -> about ()
            | _ -> general ()

        html
            $"""
        <div role="tablist" aria-label="Settings" @keydown={Ev.keyboard this.OnKey}>
            {tabs |> List.map this.Tab}
        </div>
        <div role="tabpanel" id="panel" aria-labelledby="tab-{this.selected}" tabindex="0">{cache panel}</div>"""

defineElement<SettingsTabs> "my-settings-tabs"
