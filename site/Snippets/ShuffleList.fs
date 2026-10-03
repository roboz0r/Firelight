module Snippets.ShuffleList

open Fable.Core
open Firelight
open Firelight.Motion
open type Firelight.Lit

let private random = System.Random()

[<AttachMembers>]
type ShuffleList() =
    inherit LitElement()

    let mutable fruits = [ "Apple"; "Banana"; "Cherry"; "Damson"; "Elderberry" ]

    static member styles =
        css
            $$"""
        :host { display: grid; gap: 1rem; justify-items: start; }
        button { font: inherit; padding: 0.4rem 0.9rem; cursor: pointer; }
        ul { margin: 0; padding: 0; list-style: none; display: grid; gap: 0.4rem; min-width: 12rem; }
        li { padding: 0.4rem 0.75rem; border: 1px solid var(--border); border-radius: 0.5rem; background: var(--bg); }
        """

    member this.Shuffle() =
        fruits <- fruits |> List.sortBy (fun _ -> random.Next())
        this.requestUpdate ()

    // repeat keys each <li> by fruit, so the same element moves to its new position,
    // and Motion.animate() animates it there from where it was.
    override this.render() =
        html
            $"""
        <button @click={fun _ -> this.Shuffle()}>Shuffle</button>
        <ul>{repeat (fruits, (fun fruit _ -> fruit), (fun fruit _ -> html $"<li {Motion.animate ()}>{fruit}</li>"))}</ul>"""

defineElement<ShuffleList> "my-shuffle-list"
