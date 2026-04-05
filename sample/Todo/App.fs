namespace Todo

open System
open Fable.Core
open Fable.Core.JsInterop
open Browser
open Browser.Types
open Firelight
open Firelight.Context
open type Firelight.Lit

open TodoModel

// --- Contexts ---

type TodoStateContext =
    inherit LitContext<TodoState>

type TodoDispatchContext =
    inherit LitContext<TodoMsg -> unit>

module Context =
    let stateCtx: TodoStateContext = LitContext.createContext (JS.Symbol())
    let dispatchCtx: TodoDispatchContext = LitContext.createContext (JS.Symbol())

// --- TodoItem component ---

[<Interface>]
type TodoItemProps =
    abstract todoId: int with get, set

[<AttachMembers>]
type TodoItemComponent() =
    inherit LitElement()

    let mutable _todoId = -1
    let mutable _item: TodoItem option = None
    let mutable dispatch: TodoMsg -> unit = ignore

    // Subscribe to state — update _item whenever state changes.
    // ContextConsumer calls host.requestUpdate() after the callback when subscribe = true.
    let stateConsumer =
        ContextConsumer(
            jsThis,
            ContextConsumer.Options(
                Context.stateCtx,
                (fun state _ ->
                    if _todoId > 0 then
                        _item <- state.Items |> List.tryFind (fun x -> x.Id = _todoId)
                ),
                subscribe = true
            )
        )

    let _dispatchConsumer =
        ContextConsumer(
            jsThis,
            ContextConsumer.Options(Context.dispatchCtx, (fun dispatchFn _ -> dispatch <- dispatchFn), subscribe = true)
        )

    static member properties =
        PropertyDeclarations.create [ "todoId", PropertyDeclaration<int>(attribute = !^"todo-id") ]

    static member styles = [| Styling.theme |]

    override _.render() =
        match _item with
        | None -> html $"<span></span>"
        | Some item ->
            let id = _todoId

            html
                $"""
{Stylesheets.stylesheetLinks ()}
<div class="flex items-center gap-3 bg-slate-900 border border-slate-800 hover:border-slate-700 rounded-xl px-4 py-3 transition-colors">
    <input class="w-4 h-4 cursor-pointer accent-amber-500 shrink-0"
        type="checkbox" id={id} ?checked={item.Done}
        @click={fun _ -> dispatch (ToggleTodo(id, not item.Done))} />
    <label class={classMap (
                      ClassInfo.create [
                          "flex-1 text-sm cursor-pointer select-none transition-colors", true
                          "text-slate-500 line-through", item.Done
                          "text-slate-100", not item.Done
                      ]
                  )} for={id}>{item.Text}</label>
    <button class={classMap (
                       ClassInfo.create [
                           "text-xs px-3 py-1 rounded-lg bg-rose-600 hover:bg-rose-500 active:bg-rose-700 text-white transition-colors font-medium shrink-0",
                           true
                           "hidden", not item.Done
                       ]
                   )}
        @click={fun _ -> dispatch (RemoveTodo id)}>Remove</button>
</div>"""

    interface TodoItemProps with
        member _.todoId
            with get () = _todoId
            and set value =
                _todoId <- value
                // Look up this item's current state immediately so the first render is correct.
                _item <-
                    stateConsumer.value
                    |> Option.bind (fun state -> state.Items |> List.tryFind (fun x -> x.Id = value))

// --- TodoApp root component ---

[<AttachMembers>]
type TodoApp() as this =
    inherit LitElement()

    let mutable _state: TodoState = { Items = [] }
    let inputRef = createRef<HTMLInputElement> ()
    let btnRef = createRef<HTMLButtonElement> ()

    let stateProvider =
        ContextProvider(jsThis, ContextProvider.Options(Context.stateCtx, _state))

    // Provide a stable function reference; the closure always reads the current _state.
    let _dispatchProvider =
        ContextProvider(jsThis, ContextProvider.Options(Context.dispatchCtx, fun msg -> this.Dispatch msg))

    static member styles = [| Styling.theme |]

    member this.Dispatch(msg: TodoMsg) =
        let newState = TodoMsg.update msg _state
        _state <- newState
        stateProvider.setValue newState
        this.requestUpdate ()

    member this.AddTodo() =
        match inputRef.value with
        | Some input when not (String.IsNullOrWhiteSpace input.value) ->
            this.Dispatch(AddTodo input.value)
            input.value <- ""

            match btnRef.value with
            | Some btn -> btn.disabled <- true
            | None -> ()
        | _ -> ()

    member _.RenderItem (item: TodoItem) _ : HTMLTemplateResult =
        html $"<li class=\"list-none\"><todo-item todo-id={item.Id}></todo-item></li>"

    override this.render() =
        let addDisabled =
            match inputRef.value with
            | Some input -> String.IsNullOrWhiteSpace input.value
            | None -> true

        let doneCount = _state.Items |> List.filter (fun x -> x.Done) |> List.length
        let totalCount = _state.Items.Length

        let countLabel =
            if totalCount = 0 then "No tasks yet"
            elif doneCount = totalCount then "All done!"
            else $"{doneCount} of {totalCount} done"

        html
            $"""
{Stylesheets.stylesheetLinks ()}
<div class="min-h-screen bg-slate-950 text-white">
    <div class="max-w-2xl mx-auto px-6 py-16">

        <header class="mb-10">
            <h1 class="text-4xl font-bold tracking-tight text-white">Tasks</h1>
            <p class="text-slate-400 mt-1 text-sm">{countLabel}</p>
        </header>

        <div class="flex gap-3 mb-8">
            <input {ref inputRef}
                class="flex-1 bg-slate-800 border border-slate-700 rounded-xl px-4 py-3 text-slate-100 placeholder-slate-500 focus:outline-hidden focus:ring-2 focus:ring-amber-500 focus:border-transparent transition-all text-sm"
                type="text" placeholder="What needs doing?"
                @input={fun _ ->
                            match btnRef.value with
                            | Some btn -> btn.disabled <- String.IsNullOrWhiteSpace(inputRef.value.Value.value)
                            | None -> ()} />
            <button {ref btnRef}
                class="px-6 py-3 bg-amber-500 hover:bg-amber-400 active:bg-amber-600 disabled:bg-slate-800 disabled:text-slate-600 disabled:cursor-not-allowed text-slate-950 font-semibold rounded-xl transition-colors text-sm"
                ?disabled={addDisabled}
                @click={fun _ -> this.AddTodo()}>Add</button>
        </div>

        <ul class="flex flex-col gap-2 list-none p-0 m-0">
            {repeat (_state.Items, (fun item _ -> item.Id), fun item i -> this.RenderItem item i)}
        </ul>

    </div>
</div>"""

// --- Entry point ---

module App =
    let start () =
        Lit.defineElement<TodoItemComponent> "todo-item"
        Lit.defineElement<TodoApp> "todo-app"

        render (html $"<todo-app></todo-app>", document.getElementById "todo-app")
        |> ignore

    do start ()
