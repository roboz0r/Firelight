module Snippets.EventPath

open Fable.Core
open Browser.Types
open Firelight
open type Firelight.Lit

type Signal = { Number: int }

/// Raises "signal" from its host, composed or not.
[<AttachMembers>]
type SignalButtons() =
    inherit LitElement()

    static member styles =
        css
            $$"""
        :host { display: flex; flex-wrap: wrap; gap: 0.5rem; }
        button { font: inherit; padding: 0.2rem 0.6rem; }
        """

    member val sent = 0 with get, set

    member this.Raise(composed: bool) =
        this.sent <- this.sent + 1
        let signal = { Number = this.sent }
        this.dispatchEvent (Event.customEvent ("signal", signal, composed = composed)) |> ignore

    override this.render() =
        html
            $"""
        <button @click={fun _ -> this.Raise true}>Raise, composed</button>
        <button @click={fun _ -> this.Raise false}>Raise, not composed</button>"""

/// Has a <my-signal-buttons> in its shadow DOM and listens to it.
[<AttachMembers>]
type EventPath() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [ "heard", PropertyDeclaration<string>(state = true) ]

    static member styles =
        css
            $$"""
        :host { display: grid; gap: 0.75rem; padding: 0.75rem; border: 1px dashed var(--border); border-radius: 0.5rem; }
        p { margin: 0; }
        """

    member val heard = "The component hasn't heard a signal yet." with get, set

    override this.render() =
        let onSignal =
            Ev.custom<Signal> (fun e ->
                let target = (e.target :?> HTMLElement).localName

                e.detail
                |> Option.iter (fun s -> this.heard <- $"The component heard signal {s.Number}. Its target: <{target}>."))

        html
            $"""
        <my-signal-buttons @signal={onSignal}></my-signal-buttons>
        <p>{this.heard}</p>"""

defineElement<SignalButtons> "my-signal-buttons"
defineElement<EventPath> "my-event-path"
