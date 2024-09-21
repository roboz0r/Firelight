namespace GettingStarted

open Fable.Core
open Fable.Core.JsInterop
open Lit
open type Lit

[<AttachMembers>]
type ClassStyling() =
    inherit LitElement()

    static member styles: CSSResult =
        css
            $$"""
        :host {
            display: block;
            background-color: lightgray;
            padding: 8px;
        }
        :host(.blue) {
            background-color: aliceblue;
            color: darkgreen;
        }
            """

    override this.render() = html $"""<p>Hello World</p>"""

[<AttachMembers>]
type SlottedStyling() =
    inherit LitElement()

    static member styles: CSSResult =
        css
            $$"""
    ::slotted(*) { font-family: Roboto; }
    ::slotted(p) { color: blue; }
    div ::slotted(*) { color: red; }
            """

    override this.render() =
        html
            $"""
      <slot></slot>
      <div><slot name="hi"></slot></div>
      """

[<AttachMembers>]
type DynamicStyling() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create
            [ "classes", (PropertyDeclaration<obj>())
              "styles2", (PropertyDeclaration<obj>()) ]

    static member styles: CSSResult =
        css
            $$"""
    .someclass { border: 1px solid red; padding: 4px; }
    .anotherclass { background-color: navy; }

            """

    member val classes: ClassInfo =
        !!{| someclass = true
             anotherclass = true |} with get, set

    member val styles2: StyleInfo =
        !!{| color = "lightgreen"
             fontFamily = "Roboto" |} with get, set

    override this.render() =
        html
            $"""
      <div class={classMap (this.classes)} style={styleMap (this.styles2)}>
        Some content
      </div>
      """

[<AttachMembers>]
type DynamicStyling2() =
    inherit LitElement()

    let classes = ClassInfo.create [ "someclass", true; "anotherclass", true ]

    let styles2 =
        StyleInfo.create [ "color", Some "lightgreen"; "fontFamily", Some "Roboto" ]

    static member properties =
        PropertyDeclarations.create
            [ "classes", (PropertyDeclaration<obj>())
              "styles2", (PropertyDeclaration<obj>()) ]

    static member styles: CSSResult =
        css
            $$"""
    .someclass { border: 1px solid red; padding: 4px; }
    .anotherclass { background-color: navy; }

            """

    override this.render() =
        html
            $"""
      <div class={classMap (classes)} style={styleMap (styles2)}>
        DynamicStyling2
      </div>
      """
