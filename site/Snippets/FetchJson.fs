module Snippets.FetchJson

open Fable.Core
open Fetch
open Firelight
open Firelight.Task
open type Firelight.Lit

/// A book as the server sends it. Nothing checks that the JSON has these fields.
type Book =
    abstract title: string
    abstract author: string
    abstract year: int

let getBooks (url: string) (signal: AbortSignal) =
    promise {
        // Fails for a response that isn't ok, such as a 404, as well as for no response.
        let! response = fetch url [ Signal signal ]
        return! response.json<Book[]> ()
    }

let private bookList (books: Book[]) =
    html $"""<ul>{books |> Array.map (fun b -> html $"<li><cite>{b.title}</cite>, {b.author} ({b.year})</li>")}</ul>"""

[<AttachMembers>]
type BookList() as this =
    inherit LitElement()

    /// Set when leaving the page cancelled a request, so coming back can make it again.
    let mutable interrupted = false

    let books =
        LitTask(
            this,
            TaskConfig(
                TaskFunction(fun (args: string[]) options ->
                    match args with
                    // U2.Case2, not !^: nothing here says if the task's result is the books or the promise.
                    | [| url |] -> U2.Case2(getBooks url options.signal)
                    // Never happens, as args holds one URL, but a match covers every length.
                    | _ -> initialState
                ),
                // A relative URL, next to this page. Yours would be your API's address.
                args = fun () -> [| if this.broken then "no-such-file.json" else "books.json" |]
            )
        )

    static member properties =
        PropertyDeclarations.create [ "broken", PropertyDeclaration<bool>(state = true) ]

    static member styles =
        css
            $$"""
        :host { display: grid; gap: 0.5rem; justify-items: start; }
        p, ul { margin: 0; }
        button { font: inherit; padding: 0.3rem 0.9rem; }
        """

    member val broken = false with get, set

    override this.disconnectedCallback() =
        base.disconnectedCallback ()
        // A task doesn't stop when its component leaves the page; abort the request ourselves.
        if books.status = TaskStatus.PENDING then
            interrupted <- true
            books.abort ()

    override this.connectedCallback() =
        base.connectedCallback ()

        if interrupted then
            interrupted <- false
            books.run () |> ignore

    override this.render() =
        let status =
            StatusRenderer(
                pending = (fun () -> html $"""<p role="status">Loading books…</p>"""),
                complete = bookList,
                error = fun e -> html $"""<p role="alert">The books didn't load. {(unbox<exn> e).Message}</p>"""
            )

        html
            $"""
        <label>
            <input type="checkbox" .checked={this.broken} @change={Ev.checked' (fun on -> this.broken <- on)}>
            Ask for a file that isn't there
        </label>
        <button @click={fun _ -> books.run () |> ignore}>Load again</button>
        {books.render status}"""

defineElement<BookList> "my-book-list"
