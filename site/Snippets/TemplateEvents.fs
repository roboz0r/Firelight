module Snippets.TemplateEvents

open Fable.Core
open Browser.Types
open Firelight
open type Firelight.Lit

/// A text box that raises `tag-added`, with the tag as its detail, when Enter is pressed.
[<AttachMembers>]
type TagInput() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [ "text", PropertyDeclaration<string>(state = true) ]

    static member styles = css $$"""input { font: inherit; padding: 0.3rem 0.6rem; }"""

    member val text = "" with get, set

    member this.Add() =
        let tag = this.text.Trim()

        if tag <> "" then
            this.dispatch (Event.customEvent ("tag-added", tag))
            this.text <- ""

    override this.render() =
        // Ev.keyboard makes `e` a KeyboardEvent, so `e.key` needs no annotation.
        let addOnEnter =
            Ev.keyboard (fun e ->
                if e.key = "Enter" then
                    this.Add()
            )

        html
            $"""
        <input aria-label="New tag" placeholder="Add a tag" .value={this.text}
            @input={Ev.value (fun text -> this.text <- text)} @keydown={addOnEnter}>"""

[<AttachMembers>]
type TagList() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [ "tags", PropertyDeclaration<string list>(state = true) ]

    static member styles =
        css
            $$"""
        :host { display: grid; gap: 0.75rem; justify-items: start; }
        ul { margin: 0; padding-left: 1.25rem; }
        """

    member val tags = [ "fsharp"; "lit" ] with get, set

    override this.render() =
        // The tag-added event's detail is the tag: a string option, as an event may have no detail.
        let addTag =
            Ev.custom<string>(fun e -> e.detail |> Option.iter (fun tag -> this.tags <- this.tags @ [ tag ]))

        html
            $"""
        <my-tag-input @tag-added={addTag}></my-tag-input>
        <ul>{this.tags |> List.map (fun tag -> html $"<li>{tag}</li>")}</ul>"""

defineElement<TagInput> "my-tag-input"
defineElement<TagList> "my-tag-list"
