namespace GettingStarted

open Fable.Core
open Firelight
open type Firelight.Lit

// Demonstrates styling the host element and applying a class from outside.
[<AttachMembers>]
type ClassStyling() =
    inherit LitElement()

    static member styles =
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

    override _.render() = html $"""<p>Hello World</p>"""

// Demonstrates styling content projected into slots.
[<AttachMembers>]
type SlottedStyling() =
    inherit LitElement()

    static member styles =
        css
            $$"""
        ::slotted(*) { font-family: Roboto; }
        ::slotted(p) { color: blue; }
        div ::slotted(*) { color: red; }
        """

    override _.render() =
        html
            $"""
      <slot></slot>
      <div><slot name="hi"></slot></div>
      """

// Demonstrates classMap and styleMap with dynamic values.
// Use ClassInfo.create and StyleInfo.create — these are type-safe helpers
// that avoid the need for unsafe casts.
[<AttachMembers>]
type DynamicStyling() =
    inherit LitElement()

    static member styles =
        css
            $$"""
        .outline { border: 1px solid red; padding: 4px; }
        .highlight { background-color: navy; color: white; }
        """

    member val highlighted = false with get, set

    override this.render() =
        let classes = ClassInfo.create [ "outline", true; "highlight", this.highlighted ]

        let styles =
            StyleInfo.create [ "fontFamily", Some(if this.highlighted then "Roboto" else "serif") ]

        html
            $"""
      <div class={classMap classes} style={styleMap styles}>
        {if this.highlighted then "Highlighted!" else "Normal"}
      </div>
      <button @click={fun _ -> this.highlighted <- not this.highlighted}>Toggle</button>
      """
