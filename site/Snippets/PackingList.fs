module Snippets.PackingList

open Fable.Core
open Fable.Core.JsInterop
open Browser
open Firelight
open type Firelight.Lit

type Item = { Text: string; Packed: bool }

let private defaults =
    [ { Text = "Passport"; Packed = false }; { Text = "Charger"; Packed = false } ]

/// Change the version when Item changes shape, so old data is ignored rather than misread.
let private key = "firelight-cookbook-packing-v1"

/// Plain objects in, JSON out: Fable's representation of a record isn't meant for storing.
let save (items: Item list) =
    let json =
        items
        |> List.map (fun i -> {| text = i.Text; packed = i.Packed |})
        |> Array.ofList
        |> JS.JSON.stringify

    // Storage can be full or turned off; the list still works, it just isn't kept.
    try
        localStorage.setItem (key, json)
    with _ ->
        ()

/// The stored list, or None if there's none or it isn't what we wrote.
let load () : Item list option =
    let decode (o: obj) =
        if isNull o then
            None
        else
            match (o?text: obj), (o?packed: obj) with
            | (:? string as text), (:? bool as packed) -> Some { Text = text; Packed = packed }
            | _ -> None

    try
        match localStorage.getItem key with
        | null -> None
        | json ->
            match JS.JSON.parse json with
            | :? (obj array) as stored -> stored |> Array.choose decode |> List.ofArray |> Some
            | _ -> None
    with _ ->
        None

[<AttachMembers>]
type PackingList() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [
            "items", PropertyDeclaration<Item list>(state = true)
            "draft", PropertyDeclaration<string>(state = true)
        ]

    static member styles =
        css
            $$"""
        :host { display: grid; gap: 0.5rem; justify-items: start; }
        ul { margin: 0; padding: 0; list-style: none; display: grid; gap: 0.25rem; }
        li { display: flex; gap: 0.5rem; align-items: center; }
        .packed { text-decoration: line-through; color: var(--muted); }
        input, button { font: inherit; }
        input[type="text"] { padding: 0.2rem 0.4rem; }
        button { padding: 0.2rem 0.7rem; }
        """

    member val items = defaults with get, set
    member val draft = "" with get, set

    /// Every change goes through here, so every change is saved.
    member this.Change(items: Item list) =
        this.items <- items
        save items

    // Once the first update is done, so the first render matches the prerendered HTML, and the
    // change starts an update of its own.
    override this.firstUpdated _ =
        promise {
            let! _ = this.updateComplete
            load () |> Option.iter (fun items -> this.items <- items)
        }
        |> Promise.start

    member this.ItemView (index: int) (item: Item) =
        let toggle packed =
            this.Change(this.items |> List.updateAt index { item with Packed = packed })

        let remove () = this.Change(this.items |> List.removeAt index)

        html
            $"""
        <li>
            <label class={if item.Packed then "packed" else ""}>
                <input type="checkbox" .checked={item.Packed} @change={Ev.checked' toggle}> {item.Text}
            </label>
            <button aria-label="Remove {item.Text}" @click={fun _ -> remove ()}>×</button>
        </li>"""

    override this.render() =
        let add =
            Ev.submit (fun e ->
                e.preventDefault ()
                let text = this.draft.Trim()

                if text <> "" then
                    this.Change(this.items @ [ { Text = text; Packed = false } ])
                    this.draft <- ""
            )

        html
            $"""
        <ul>{this.items |> List.mapi this.ItemView}</ul>
        <form @submit={add}>
            <input type="text" aria-label="New item" .value={this.draft} @input={Ev.value (fun t -> this.draft <- t)}>
            <button>Add</button>
        </form>
        <button @click={fun _ -> this.Change defaults}>Start again</button>"""

defineElement<PackingList> "my-packing-list"
