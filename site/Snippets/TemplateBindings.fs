module Snippets.TemplateBindings

open Fable.Core
open Browser.Types
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type NameField() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [ "name", PropertyDeclaration<string>(state = true) ]

    static member styles =
        css
            $$"""
        :host { display: grid; gap: 0.75rem; justify-items: start; }
        input, button { font: inherit; padding: 0.3rem 0.6rem; }
        p { margin: 0; }
        """

    member val name = "" with get, set

    override this.render() =
        html
            $"""
        <label>
            Name
            <input maxlength="20" .value={this.name}
                @input={fun (e: Event) -> this.name <- (e.target :?> HTMLInputElement).value}>
        </label>
        <meter max="20" value={this.name.Length} aria-label="Characters used"></meter>
        <button ?disabled={this.name = ""} @click={fun _ -> this.name <- ""}>Clear</button>
        <p>Hello, {if this.name = "" then "whoever you are" else this.name}.</p>"""

defineElement<NameField> "my-name-field"
