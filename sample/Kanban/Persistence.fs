module Kanban.Persistence

open Browser
open Thoth.Json
open KanbanModel

let storageKey = "kanban-board-v1"

let encode (state: KanbanState) : string = Encode.Auto.toString (0, state)

let decode (json: string) : KanbanState option =
    match Decode.Auto.fromString<KanbanState> (json) with
    | Ok state -> Some state
    | Error err ->
        console.warn ("Failed to decode saved Kanban state:", err)
        None
