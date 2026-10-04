module Snippets.Shortcuts

open Fable.Core
open Browser
open Browser.Types
open Firelight
open type Firelight.Lit

/// Calls `run` when `key` is pressed anywhere on the page, unless the user is typing in a field.
type KeyboardShortcut(host: ReactiveControllerHost, key: string, run: unit -> unit) as this =
    let mutable stopListening = ignore

    do host.addController this

    /// Single-key shortcuts need an off switch (WCAG 2.1.4): bind this to a setting.
    member val Enabled = true with get, set

    interface ReactiveController with
        member _.hostConnected() =
            let onKey (e: Event) =
                let e = e :?> KeyboardEvent
                // Where the key was pressed. Inside a shadow root, e.target is only the outermost host.
                let origin = e.composedPath().[0] :?> HTMLElement

                let typing =
                    origin.isContentEditable
                    || List.contains origin.tagName [ "INPUT"; "TEXTAREA"; "SELECT" ]

                if
                    this.Enabled
                    && e.key = key
                    && not e.repeat
                    && not typing
                    && not (e.ctrlKey || e.metaKey || e.altKey)
                then
                    e.preventDefault ()
                    run ()

            document.addEventListener ("keydown", onKey)
            stopListening <- fun () -> document.removeEventListener ("keydown", onKey)

        member _.hostDisconnected() = stopListening ()
        member _.hostUpdate() = ()
        member _.hostUpdated() = ()

[<AttachMembers>]
type ShortcutDemo() as this =
    inherit LitElement()

    let search = createRef<HTMLInputElement> ()

    let shortcuts =
        [
            KeyboardShortcut(this, "/", (fun () -> search.value |> Option.iter (fun s -> s.focus ())))
            KeyboardShortcut(this, "?", (fun () -> this.showHelp <- not this.showHelp))
        ]

    static member properties =
        PropertyDeclarations.create [
            "showHelp", PropertyDeclaration<bool>(state = true)
            "enabled", PropertyDeclaration<bool>(state = true)
        ]

    static member styles =
        css
            $$"""
        :host { display: grid; gap: 0.5rem; justify-items: start; }
        input[type="search"] { font: inherit; padding: 0.3rem 0.5rem; width: 16rem; max-width: 100%; }
        kbd { font: inherit; padding: 0 0.3rem; border: 1px solid var(--border); border-radius: 0.25rem; }
        dl { display: grid; grid-template-columns: auto 1fr; gap: 0.25rem 0.75rem; margin: 0; }
        dd { margin: 0; }
        """

    member val showHelp = false with get, set
    member val enabled = true with get, set

    member this.SetEnabled(on: bool) =
        this.enabled <- on

        for shortcut in shortcuts do
            shortcut.Enabled <- on

    override this.render() =
        let help =
            if this.showHelp then
                html
                    $"""
                <dl aria-label="Keyboard shortcuts">
                    <dt><kbd>/</kbd></dt><dd>Search</dd>
                    <dt><kbd>?</kbd></dt><dd>Show or hide these shortcuts</dd>
                </dl>"""
            else
                nothing

        html
            $"""
        <input type="search" aria-label="Search" aria-keyshortcuts={ifDefined (if this.enabled then Some "/" else None)}
            placeholder={if this.enabled then "Press / to search" else "Search"} {ref search}>
        <p>Press <kbd>?</kbd> for the shortcuts.</p>
        {help}
        <label>
            <input type="checkbox" .checked={this.enabled} @change={Ev.checked' this.SetEnabled}>
            Single-key shortcuts
        </label>"""

defineElement<ShortcutDemo> "my-shortcut-demo"
