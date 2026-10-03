module Todo.TodoModel

type TodoItem = { Id: int; Text: string; Done: bool }

type TodoState = { Items: TodoItem list; NextId: int }

type TodoMsg =
    | AddTodo of string
    | ToggleTodo of id: int * isDone: bool
    | RemoveTodo of id: int

let init () = { Items = []; NextId = 1 }

let update (msg: TodoMsg) (state: TodoState) =
    match msg with
    | AddTodo text ->
        {
            Items =
                {
                    Id = state.NextId
                    Text = text
                    Done = false
                }
                :: state.Items
            NextId = state.NextId + 1
        }
    | ToggleTodo(id, isDone) ->
        { state with
            Items =
                state.Items
                |> List.map (fun item -> if item.Id = id then { item with Done = isDone } else item)
        }
    | RemoveTodo id ->
        { state with
            Items = state.Items |> List.filter (fun item -> item.Id <> id)
        }
