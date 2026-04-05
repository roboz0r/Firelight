module Kanban.Templates

open Browser
open Browser.Types
open Fable.Core.JsInterop
open Firelight
open type Firelight.Lit
open KanbanModel
open DragInterop

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

let private priorityColor (p: Priority) =
    match p with
    | High -> "bg-rose-500"
    | Medium -> "bg-amber-400"
    | Low -> "bg-sky-400"

let private priorityLabel (p: Priority) =
    match p with
    | High -> "High"
    | Medium -> "Medium"
    | Low -> "Low"

/// Build a class string from a list of (classes, condition) pairs.
/// Unlike classMap, supports space-separated class groups per entry.
let private classes (pairs: (string * bool) list) =
    pairs
    |> List.choose (fun (cls, cond) -> if cond then Some cls else None)
    |> String.concat " "

/// Compute the insertion index from the mouse Y relative to cards in the column.
let calculateInsertIndex (e: Event) =
    let column = e.currentTarget :?> HTMLElement
    let cards = column.querySelectorAll "[data-card-id]"
    let mouseY = clientY e

    seq { 0 .. int cards.length - 1 }
    |> Seq.tryFindIndex (fun i ->
        let rect = (cards.[i] :?> HTMLElement).getBoundingClientRect ()
        mouseY < rect.top + rect.height / 2.0
    )
    |> Option.defaultValue (int cards.length)

// ---------------------------------------------------------------------------
// Card template
// ---------------------------------------------------------------------------

let cardTemplate
    (card: Card)
    (columnId: ColumnId)
    (isDragged: bool)
    (dispatch: KanbanMsg -> unit)
    : HTMLTemplateResult =
    let (CardId idStr) = card.Id

    let priorityBadge =
        let color = priorityColor card.Priority
        let label = priorityLabel card.Priority

        html
            $"""<span class={classes [
                                 "text-[10px] font-semibold uppercase px-1.5 py-0.5 rounded-sm text-white shrink-0", true
                                 color, true
                             ]}>{label}</span>"""

    let descHtml =
        if card.Description <> "" then
            html $"<p class=\"text-xs text-slate-500 line-clamp-2\">{card.Description}</p>"
        else
            html $""

    html
        $"""
    <div class={classes [
                    "rounded-lg p-3 bg-white shadow-xs border border-slate-200 cursor-grab active:cursor-grabbing transition-opacity select-none",
                    true
                    "opacity-30", isDragged
                ]}
        data-card-id={idStr}
        draggable="true"
        @dragstart={fun (e: Event) ->
                        setEffectAllowed "move" e
                        dispatch (DragStart(card.Id, columnId))}
        @dragend={fun _ -> dispatch DragEnd}>
        <div class="flex items-center justify-between gap-2 mb-1">
            <h3 class="font-medium text-sm text-slate-800 truncate">{card.Title}</h3>
            {priorityBadge}
        </div>
        {descHtml}
        <div class="flex justify-end mt-2 gap-1">
            <button class="text-xs px-2 py-0.5 rounded-sm text-slate-400 hover:text-slate-600 hover:bg-slate-100 transition-colors"
                @click={fun (e: Event) ->
                            e.stopPropagation ()
                            dispatch (StartEdit(EditingCard card))}>Edit</button>
            <button class="text-xs px-2 py-0.5 rounded-sm text-slate-400 hover:text-rose-600 hover:bg-rose-50 transition-colors"
                @click={fun (e: Event) ->
                            e.stopPropagation ()
                            dispatch (DeleteCard card.Id)}>Del</button>
        </div>
    </div>"""

// ---------------------------------------------------------------------------
// Drop indicator
// ---------------------------------------------------------------------------

let dropIndicatorTemplate () : HTMLTemplateResult =
    html
        $"""<div class="drop-indicator flex items-center my-0.5">
            <div class="w-2 h-2 rounded-full bg-blue-400 -mr-1 shrink-0 z-10"></div>
            <div class="h-0.5 bg-blue-400 w-full"></div>
        </div>"""

// ---------------------------------------------------------------------------
// Add-card inline form
// ---------------------------------------------------------------------------

let addCardFormTemplate (columnId: ColumnId) (dispatch: KanbanMsg -> unit) : HTMLTemplateResult =
    let inputRef = createRef<HTMLInputElement> ()

    html
        $"""
    <div class="mt-2 flex flex-col gap-2">
        <input {ref inputRef}
            class="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:outline-hidden focus:ring-2 focus:ring-blue-400 focus:border-transparent"
            type="text" placeholder="Card title..."
            @keydown={fun (e: Event) ->
                          let key: string = e?key

                          if key = "Enter" then
                              match inputRef.value with
                              | Some input when input.value.Trim() <> "" -> dispatch (AddCard(columnId, input.value.Trim()))
                              | _ -> ()
                          elif key = "Escape" then
                              dispatch CancelEdit} />
        <div class="flex gap-2">
            <button class="text-xs px-3 py-1.5 bg-blue-500 hover:bg-blue-600 text-white rounded-lg font-medium transition-colors"
                @click={fun _ ->
                            match inputRef.value with
                            | Some input when input.value.Trim() <> "" -> dispatch (AddCard(columnId, input.value.Trim()))
                            | _ -> ()}>Add</button>
            <button class="text-xs px-3 py-1.5 text-slate-500 hover:text-slate-700 transition-colors"
                @click={fun _ -> dispatch CancelEdit}>Cancel</button>
        </div>
    </div>"""

// ---------------------------------------------------------------------------
// Column header
// ---------------------------------------------------------------------------

let columnHeaderTemplate (col: Column) (cardCount: int) (dispatch: KanbanMsg -> unit) : HTMLTemplateResult =
    html
        $"""
    <div class="flex items-center justify-between mb-3 px-1">
        <div class="flex items-center gap-2">
            <h2 class="font-semibold text-sm text-slate-700">{col.Title}</h2>
            <span class="text-xs text-slate-400 bg-slate-100 px-1.5 py-0.5 rounded-full">{cardCount}</span>
        </div>
        <button class="text-slate-400 hover:text-rose-500 transition-colors text-sm"
            @click={fun _ ->
                        if window.confirm $"Delete \"{col.Title}\" and all its cards?" then
                            dispatch (DeleteColumn col.Id)}
            title="Delete column">✕</button>
    </div>"""

// ---------------------------------------------------------------------------
// Column template
// ---------------------------------------------------------------------------

let columnTemplate
    (col: Column)
    (cards: Map<CardId, Card>)
    (dragState: DragState)
    (editTarget: EditTarget)
    (dispatch: KanbanMsg -> unit)
    : HTMLTemplateResult =
    let isDropTarget =
        match dragState with
        | DragOver(_, _, targetCol, _) -> targetCol = col.Id
        | _ -> false

    let dragInsertIndex =
        match dragState with
        | DragOver(_, _, targetCol, idx) when targetCol = col.Id -> Some idx
        | _ -> None

    let draggedCardId =
        match dragState with
        | Dragging(cardId, _)
        | DragOver(cardId, _, _, _) -> Some cardId
        | NotDragging -> None

    let cardIds = col.CardIds
    let cardCount = cardIds.Length

    let isAdding =
        match editTarget with
        | AddingCard colId -> colId = col.Id
        | _ -> false

    let cardElements =
        cardIds
        |> List.mapi (fun i cardId ->
            let indicator =
                match dragInsertIndex with
                | Some idx when idx = i -> dropIndicatorTemplate ()
                | _ -> html $""

            let cardHtml =
                match cards |> Map.tryFind cardId with
                | Some card ->
                    let isDragged = draggedCardId = Some cardId
                    cardTemplate card col.Id isDragged dispatch
                | None -> html $""

            html $"{indicator}{cardHtml}"
        )
        |> Array.ofList

    let trailingIndicator =
        match dragInsertIndex with
        | Some idx when idx = cardCount -> dropIndicatorTemplate ()
        | _ -> html $""

    let addCardSection =
        if isAdding then
            addCardFormTemplate col.Id dispatch
        else
            html
                $"""
            <button class="mt-2 w-full text-left text-sm text-slate-400 hover:text-slate-600 hover:bg-slate-100 rounded-lg px-3 py-1.5 transition-colors"
                @click={fun _ -> dispatch (StartEdit(AddingCard col.Id))}>+ Add card</button>"""

    html
        $"""
    <div class={classes [
                    "flex flex-col bg-slate-50 rounded-xl p-3 min-w-[280px] max-w-[320px] w-[300px] shrink-0", true
                    "ring-2 ring-blue-400 ring-opacity-50", isDropTarget
                ]}
        @dragover={fun (e: Event) ->
                       e.preventDefault ()
                       setDropEffect "move" e
                       let idx = calculateInsertIndex e
                       dispatch (DragEnterColumn(col.Id, idx))}
        @drop={fun (e: Event) ->
                   e.preventDefault ()
                   dispatch DropCard}>
        {columnHeaderTemplate col cardCount dispatch}
        <div class="flex flex-col gap-2 overflow-y-auto max-h-[calc(100vh-14rem)] min-h-8">
            {cardElements}
            {trailingIndicator}
        </div>
        {addCardSection}
    </div>"""

// ---------------------------------------------------------------------------
// Add-column form / button
// ---------------------------------------------------------------------------

let addColumnTemplate (editTarget: EditTarget) (dispatch: KanbanMsg -> unit) : HTMLTemplateResult =
    let isAdding =
        match editTarget with
        | AddingColumn -> true
        | _ -> false

    if isAdding then
        let inputRef = createRef<HTMLInputElement> ()

        html
            $"""
        <div class="flex flex-col bg-slate-50 rounded-xl p-3 min-w-[280px] max-w-[320px] w-[300px] shrink-0">
            <input {ref inputRef}
                class="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:outline-hidden focus:ring-2 focus:ring-blue-400 focus:border-transparent"
                type="text" placeholder="Column title..."
                @keydown={fun (e: Event) ->
                              let key: string = e?key

                              if key = "Enter" then
                                  match inputRef.value with
                                  | Some input when input.value.Trim() <> "" -> dispatch (AddColumn(input.value.Trim()))
                                  | _ -> ()
                              elif key = "Escape" then
                                  dispatch CancelEdit} />
            <div class="flex gap-2 mt-2">
                <button class="text-xs px-3 py-1.5 bg-blue-500 hover:bg-blue-600 text-white rounded-lg font-medium transition-colors"
                    @click={fun _ ->
                                match inputRef.value with
                                | Some input when input.value.Trim() <> "" -> dispatch (AddColumn(input.value.Trim()))
                                | _ -> ()}>Add</button>
                <button class="text-xs px-3 py-1.5 text-slate-500 hover:text-slate-700 transition-colors"
                    @click={fun _ -> dispatch CancelEdit}>Cancel</button>
            </div>
        </div>"""
    else
        html
            $"""
        <button class="flex items-center justify-center min-w-[280px] max-w-[320px] w-[300px] shrink-0 bg-slate-100 hover:bg-slate-200 text-slate-500 hover:text-slate-700 rounded-xl p-3 border-2 border-dashed border-slate-300 hover:border-slate-400 transition-colors text-sm font-medium"
            @click={fun _ -> dispatch (StartEdit AddingColumn)}>+ Add column</button>"""

// ---------------------------------------------------------------------------
// Edit-card modal
// ---------------------------------------------------------------------------

let editCardModalTemplate (card: Card) (dispatch: KanbanMsg -> unit) : HTMLTemplateResult =
    let titleRef = createRef<HTMLInputElement> ()
    let descRef = createRef<HTMLTextAreaElement> ()

    let priorityButton (p: Priority) (current: Priority) =
        let isActive = p = current
        let color = priorityColor p
        let label = priorityLabel p

        html
            $"""
        <button type="button"
            class={classes [
                       "text-xs px-3 py-1.5 rounded-lg font-medium transition-colors border", true
                       "text-white border-transparent", isActive
                       color, isActive
                       "text-slate-600 border-slate-300 hover:border-slate-400", not isActive
                   ]}
            @click={fun _ -> dispatch (SetEditPriority p)}>{label}</button>"""

    let currentPriority = card.Priority

    html
        $"""
    <div class="fixed inset-0 bg-black/50 flex items-center justify-center z-50"
        @mousedown={fun (e: Event) ->
                        if e.target = e.currentTarget then
                            dispatch CancelEdit}>
        <div class="bg-white rounded-2xl shadow-xl p-6 w-full max-w-md mx-4"
            @mousedown={fun (e: Event) -> e.stopPropagation ()}>
            <h2 class="text-lg font-semibold text-slate-800 mb-4">Edit Card</h2>
            <div class="flex flex-col gap-3">
                <input {ref titleRef}
                    class="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:outline-hidden focus:ring-2 focus:ring-blue-400 focus:border-transparent"
                    type="text" placeholder="Title" value={card.Title} />
                <textarea {ref descRef}
                    class="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:outline-hidden focus:ring-2 focus:ring-blue-400 focus:border-transparent resize-none"
                    rows="3" placeholder="Description (optional)">{card.Description}</textarea>
                <div class="flex gap-2">
                    {[|
                         priorityButton Low currentPriority
                         priorityButton Medium currentPriority
                         priorityButton High currentPriority
                     |]}
                </div>
            </div>
            <div class="flex justify-end gap-2 mt-4">
                <button class="text-sm px-4 py-2 text-slate-500 hover:text-slate-700 transition-colors"
                    @click={fun _ -> dispatch CancelEdit}>Cancel</button>
                <button class="text-sm px-4 py-2 bg-blue-500 hover:bg-blue-600 text-white rounded-lg font-medium transition-colors"
                    @click={fun _ ->
                                let title =
                                    match titleRef.value with
                                    | Some input -> input.value.Trim()
                                    | None -> card.Title

                                let desc =
                                    match descRef.value with
                                    | Some ta -> ta.value.Trim()
                                    | None -> card.Description

                                if title <> "" then
                                    dispatch (
                                        UpdateCard
                                            { card with
                                                Title = title
                                                Description = desc
                                            }
                                    )}>Save</button>
            </div>
        </div>
    </div>"""

// ---------------------------------------------------------------------------
// Board template (top-level layout)
// ---------------------------------------------------------------------------

let boardTemplate (model: KanbanState) (dispatch: KanbanMsg -> unit) : HTMLTemplateResult =
    let columns =
        model.Columns
        |> List.map (fun col -> columnTemplate col model.Cards model.Drag model.Edit dispatch)
        |> Array.ofList

    let editModal =
        match model.Edit with
        | EditingCard card -> editCardModalTemplate card dispatch
        | _ -> html $""

    html
        $"""
    {Stylesheets.stylesheetLinks ()}
    <div class="min-h-screen bg-linear-to-br from-slate-100 to-slate-200">
        <header class="bg-white border-b border-slate-200 px-6 py-4 shadow-xs">
            <h1 class="text-xl font-bold text-slate-800 tracking-tight">Kanban Board</h1>
        </header>
        <div class="flex gap-4 p-6 overflow-x-auto items-start">
            {columns}
            {addColumnTemplate model.Edit dispatch}
        </div>
        {editModal}
    </div>"""
