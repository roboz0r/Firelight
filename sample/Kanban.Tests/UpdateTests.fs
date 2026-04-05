module Kanban.Tests.UpdateTests

open Fable.Pyxpecto
open Kanban.KanbanModel

/// A minimal state with two columns and one card in the first column.
let private twoColumnState () =
    let col1 = ColumnId "col-1"
    let col2 = ColumnId "col-2"

    let card1 =
        {
            Id = CardId "card-1"
            Title = "Task 1"
            Description = "Desc 1"
            Priority = Medium
        }

    let card2 =
        {
            Id = CardId "card-2"
            Title = "Task 2"
            Description = "Desc 2"
            Priority = Low
        }

    {
        Columns =
            [
                {
                    Id = col1
                    Title = "To Do"
                    CardIds = [ card1.Id; card2.Id ]
                }
                {
                    Id = col2
                    Title = "Done"
                    CardIds = []
                }
            ]
        Cards = Map.ofList [ card1.Id, card1; card2.Id, card2 ]
        Drag = NotDragging
        Edit = NoEdit
    }

let addCardTests =
    testList "AddCard" [
        testCase "appends card to correct column"
        <| fun () ->
            let state = twoColumnState ()
            let state' = KanbanMsg.update (AddCard(ColumnId "col-1", "New Card")) state

            let col1 = state'.Columns |> List.find (fun c -> c.Id = ColumnId "col-1")
            Expect.equal col1.CardIds.Length 3 "Column should have 3 cards"

            let newCardId = col1.CardIds |> List.last
            let newCard = state'.Cards |> Map.find newCardId
            Expect.equal newCard.Title "New Card" "Card title should match"
            Expect.equal newCard.Priority Medium "Default priority should be Medium"

        testCase "does not affect other columns"
        <| fun () ->
            let state = twoColumnState ()
            let state' = KanbanMsg.update (AddCard(ColumnId "col-1", "New Card")) state

            let col2 = state'.Columns |> List.find (fun c -> c.Id = ColumnId "col-2")
            Expect.equal col2.CardIds.Length 0 "Other column should remain empty"

        testCase "clears edit state"
        <| fun () ->
            let state =
                { twoColumnState () with
                    Edit = AddingCard(ColumnId "col-1")
                }

            let state' = KanbanMsg.update (AddCard(ColumnId "col-1", "New Card")) state
            Expect.equal state'.Edit NoEdit "Edit should be cleared"
    ]

let updateCardTests =
    testList "UpdateCard" [
        testCase "replaces card in map"
        <| fun () ->
            let state = twoColumnState ()

            let updated =
                {
                    Id = CardId "card-1"
                    Title = "Updated"
                    Description = "New desc"
                    Priority = High
                }

            let state' = KanbanMsg.update (UpdateCard updated) state

            let card = state'.Cards |> Map.find (CardId "card-1")
            Expect.equal card.Title "Updated" "Title should be updated"
            Expect.equal card.Priority High "Priority should be updated"

        testCase "does not affect column structure"
        <| fun () ->
            let state = twoColumnState ()

            let updated =
                {
                    Id = CardId "card-1"
                    Title = "Updated"
                    Description = ""
                    Priority = High
                }

            let state' = KanbanMsg.update (UpdateCard updated) state
            Expect.equal state'.Columns state.Columns "Columns should be unchanged"
    ]

let deleteCardTests =
    testList "DeleteCard" [
        testCase "removes card from map and column"
        <| fun () ->
            let state = twoColumnState ()
            let state' = KanbanMsg.update (DeleteCard(CardId "card-1")) state

            Expect.isFalse (state'.Cards |> Map.containsKey (CardId "card-1")) "Card should be removed from map"
            let col1 = state'.Columns |> List.find (fun c -> c.Id = ColumnId "col-1")
            Expect.isFalse (col1.CardIds |> List.contains (CardId "card-1")) "Card should be removed from column"

        testCase "leaves other cards intact"
        <| fun () ->
            let state = twoColumnState ()
            let state' = KanbanMsg.update (DeleteCard(CardId "card-1")) state

            Expect.isTrue (state'.Cards |> Map.containsKey (CardId "card-2")) "Other card should remain"
    ]

let dragDropTests =
    testList "Drag and Drop" [
        testCase "DragStart sets Dragging state"
        <| fun () ->
            let state = twoColumnState ()
            let state' = KanbanMsg.update (DragStart(CardId "card-1", ColumnId "col-1")) state
            Expect.equal state'.Drag (Dragging(CardId "card-1", ColumnId "col-1")) "Should be Dragging"

        testCase "DragEnterColumn transitions from Dragging to DragOver"
        <| fun () ->
            let state =
                { twoColumnState () with
                    Drag = Dragging(CardId "card-1", ColumnId "col-1")
                }

            let state' = KanbanMsg.update (DragEnterColumn(ColumnId "col-2", 0)) state

            Expect.equal
                state'.Drag
                (DragOver(CardId "card-1", ColumnId "col-1", ColumnId "col-2", 0))
                "Should be DragOver"

        testCase "DragEnterColumn short-circuits on same target and index"
        <| fun () ->
            let drag = DragOver(CardId "card-1", ColumnId "col-1", ColumnId "col-2", 0)
            let state = { twoColumnState () with Drag = drag }
            let state' = KanbanMsg.update (DragEnterColumn(ColumnId "col-2", 0)) state
            Expect.isTrue (obj.ReferenceEquals(state, state')) "Should return same state instance"

        testCase "DragEnterColumn updates when target or index changes"
        <| fun () ->
            let drag = DragOver(CardId "card-1", ColumnId "col-1", ColumnId "col-2", 0)
            let state = { twoColumnState () with Drag = drag }
            let state' = KanbanMsg.update (DragEnterColumn(ColumnId "col-2", 1)) state

            Expect.equal
                state'.Drag
                (DragOver(CardId "card-1", ColumnId "col-1", ColumnId "col-2", 1))
                "Index should update"

        testCase "DragEnterColumn ignored when NotDragging"
        <| fun () ->
            let state = twoColumnState ()
            let state' = KanbanMsg.update (DragEnterColumn(ColumnId "col-2", 0)) state
            Expect.equal state'.Drag NotDragging "Should remain NotDragging"

        testCase "DragEnd resets to NotDragging"
        <| fun () ->
            let state =
                { twoColumnState () with
                    Drag = Dragging(CardId "card-1", ColumnId "col-1")
                }

            let state' = KanbanMsg.update DragEnd state
            Expect.equal state'.Drag NotDragging "Should be NotDragging"

        testCase "DropCard moves card between columns"
        <| fun () ->
            let drag = DragOver(CardId "card-1", ColumnId "col-1", ColumnId "col-2", 0)
            let state = { twoColumnState () with Drag = drag }
            let state' = KanbanMsg.update DropCard state

            let col1 = state'.Columns |> List.find (fun c -> c.Id = ColumnId "col-1")
            let col2 = state'.Columns |> List.find (fun c -> c.Id = ColumnId "col-2")
            Expect.isFalse (col1.CardIds |> List.contains (CardId "card-1")) "Card should be removed from source"
            Expect.equal col2.CardIds [ CardId "card-1" ] "Card should be in target at index 0"
            Expect.equal state'.Drag NotDragging "Drag should be cleared"

        testCase "DropCard inserts at correct index"
        <| fun () ->
            // Move card-2 to col-2 first, then drop card-1 at index 1
            let state = twoColumnState ()

            let state' =
                state
                |> KanbanMsg.update (DragStart(CardId "card-2", ColumnId "col-1"))
                |> fun s ->
                    { s with
                        Drag = DragOver(CardId "card-2", ColumnId "col-1", ColumnId "col-2", 0)
                    }
                |> KanbanMsg.update DropCard
                |> fun s ->
                    { s with
                        Drag = DragOver(CardId "card-1", ColumnId "col-1", ColumnId "col-2", 1)
                    }
                |> KanbanMsg.update DropCard

            let col2 = state'.Columns |> List.find (fun c -> c.Id = ColumnId "col-2")
            Expect.equal col2.CardIds [ CardId "card-2"; CardId "card-1" ] "Cards should be in insertion order"

        testCase "DropCard with no DragOver resets drag"
        <| fun () ->
            let state =
                { twoColumnState () with
                    Drag = Dragging(CardId "card-1", ColumnId "col-1")
                }

            let state' = KanbanMsg.update DropCard state
            Expect.equal state'.Drag NotDragging "Should reset to NotDragging"
            Expect.equal state'.Columns state.Columns "Columns should be unchanged"
    ]

let columnTests =
    testList "Columns" [
        testCase "AddColumn appends a new column"
        <| fun () ->
            let state = twoColumnState ()
            let state' = KanbanMsg.update (AddColumn "Review") state

            Expect.equal state'.Columns.Length 3 "Should have 3 columns"
            let last = state'.Columns |> List.last
            Expect.equal last.Title "Review" "New column title should match"
            Expect.equal last.CardIds [] "New column should be empty"

        testCase "DeleteColumn removes column and its cards"
        <| fun () ->
            let state = twoColumnState ()
            let state' = KanbanMsg.update (DeleteColumn(ColumnId "col-1")) state

            Expect.equal state'.Columns.Length 1 "Should have 1 column remaining"
            Expect.isFalse (state'.Cards |> Map.containsKey (CardId "card-1")) "Card 1 should be removed"
            Expect.isFalse (state'.Cards |> Map.containsKey (CardId "card-2")) "Card 2 should be removed"

        testCase "DeleteColumn on empty column leaves cards unchanged"
        <| fun () ->
            let state = twoColumnState ()
            let state' = KanbanMsg.update (DeleteColumn(ColumnId "col-2")) state

            Expect.equal state'.Columns.Length 1 "Should have 1 column"
            Expect.equal state'.Cards.Count 2 "Cards should be unchanged"
    ]

let editTests =
    testList "Edit state" [
        testCase "StartEdit sets edit target"
        <| fun () ->
            let state = twoColumnState ()
            let card = state.Cards |> Map.find (CardId "card-1")
            let state' = KanbanMsg.update (StartEdit(EditingCard card)) state
            Expect.equal state'.Edit (EditingCard card) "Should be editing the card"

        testCase "SetEditPriority updates card in EditingCard"
        <| fun () ->
            let state = twoColumnState ()
            let card = state.Cards |> Map.find (CardId "card-1")

            let state' =
                { state with Edit = EditingCard card }
                |> KanbanMsg.update (SetEditPriority High)

            match state'.Edit with
            | EditingCard c -> Expect.equal c.Priority High "Priority should be High"
            | _ -> Expect.isFalse true "Should still be EditingCard"

        testCase "SetEditPriority ignored when not editing a card"
        <| fun () ->
            let state = twoColumnState ()
            let state' = KanbanMsg.update (SetEditPriority High) state
            Expect.equal state'.Edit NoEdit "Should remain NoEdit"

        testCase "CancelEdit resets to NoEdit"
        <| fun () ->
            let card =
                {
                    Id = CardId "card-1"
                    Title = ""
                    Description = ""
                    Priority = Medium
                }

            let state =
                { twoColumnState () with
                    Edit = EditingCard card
                }

            let state' = KanbanMsg.update CancelEdit state
            Expect.equal state'.Edit NoEdit "Should be NoEdit"
    ]

let all =
    testList "KanbanMsg.update" [
        addCardTests
        updateCardTests
        deleteCardTests
        dragDropTests
        columnTests
        editTests
    ]
