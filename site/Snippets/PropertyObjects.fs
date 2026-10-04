module Snippets.PropertyObjects

open Fable.Core
open Fable.Core.JsInterop
open Firelight
open type Firelight.Lit

type Player = { Name: string; Score: int }

let private rankingStyles =
    css
        $$"""
    :host { display: block; padding: 0.5rem 0.75rem; border: 1px solid var(--border); border-radius: 0.5rem; }
    h3 { margin: 0 0 0.25rem; font-size: 1rem; }
    ol { margin: 0; padding-inline-start: 1.25rem; }
    p { margin: 0.25rem 0 0; color: var(--muted); }
    """

let private ranking (title: string) (players: Player list) (renders: int) =
    html
        $"""
    <h3>{title}</h3>
    <ol>{players |> List.map (fun p -> html $"<li>{p.Name}: {p.Score}</li>")}</ol>
    <p>Renders: {renders}</p>"""

/// <my-ranking .players={players}>: Lit's default check, by identity.
[<AttachMembers>]
type Ranking() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [ "players", PropertyDeclaration<Player list>(attribute = false) ]

    static member styles = rankingStyles

    member val players: Player list = [] with get, set
    member val renders = 0 with get, set

    override this.render() =
        this.renders <- this.renders + 1
        ranking "By identity" this.players this.renders

/// The same, but a list equal to the last one isn't a change.
[<AttachMembers>]
type EqualityRanking() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [
            "players", PropertyDeclaration<Player list>(attribute = false, hasChanged = fun next prev -> next <> prev)
        ]

    static member styles = rankingStyles

    member val players: Player list = [] with get, set
    member val renders = 0 with get, set

    override this.render() =
        this.renders <- this.renders + 1
        ranking "By equality" this.players this.renders

/// Owns the scores and passes them down with `.players`.
[<AttachMembers>]
type Scoreboard() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [
            "players", PropertyDeclaration<Player list>(state = true)
            "note", PropertyDeclaration<string>(state = true)
        ]

    static member styles =
        css
            $$"""
        :host { display: grid; gap: 0.75rem; }
        .controls { display: flex; flex-wrap: wrap; gap: 0.5rem; align-items: center; }
        .boards { display: grid; grid-template-columns: repeat(auto-fit, minmax(10rem, 1fr)); gap: 0.75rem; }
        button, input { font: inherit; padding: 0.2rem 0.6rem; }
        """

    member val players = [ { Name = "Ana"; Score = 2 }; { Name = "Ben"; Score = 1 } ] with get, set
    member val note = "" with get, set

    member this.Score(name: string) =
        this.players <-
            this.players
            |> List.map (fun p -> if p.Name = name then { p with Score = p.Score + 1 } else p)

    override this.render() =
        // A new list on every render, equal to the last one unless a score changed.
        let ranked = this.players |> List.sortByDescending (fun p -> p.Score)

        html
            $"""
        <div class="controls">
            {this.players |> List.map (fun p -> html $"<button @click={fun _ -> this.Score p.Name}>+1 {p.Name}</button>")}
            <label>Note <input .value={this.note} @input={Ev.value (fun note -> this.note <- note)}></label>
        </div>
        <div class="boards">
            <my-ranking .players={ranked}></my-ranking>
            <my-equality-ranking .players={ranked}></my-equality-ranking>
        </div>"""

defineElement<Ranking> "my-ranking"
defineElement<EqualityRanking> "my-equality-ranking"
defineElement<Scoreboard> "my-scoreboard"
