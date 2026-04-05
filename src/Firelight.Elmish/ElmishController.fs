namespace Firelight.Elmish

open Elmish
open Firelight

/// Wires an Elmish dispatch loop to a LitElement host via ReactiveController.
///
/// The loop starts in the constructor: init() fires synchronously so `model`
/// is always valid before the first render. State updates call
/// `host.requestUpdate()` only while the host is connected; updates while
/// disconnected are applied silently and the component renders the correct
/// state on reconnection.
///
/// Typical usage via the companion module:
///   let loop = ElmishController.simple this init update
///   let loop = ElmishController.withCmds this init update
///
/// Advanced — full Elmish Program (withConsoleTrace, withSubscription, etc.):
///   let loop =
///       Program.mkProgram init update (fun _ _ -> ())
///       |> Program.withConsoleTrace
///       |> fun p -> ElmishController(this, p)
type ElmishController<'Model, 'Msg>(host: ReactiveControllerHost, program: Program<unit, 'Model, 'Msg, unit>) as this =

    let mutable _model: 'Model = Unchecked.defaultof<_>
    let mutable _dispatch: 'Msg -> unit = ignore
    let mutable _alive = false

    do
        host.addController this

        program
        |> Program.withSetState (fun model dispatch ->
            _model <- model
            _dispatch <- dispatch

            if _alive then
                host.requestUpdate ()
        )
        |> Program.runWith ()

    /// Current model — always valid (initialised synchronously on construction).
    member _.model = _model

    /// Dispatch a message into the loop.
    member _.dispatch(msg: 'Msg) = _dispatch msg

    interface ReactiveController with
        member _.hostConnected() =
            _alive <- true
            host.requestUpdate ()

        member _.hostDisconnected() = _alive <- false
        member _.hostUpdate() = ()
        member _.hostUpdated() = ()


[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ElmishController =

    /// Create a simple loop — init and update return only the model, no Cmds.
    let simple (host: ReactiveControllerHost) (init: unit -> 'Model) (update: 'Msg -> 'Model -> 'Model) =
        ElmishController(host, Program.mkSimple init update (fun _ _ -> ()))

    /// Create a loop with Cmd support — init and update return the new model and a Cmd.
    let withCmds
        (host: ReactiveControllerHost)
        (init: unit -> 'Model * Cmd<'Msg>)
        (update: 'Msg -> 'Model -> 'Model * Cmd<'Msg>)
        =
        ElmishController(host, Program.mkProgram init update (fun _ _ -> ()))
