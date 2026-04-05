namespace GettingStarted

open Fable.Core
open Firelight
open type Firelight.Lit

module Color =
    // A CSS value constant — interpolating a CSSResult into another css tag is valid Lit CSS composition.
    let red = css $"red"

[<AttachMembers>]
type SimpleGreeting() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [ "name", PropertyDeclaration<string>() ]

    static member styles = css $""":host {{ color: {Color.red}; font-weight: bold; }}"""

    // Lit would silently generate this property accessor for us, but defining it explicitly
    // gives type-checked access and lets us provide a default value.
    // https://lit.dev/docs/components/properties/#accessors-custom
    member val name = "World" with get, set

    override this.render() =
        html $"""<div><h1>Hello, {this.name}!</h1></div>"""
