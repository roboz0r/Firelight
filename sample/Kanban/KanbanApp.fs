namespace Kanban

open Browser
open Fable.Core
open Elmish
open Firelight
open Firelight.Elmish
open type Firelight.Lit

open KanbanModel
open Templates

[<AttachMembers>]
type KanbanApp() as this =
    inherit LitElement()

    let elmish =
        Program.mkSimple KanbanMsg.init KanbanMsg.update (fun _ _ -> ())
        |> DevTools.withLocalStorage DevTools.DefaultDelay Persistence.storageKey Persistence.encode Persistence.decode
        |> fun p -> ElmishController(this, p)

    static member styles =
        [|
            Styling.theme
            css
                $$"""
            .drop-indicator {
                animation: drop-pulse 1s ease-in-out infinite;
            }
            @keyframes drop-pulse {
                0%, 100% { opacity: 0.4; }
                50% { opacity: 1; }
            }
            """
        |]

    override _.render() =
        boardTemplate elmish.model elmish.dispatch

module App =
    let start () =
        Lit.defineElement<KanbanApp> "kanban-app"

        Lit.render (html $"<kanban-app></kanban-app>", document.getElementById "kanban-app")
        |> ignore

    do start ()
