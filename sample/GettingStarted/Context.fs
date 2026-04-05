namespace GettingStarted

open Fable.Core
open Fable.Core.JsInterop
open Firelight
open Firelight.Context
open type Firelight.Lit

// MyContext is both a Context<string> and a symbol, which is the JS pattern
// @lit/context uses for context keys.
type MyContext =
    inherit Context<string>
    inherit symbol

module Ctx =
    let ctx: MyContext = LitContext.createContext (JS.Symbol("name"))

// A simple consumer — subscribes to the context and re-renders when the value changes.
[<AttachMembers>]
type MyContextElement() =
    inherit LitElement()

    let myDataCtx =
        ContextConsumer(jsThis, ContextConsumer.Options(Ctx.ctx, subscribe = true))

    override _.render() =
        html $"""<p>MyContextElement: {ifDefined (myDataCtx.value)}</p>"""

[<Interface>]
type MyContextAppProps =
    abstract myData: string with get, set

// A provider component that shares a string value with all descendant MyContextElements.
[<AttachMembers>]
type MyContextApp() =
    inherit LitElement()

    let mutable clickCount = 0
    let mutable _myData = "from app"
    let provider = ContextProvider(jsThis, ContextProvider.Options(Ctx.ctx, _myData))

    static member properties =
        PropertyDeclarations.create [
            nameof Unchecked.defaultof<MyContextAppProps>.myData, PropertyDeclaration<string>(attribute = !^false)
        ]

    member this.Click() =
        clickCount <- clickCount + 1
        (this :> MyContextAppProps).myData <- $"from app {clickCount}"

    interface MyContextAppProps with
        member _.myData
            with get () = _myData
            and set value =
                _myData <- value
                provider.setValue value

    override this.render() =
        html
            $"""
    <div>
        <p>MyContextApp: {(this :> MyContextAppProps).myData}</p>
        <button @click={fun _ -> this.Click()}>Click me</button>
        <slot></slot>
    </div>"""

    // Implement the ReactiveElementHost marker so this element can host a ContextProvider.
    interface ReactiveElementHost
