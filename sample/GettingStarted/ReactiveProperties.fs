namespace GettingStarted

open Fable.Core
open Fable.Core.JsInterop
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type MyReactive() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [
            "active", PropertyDeclaration<bool>(``type`` = jsConstructor<Boolean>, reflect = true)
        ]

    // In $$""" strings, single { and } are literal — no escaping needed.
    static member styles =
        css
            $$"""
        :host {
            display: inline-block;
        }
        :host([active]) {
            border: 1px solid red;
        }"""

    member val active = false with get, set

    override this.render() =
        html
            $"""
      <span>Active: {this.active}</span>
      <button @click={fun _ -> this.active <- not this.active}>Toggle active</button>"""

[<AttachMembers>]
type CustomChangeDetection() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [
            "value",
            PropertyDeclaration<int>(
                ``type`` = jsConstructor<Number>,
                reflect = true,
                // Only re-render on odd values.
                hasChanged = fun value _ -> value % 2 = 1
            )
        ]

    member val value = 1 with get, set

    override this.render() =
        html
            $"""
    <p>Value: {this.value}</p>
    <p>The value increments with each click but only renders on odd numbers.</p>
    <button @click={fun _ -> this.value <- this.value + 1}>Increment</button>"""
