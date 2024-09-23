module Todo.TodoModel

type TodoItem = { Id: int; Text: string; Done: bool }

type TodoState = { Items: TodoItem list }

type TodoMsg =
    | AddTodo of string
    | ToggleTodo of id: int * isDone: bool
    | RemoveTodo of id: int

module TodoMsg =

    let nextId =
        let mutable i = 0

        fun () ->
            i <- i + 1
            i

    let update (msg: TodoMsg) (state: TodoState) =
        match msg with
        | AddTodo text ->
            {
                Items =
                    {
                        Id = nextId ()
                        Text = text
                        Done = false
                    }
                    :: state.Items
            }
        | ToggleTodo(id, isDone) ->
            {
                Items =
                    state.Items
                    |> List.map (fun item -> if item.Id = id then { item with Done = isDone } else item)
            }
        | RemoveTodo id ->
            {
                Items =
                    state.Items
                    |> List.filter (fun item -> item.Id <> id)
            }
