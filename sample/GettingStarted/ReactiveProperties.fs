namespace GettingStarted

open Fable.Core
open Fable.Core.JsInterop
open Lit
open type Lit

[<AttachMembers>]
type MyReactive() =
    inherit LitElement()

    static member properties: PropertyDeclarations =
        PropertyDeclarations.create
            [ "active", (PropertyDeclaration<bool>(``type`` = jsConstructor<Boolean>, reflect = true)) ]

    static member styles: CSSResult =
        css
            $"""
            :host {{ 
                display: inline-block; 
            }}
            
            :host([active]) {{
                border: 1px solid red;
            }}"""

    member val active = false with get, set

    override this.render() =
        html
            $"""
      <span>Active: {this.active}</span>
      <button @click="{fun () -> (this.active <- not this.active)}">Toggle active</button>"""

[<AttachMembers>]
type CustomChangeDetection() =
    inherit LitElement()

    static member properties: PropertyDeclarations =
        PropertyDeclarations.create
            [ "value",
              (PropertyDeclaration<int>(
                  ``type`` = jsConstructor<Number>,
                  reflect = true,
                  hasChanged =
                      fun value oldValue ->
                          let hasChanged = value % 2 = 1
                          printfn "Value: %A, Old Value: %A, Has Changed: %A" value oldValue hasChanged
                          hasChanged

              )) ]

    member val value = 1 with get, set

    override this.render() =
        html
            $"""
    <p>Value: {this.value}</p>
    <p>The value increments with each click but only renders on odd numbers.</p>
    <button @click="{fun () -> (this.value <- this.value + 1)}">Increment</button>
    """
