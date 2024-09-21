namespace GettingStarted

open System
open Fable.Core
open Lit
open type Lit.Lit

type ClockHost =
    inherit ReactiveControllerHost
    abstract member TickRate: int with get

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
    let clock = new ClockController(this)

    static member properties =
        PropertyDeclarations.create [ "tickRate", (PropertyDeclaration<int>()) ]

    static member styles: CSSResult =
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
        let time = clock.Value
        html $"""<p>Current time: {time}</p>"""

    interface ClockHost with
        member this.TickRate = this.tickRate
