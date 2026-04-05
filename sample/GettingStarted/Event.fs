namespace GettingStarted

open Fable.Core
open Browser
open Browser.Types
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type EventListeners() =
    inherit LitElement()

    // LitEventListener wraps a handler with additional options (once, capture, passive).
    let listener =
        LitEventListener(
            (fun (ev: Event) ->
                console.log ($"Button clicked! {ev.``type``}")
                console.log (ev.target)
            ),
            once = false
        )

    static member properties =
        PropertyDeclarations.create [ "name", PropertyDeclaration<string>() ]

    member val name = "World" with get, set

    override this.render() =
        html
            $"""
        <div>
            <h1>Hello, {this.name}!</h1>
            <button @click={fun _ -> this.name <- "Fable"}>Change Name</button>
            <button @click={listener}>Advanced Listener</button>
        </div>"""
