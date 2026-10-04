module Snippets.AnimatedList

open Fable.Core
open Browser.Types
open Firelight
open Firelight.Motion
open type Firelight.Lit

// Fable.Browser.Dom has no matchMedia, so declare the part this needs.
type MediaQueryList =
    inherit EventTarget
    abstract matches: bool

[<Emit("window.matchMedia($0)")>]
let private matchMedia (query: string) : MediaQueryList = jsNative

let private names = [| "Ada"; "Grace"; "Alan"; "Barbara"; "Edsger"; "Frances"; "Tony"; "Radia" |]

type Person = { Id: int; Name: string }

[<AttachMembers>]
type AnimatedList() as this =
    inherit LitElement()

    let mutable nextId = 3
    let mutable stopListening = ignore

    // Options for every animate() in this component, and a switch to turn them all off.
    let motion =
        AnimateController(
            this,
            AnimateControllerOptions(
                defaultOptions =
                    MotionOptions(keyframeOptions = MotionKeyframeOptions(duration = U2.Case1 250.0, easing = "ease-out"))
            )
        )

    // A new item fades in, and the rest slide to their new places.
    let itemMotion = MotionOptions(``in`` = Motion.fadeIn, skipInitial = true)

    static member properties =
        PropertyDeclarations.create [ "people", PropertyDeclaration<Person list>(state = true) ]

    static member styles =
        css
            $$"""
        :host { display: grid; gap: 0.75rem; justify-items: start; }
        ul { margin: 0; padding: 0; list-style: none; display: grid; gap: 0.35rem; width: 14rem; }
        li { display: flex; justify-content: space-between; align-items: center; padding: 0.3rem 0.5rem 0.3rem 0.75rem;
             border: 1px solid var(--border); border-radius: 0.5rem; background: var(--bg); }
        button { font: inherit; padding: 0.2rem 0.7rem; }
        """

    member val people = [ for i in 0..2 -> { Id = i; Name = names[i] } ] with get, set

    member this.Add() =
        let person = { Id = nextId; Name = names[nextId % names.Length] }
        nextId <- nextId + 1
        // At the top, so the others visibly move down to make room.
        this.people <- person :: this.people

    /// Removing the item takes its button, and the focus, with it: move the focus to the next
    /// item's button, or to Add someone.
    member this.Remove(id: int) =
        match this.people |> List.tryFindIndex (fun p -> p.Id = id) with
        | None -> ()
        | Some index ->
            this.people <- this.people |> List.filter (fun p -> p.Id <> id)

            let next =
                match List.tryItem (min index (this.people.Length - 1)) this.people with
                | Some p -> $"#remove-{p.Id}"
                | None -> "#add"

            async {
                let! _ = this.updateComplete |> Async.AwaitPromise
                this.query<HTMLElement> next |> Option.iter _.focus()
            }
            |> Async.StartImmediate

    // Follow the reduced-motion setting, including changes while the page is open.
    override this.connectedCallback() =
        base.connectedCallback ()
        let query = matchMedia "(prefers-reduced-motion: reduce)"

        let apply () =
            motion.disabled <- query.matches

            if query.matches then
                motion.finish ()

        apply ()
        stopListening <- Ev.listen query "change" (Ev.event (fun _ -> apply ()))

    override this.disconnectedCallback() =
        base.disconnectedCallback ()
        stopListening ()

    member this.PersonView(person: Person) =
        html
            $"""
        <li {Motion.animate itemMotion}>
            {person.Name}
            <button id="remove-{person.Id}" aria-label="Remove {person.Name}" @click={fun _ -> this.Remove person.Id}>×</button>
        </li>"""

    override this.render() =
        html
            $"""
        <button id="add" @click={fun _ -> this.Add()}>Add someone</button>
        <ul>{repeat (this.people, (fun p -> p.Id), this.PersonView)}</ul>"""

defineElement<AnimatedList> "my-animated-list"
