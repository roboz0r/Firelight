namespace GettingStarted

open System
open Fable.Core
open Firelight
open type Firelight.Lit

type ClockHost =
    inherit ReactiveControllerHost
    abstract member TickRate: int with get

// A ReactiveController that ticks on an interval and requests a host update each tick.
// The nonce pattern ensures the async loop stops cleanly when the host disconnects.
type ClockController(host: ClockHost) as this =
    let mutable nonce = 1
    let mutable value = DateTime.Now
    do host.addController this

    member _.Value = value

    interface ReactiveController with
        member _.hostConnected() =
            let i = nonce

            async {
                while i = nonce do
                    do! Async.Sleep host.TickRate
                    value <- DateTime.Now
                    host.requestUpdate ()
            }
            |> Async.StartImmediate

        member _.hostDisconnected() = nonce <- nonce + 1
        member _.hostUpdate() = ()
        member _.hostUpdated() = ()

[<AttachMembers>]
type ClockElement() as this =
    inherit LitElement()
    let clock = ClockController(this)

    static member properties =
        PropertyDeclarations.create [ "tickRate", PropertyDeclaration<int>() ]

    static member styles =
        css
            $$"""
        :host {
            display: block;
            font-size: 1em;
            padding: 2px;
            background-color: lightgray;
        }
        """

    member val tickRate = 1000 with get, set

    override _.render() =
        html $"""<p>Current time: {clock.Value}</p>"""

    interface ClockHost with
        member this.TickRate = this.tickRate
