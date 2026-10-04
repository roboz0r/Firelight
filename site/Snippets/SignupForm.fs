module Snippets.SignupForm

open System.Text.RegularExpressions
open Fable.Core
open Browser.Types
open Firelight
open type Firelight.Lit

type Signup = { Name: string; Email: string }

/// Each field that has a problem, with its message, in the form's order.
let validate (form: Signup) =
    [
        if form.Name.Trim() = "" then
            "name", "Enter your name."
        if not (Regex.IsMatch(form.Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) then
            "email", "Enter an email address, such as ada@example.com."
    ]

let private field (id: string) (label: string) (kind: string) (value: string) (error: string option) setValue =
    let invalid = error |> Option.map (fun _ -> "true")
    let describedBy = error |> Option.map (fun _ -> id + "-error")

    let message =
        match error with
        | Some text -> html $"""<p class="error" id="{id}-error">{text}</p>"""
        | None -> nothing

    html
        $"""
    <label for={id}>{label}</label>
    <input id={id} type={kind} autocomplete={kind} .value={value} @input={Ev.value setValue}
        aria-invalid={ifDefined invalid} aria-describedby={ifDefined describedBy}>
    {message}"""

[<AttachMembers>]
type SignupForm() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [
            "form", PropertyDeclaration<Signup>(state = true)
            "tried", PropertyDeclaration<bool>(state = true)
            "thanks", PropertyDeclaration<string option>(state = true)
        ]

    static member styles =
        css
            $$"""
        form { display: grid; gap: 0.35rem; justify-items: start; }
        input { font: inherit; padding: 0.3rem 0.5rem; width: 16rem; max-width: 100%; }
        input[aria-invalid] { border: 2px solid var(--accent); }
        .error { margin: 0; color: var(--accent); }
        button { font: inherit; padding: 0.3rem 0.9rem; margin-top: 0.5rem; }
        p[role="status"] { margin: 0.5rem 0 0; }
        """

    member val form = { Name = ""; Email = "" } with get, set
    /// Whether the user has pressed Sign up: errors show from then on, and clear as they're fixed.
    member val tried = false with get, set
    member val thanks: string option = None with get, set

    member this.Submit(e: SubmitEvent) =
        e.preventDefault ()
        this.tried <- true

        match validate this.form with
        | [] ->
            // Send this.form to your server here.
            this.thanks <- Some $"Thanks, {this.form.Name.Trim()}. Check your inbox."
            this.form <- { Name = ""; Email = "" }
            this.tried <- false
        | (firstId, _) :: _ ->
            this.thanks <- None

            async {
                let! _ = this.updateComplete |> Async.AwaitPromise
                this.query<HTMLInputElement> ("#" + firstId) |> Option.iter _.focus()
            }
            |> Async.StartImmediate

    override this.render() =
        let errors = if this.tried then Map(validate this.form) else Map.empty

        html
            $"""
        <form novalidate @submit={Ev.submit this.Submit}>
            {field "name" "Name" "name" this.form.Name (errors.TryFind "name") (fun v -> this.form <- { this.form with Name = v })}
            {field "email" "Email" "email" this.form.Email (errors.TryFind "email") (fun v -> this.form <- { this.form with Email = v })}
            <button>Sign up</button>
        </form>
        <p role="status">{match this.thanks with
                           | Some text -> html $"{text}"
                           | None -> nothing}</p>"""

defineElement<SignupForm> "my-signup-form"
