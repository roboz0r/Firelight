namespace Todo

open System
open Fable.Core
open Fable.Core.JsInterop
open Browser
open Browser.Types
open Lit
open Lit.Context
open type Lit.Lit

open TodoModel

type TodoStateContext =
    inherit LitContext<TodoState>

type TodoDispatchContext =
    inherit LitContext<TodoMsg -> unit>

module Context =
    let stateSymbol = JS.Symbol()
    let dispatchSymbol = JS.Symbol()
    let stateCtx: TodoStateContext = LitContext.createContext (stateSymbol)
    let dispatchCtx: TodoDispatchContext = LitContext.createContext (dispatchSymbol)

type TodoItemProps =
    abstract todoId: int with get, set
    abstract item: TodoItem option with get, set

[<AttachMembers>]
type TodoItemComponent() as this =
    inherit LitElement()

    let tryFindItem2 id state =
        let items = state.Items
        List.tryFind (fun item -> item.Id = id) items

    let tryFindItem (ctx: ContextConsumer<_, _, _>) (id: int) =
        if (id > 0) then
            ctx.value |> Option.bind (tryFindItem2 id)
        else
            None

    let mutable _todoId = -1
    let mutable _item = None
    let mutable dispatch = fun _ -> console.log "TodoItemComponent.dispatch is not set"

    let stateConsumer =
        ContextConsumer(jsThis, ContextConsumer.Options(Context.stateCtx, this.StateChanged, subscribe = true))

    // dispatch can be passed in as a prop via html or found via the context
    let dispatchConsumer =
        ContextConsumer(jsThis, ContextConsumer.Options(Context.dispatchCtx, this.DispatchChanged, subscribe = true))

    static member properties =
        PropertyDeclarations.create [
            nameof Unchecked.defaultof<TodoItemProps>.todoId, (PropertyDeclaration<int>(attribute = !^ "todo-id"))
            nameof Unchecked.defaultof<TodoItemProps>.item, (PropertyDeclaration<TodoItem>(state = true))
        // "dispatch", (PropertyDeclaration<TodoMsg -> unit>(state = true))
        ]

    static member styles = [| Styling.theme |]

    member this.Props = this :> TodoItemProps

    member this.StateChanged (state: TodoState) (unsub: (unit -> unit) option) : unit =
        let id = this.Props.todoId
        this.Props.item <- tryFindItem2 id state

    member this.DispatchChanged (dispatchCb: TodoMsg -> unit) (unsub: (unit -> unit) option) : unit =
        dispatch <- dispatchCb

    override this.render() =
        let props = this :> TodoItemProps
        let id = props.todoId

        match props.item with
        | Some item ->
            html
                $"""
{LitTailwind.devModeStylesLink ()}
<span class="flex flex-row items-center">
    <input class="m-2" type="checkbox" id={id.ToString()} ?checked={item.Done} 
    @click={fun _ -> dispatch (ToggleTodo(id, not item.Done))} />
    <label class="text-red-800" for={id.ToString()}>{item.Text}</label>
    <button class={classMap (
                       ClassInfo.create [
                           "px-2 mx-2 rounded shadow bg-red-800 hover:bg-red-700 text-white", true
                           "hidden", props.item |> Option.map (fun x -> not x.Done) |> Option.defaultValue true
                       ]
                   )}
        @click={fun _ -> dispatch (RemoveTodo id)}>x</button>
</span>
            """
        | None -> html $"Error: No item found"

    interface TodoItemProps with
        member this.todoId
            with get () = _todoId
            and set (value) =
                // It probably doesn't make sense to change the todoId after the component is created
                let props = this :> TodoItemProps
                _todoId <- value
                props.item <- tryFindItem stateConsumer _todoId

        member _.item
            with get () = _item
            and set (value) = _item <- value


type TodoAppProps =
    abstract state: TodoState with get, set

[<AttachMembers>]
type TodoApp() as this =
    inherit LitElement()

    let mutable state = { Items = [] }
    let inputRef = createRef<HTMLInputElement> ()
    let btnRef = createRef<HTMLButtonElement> ()

    let stateProvider =
        ContextProvider(jsThis, ContextProvider.Options(Context.stateCtx, state))

    // Dispatch provider may be passed in as a prop via html
    // or via the context
    let dispatchProvider =
        ContextProvider(jsThis, ContextProvider.Options(Context.dispatchCtx, this.Dispatch))

    static member properties =
        PropertyDeclarations.create [
            nameof Unchecked.defaultof<TodoAppProps>.state, (PropertyDeclaration<TodoState>(state = true))
        ]

    static member styles = [| Styling.theme |]

    member private this.Props = this :> TodoAppProps

    member this.Dispatch(msg: TodoMsg) =
        let newState = TodoMsg.update msg state
        this.Props.state <- newState

    member this.AddTodo() =
        let text =
            match inputRef.value with
            | Some input ->
                let v = input.value
                input.value <- ""
                v
            | None -> "text"

        this.Dispatch(AddTodo text)

    member _.RenderItemWithDispatchProp item i : Renderable =
        html $"<todo-item todo-id={item.Id} .dispatch={this.Dispatch}></todo-item>"

    member _.RenderItem item i : Renderable =
        html $"<li><todo-item todo-id={item.Id}></todo-item></li>"

    override this.render() =
        let addDisabled =
            match inputRef.value with
            | Some input -> String.IsNullOrWhiteSpace(input.value)
            | None -> true

        html
            $"""
{LitTailwind.devModeStylesLink ()}
<div class="h-screen bg-slate-50">
    <div class="w-full flex flex-col justify-center">
        <div class="flex flex-row w-full justify-center py-4">
            <h1 class="text-3xl font-bold text-blue-800">Rob's overengineered Todo App</h1>
        </div>
        <hr class="border border-4 border-blue-800" />
        <div class="flex flex-grow flex-nowrap justify-center overflow-auto">
            <div class="m-4">
                <input {ref inputRef} class="text-blue-600" 
                    type="text" id="todo-input"
                    @input={fun (ev: Event) ->
                                match btnRef.value with
                                | Some btn -> btn.disabled <- String.IsNullOrWhiteSpace(inputRef.value.Value.value)
                                | None -> ()} />
                <button {ref btnRef} class="p-2 m-2 rounded shadow bg-green-300 hover:bg-green-400 disabled:bg-gray-300"
                    ?disabled={addDisabled}
                    @click={fun (ev: Event) ->
                                (ev.target :?> HTMLButtonElement).disabled <- true
                                this.AddTodo()}>Add</button>
                <ul class="p-2 m-2">{repeat (state.Items, (fun x _ -> x.Id), this.RenderItem)}</ul>
            </div>
        </div>
    </div>
</div>
        """

    interface TodoAppProps with
        member _.state
            with get () = state
            and set (value) =
                state <- value
                stateProvider.setValue (value)

module App =
    let start () =
        DefineCustomElement<TodoApp>("todo-app")
        DefineCustomElement<TodoItemComponent>("todo-item")

        let todo = html $"<todo-app></todo-app>"

        let el = document.getElementById ("todo-app")
        let rootPart = render (todo, el)
        ()

    do start ()
