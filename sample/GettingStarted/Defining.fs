namespace GettingStarted

open Fable.Core
open Lit
open type Lit.Lit

type SimpleGreetingProps =
    abstract name: obj with get, set

module Color =
    let red = css $"red"

[<AttachMembers>]
type SimpleGreeting() =
    inherit LitElement()

    static member properties: SimpleGreetingProps =
        PropertyDeclarations.create [ "name", (PropertyDeclaration<string>(state = false)) ]

    static member styles: CSSResult =
        css $""":host {{ color: {Color.red}; font-weight: bold; }}"""

    // Lit would silently generate this property for us, but it we can define it explicitly
    // to have type checked access to the property and to provide a default value.
    // https://lit.dev/docs/components/properties/#accessors-custom
    member val name = "World" with get, set

    override this.render() =
        html $"""<div><h1>Hello, {this.name}!</h1></div>"""
