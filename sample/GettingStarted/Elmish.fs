namespace GettingStarted

open Fable.Core
open Elmish
open Thoth.Json
open Firelight
open Firelight.Elmish
open type Firelight.Lit

type CounterModel = { Count: int }

type CounterMsg =
    | Increment
    | Decrement
    | Reset
    | SetStart of int

module Counter =

    let init () = { Count = 0 }

    let update msg model =
        match msg with
        | Increment -> { model with Count = model.Count + 1 }
        | Decrement -> { model with Count = model.Count - 1 }
        | Reset -> { Count = 0 }
        | SetStart n -> { Count = n }

    let encode (model: CounterModel) : string = Encode.Auto.toString (0, model)

    let decode (json: string) : CounterModel option =
        match Decode.Auto.fromString<CounterModel> (json) with
        | Ok model -> Some model
        | Error _ -> None

[<AttachMembers>]
type Counter() as this =
    inherit LitElement()

    // ElmishController is initialised in the constructor, before Lit has applied
    // any attribute or property values from the parent template. Properties set
    // externally should therefore call into dispatch rather than trying to seed
    // init — by the time a property setter fires, the Elmish loop is already
    // running and dispatch is live.
    let elmish =
        Program.mkSimple Counter.init Counter.update (fun _ _ -> ())
        |> DevTools.withLocalStorage DevTools.DefaultDelay "gs-counter-v1" Counter.encode Counter.decode
        |> fun p -> ElmishController(this, p)

    let mutable _start = 0

    static member properties = PropertyDeclarations.create [ "start", PropertyDeclaration<int>() ]

    member _.start
        with get () = _start
        and set v =
            _start <- v
            elmish.dispatch (SetStart v)

    static member styles =
        [|
            css
                $$"""
        :host { display: inline-block; font-family: inherit; }
        .counter { display: inline-flex; align-items: center; gap: 0.5rem; }
        .count { min-width: 2.5rem; text-align: center; font-size: 1rem; font-weight: 600; }
        button {
            width: 2rem; height: 2rem;
            border: 1px solid #d4d4d8; border-radius: 0.375rem;
            background: #fff; cursor: pointer; font-size: 1rem; line-height: 1;
            transition: background-color 0.1s;
        }
        button:hover  { background: #f4f4f5; }
        button:active { background: #e4e4e7; }
        .reset {
            width: auto; padding: 0 0.625rem;
            font-size: 0.75rem; color: #71717a;
        }
        """
        |]

    override _.render() =
        let model = elmish.model

        html
            $"""
        <div class="counter">
            <button @click={fun _ -> elmish.dispatch Decrement}>−</button>
            <span class="count">{model.Count}</span>
            <button @click={fun _ -> elmish.dispatch Increment}>+</button>
            <button class="reset" @click={fun _ -> elmish.dispatch Reset}>Reset</button>
        </div>"""
