module Kanban.KanbanModel

open System

type CardId = | CardId of string
type ColumnId = | ColumnId of string

type Priority =
    | Low
    | Medium
    | High

type Card =
    {
        Id: CardId
        Title: string
        Description: string
        Priority: Priority
    }

type Column =
    {
        Id: ColumnId
        Title: string
        CardIds: CardId list
    }

/// Tracks the current drag-and-drop interaction phase.
type DragState =
    | NotDragging
    | Dragging of cardId: CardId * sourceColumnId: ColumnId
    | DragOver of cardId: CardId * sourceColumnId: ColumnId * targetColumnId: ColumnId * insertIndex: int

/// What the user is currently editing.
/// EditingCard holds a working copy so unsaved changes (e.g. priority) are
/// reflected in the UI without touching the canonical Cards map.
type EditTarget =
    | NoEdit
    | EditingCard of Card
    | AddingCard of ColumnId
    | AddingColumn

type KanbanState =
    {
        Columns: Column list
        Cards: Map<CardId, Card>
        Drag: DragState
        Edit: EditTarget
    }

type KanbanMsg =
    | AddCard of columnId: ColumnId * title: string
    | UpdateCard of Card
    | DeleteCard of CardId
    | DragStart of cardId: CardId * sourceColumnId: ColumnId
    | DragEnterColumn of targetColumnId: ColumnId * insertIndex: int
    | DragEnd
    | DropCard
    | AddColumn of title: string
    | DeleteColumn of ColumnId
    | StartEdit of EditTarget
    | SetEditPriority of Priority
    | CancelEdit

module KanbanMsg =

    let private newCardId () = CardId(Guid.NewGuid().ToString())
    let private newColumnId () = ColumnId(Guid.NewGuid().ToString())

    let private removeCardFromColumn (cardId: CardId) (col: Column) =
        { col with
            CardIds = col.CardIds |> List.filter (fun id -> id <> cardId)
        }

    let private insertAt index item list =
        let before = list |> List.truncate index
        let after = list |> List.skip (min index (List.length list))
        before @ [ item ] @ after

    let update (msg: KanbanMsg) (state: KanbanState) : KanbanState =
        match msg with
        | AddCard(columnId, title) ->
            let cardId = newCardId ()

            let card =
                {
                    Id = cardId
                    Title = title
                    Description = ""
                    Priority = Medium
                }

            { state with
                Cards = state.Cards |> Map.add cardId card
                Columns =
                    state.Columns
                    |> List.map (fun col ->
                        if col.Id = columnId then
                            { col with
                                CardIds = col.CardIds @ [ cardId ]
                            }
                        else
                            col
                    )
                Edit = NoEdit
            }

        | UpdateCard card ->
            { state with
                Cards = state.Cards |> Map.add card.Id card
                Edit = NoEdit
            }

        | DeleteCard cardId ->
            { state with
                Cards = state.Cards |> Map.remove cardId
                Columns = state.Columns |> List.map (removeCardFromColumn cardId)
                Edit = NoEdit
            }

        | DragStart(cardId, sourceColumnId) ->
            { state with
                Drag = Dragging(cardId, sourceColumnId)
            }

        | DragEnterColumn(targetColumnId, insertIndex) ->
            match state.Drag with
            | Dragging(cardId, sourceColumnId) ->
                { state with
                    Drag = DragOver(cardId, sourceColumnId, targetColumnId, insertIndex)
                }
            | DragOver(cardId, sourceColumnId, prevTarget, prevIndex) ->
                // Short-circuit if nothing changed (avoids unnecessary renders from rapid dragover events)
                if prevTarget = targetColumnId && prevIndex = insertIndex then
                    state
                else
                    { state with
                        Drag = DragOver(cardId, sourceColumnId, targetColumnId, insertIndex)
                    }
            | NotDragging -> state

        | DragEnd -> { state with Drag = NotDragging }

        | DropCard ->
            match state.Drag with
            | DragOver(cardId, sourceColumnId, targetColumnId, insertIndex) ->
                let columns =
                    state.Columns
                    |> List.map (removeCardFromColumn cardId)
                    |> List.map (fun col ->
                        if col.Id = targetColumnId then
                            { col with
                                CardIds = insertAt insertIndex cardId col.CardIds
                            }
                        else
                            col
                    )

                { state with
                    Columns = columns
                    Drag = NotDragging
                }
            | _ -> { state with Drag = NotDragging }

        | AddColumn title ->
            let col =
                {
                    Id = newColumnId ()
                    Title = title
                    CardIds = []
                }

            { state with
                Columns = state.Columns @ [ col ]
                Edit = NoEdit
            }

        | DeleteColumn columnId ->
            let col = state.Columns |> List.tryFind (fun c -> c.Id = columnId)

            let cardIdsToRemove =
                col |> Option.map (fun c -> c.CardIds) |> Option.defaultValue []

            { state with
                Columns = state.Columns |> List.filter (fun c -> c.Id <> columnId)
                Cards = cardIdsToRemove |> List.fold (fun m id -> Map.remove id m) state.Cards
            }

        | StartEdit target -> { state with Edit = target }

        | SetEditPriority priority ->
            match state.Edit with
            | EditingCard card ->
                { state with
                    Edit = EditingCard { card with Priority = priority }
                }
            | _ -> state

        | CancelEdit -> { state with Edit = NoEdit }

    /// Default initial state with three columns and a few sample cards.
    let init () : KanbanState =
        let todoCol = ColumnId "todo"
        let progressCol = ColumnId "in-progress"
        let doneCol = ColumnId "done"

        let card1 =
            {
                Id = CardId "sample-1"
                Title = "Set up project structure"
                Description = "Create the initial project scaffolding"
                Priority = High
            }

        let card2 =
            {
                Id = CardId "sample-2"
                Title = "Design domain model"
                Description = "Define types for cards, columns, and state"
                Priority = High
            }

        let card3 =
            {
                Id = CardId "sample-3"
                Title = "Implement drag and drop"
                Description = "Add DnD support between columns"
                Priority = Medium
            }

        let card4 =
            {
                Id = CardId "sample-4"
                Title = "Add card editing"
                Description = "Inline and modal editing for card details"
                Priority = Low
            }

        {
            Columns =
                [
                    {
                        Id = todoCol
                        Title = "To Do"
                        CardIds = [ card3.Id; card4.Id ]
                    }
                    {
                        Id = progressCol
                        Title = "In Progress"
                        CardIds = [ card2.Id ]
                    }
                    {
                        Id = doneCol
                        Title = "Done"
                        CardIds = [ card1.Id ]
                    }
                ]
            Cards = [ card1; card2; card3; card4 ] |> List.map (fun c -> c.Id, c) |> Map.ofList
            Drag = NotDragging
            Edit = NoEdit
        }
