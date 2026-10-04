module Snippets.ThemePicker

open Fable.Core
open Browser
open Firelight
open type Firelight.Lit

let private key = "theme"

let private choices = [ "system", "System"; "light", "Light"; "dark", "Dark" ]

[<AttachMembers>]
type ThemePicker() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [ "theme", PropertyDeclaration<string>(state = true) ]

    static member styles =
        css
            $$"""
        :host { display: grid; gap: 0.75rem; justify-items: start; }
        fieldset { display: flex; gap: 1rem; border: 1px solid var(--border); border-radius: 0.5rem; }
        /* The palette: each colour has a light and a dark value, picked by color-scheme. */
        .preview {
            --paper: light-dark(#fffdf9, #17151a);
            --ink: light-dark(#1f1d1a, #ece8e3);
            --line: light-dark(#e8e1d8, #3d3840);
            --accent: light-dark(#c2410c, #ff8a3d);
            color-scheme: light dark;
            background: var(--paper); color: var(--ink);
            border: 1px solid var(--line); border-radius: 0.5rem; padding: 0.75rem 1rem; width: 16rem;
        }
        .preview.light { color-scheme: light; }
        .preview.dark { color-scheme: dark; }
        .preview p { margin: 0 0 0.5rem; }
        .preview a { color: var(--accent); }
        """

    member val theme = "system" with get, set

    member this.Choose(theme: string) =
        this.theme <- theme

        try
            localStorage.setItem (key, theme)
        with _ ->
            ()

    member this.Restore() =
        try
            match localStorage.getItem key with
            | "light"
            | "dark" as theme -> this.theme <- theme
            | _ -> ()
        with _ ->
            ()

    // Once the first update is done, so the first render matches the prerendered HTML, and the
    // change starts an update of its own.
    override this.firstUpdated _ =
        this.updateComplete.``then`` (fun _ -> this.Restore()) |> ignore

    override this.render() =
        let choice (value: string, label: string) =
            html
                $"""
            <label>
                <input type="radio" name="theme" value={value} .checked={this.theme = value}
                    @change={fun _ -> this.Choose value}> {label}
            </label>"""

        html
            $"""
        <fieldset>
            <legend>Theme</legend>
            {choices |> List.map choice}
        </fieldset>
        <div class="preview {this.theme}">
            <p>This box follows the theme you pick. System follows your device's setting.</p>
            <a href="#how-it-works">How it works</a>
        </div>"""

defineElement<ThemePicker> "my-theme-picker"
