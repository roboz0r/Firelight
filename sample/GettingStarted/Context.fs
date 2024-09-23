namespace GettingStarted

open System
open Fable.Core
open Fable.Core.JS
open Lit
open Lit.Context
open type Lit.Lit
open Fable.Core.JsInterop

type MyContext =
    inherit Context<string>
    inherit symbol

module Ctx =
    let symbol = JS.Symbol("name") // A globally unique symbol that can be used as a key for a context.
    let ctx = LitContext.createContext<MyContext, _> (symbol)

[<AttachMembers>]
type MyContextElement() =
    inherit LitElement()

    let myDataCtx =
        ContextConsumer(jsThis, ContextConsumer.Options(Ctx.ctx, subscribe = true))

    override _.render() =
        let myData = myDataCtx.value
        html $"""<p>MyContextElement: {ifDefined (myData)}</p>"""

// The use of an interface here is optional, and maybe too awkward, but it avoids magic strings for the properties.
type MyContextAppProps =
    abstract myData: string with get, set

[<AttachMembers>]
type MyContextApp() =
    inherit LitElement()

    let mutable clickCount = 0
    let mutable _myData = "from app"

    let provider = ContextProvider(jsThis, ContextProvider.Options(Ctx.ctx, _myData))

    static member properties =
        PropertyDeclarations.create
            [ nameof Unchecked.defaultof<MyContextAppProps>.myData, (PropertyDeclaration<string>(attribute = !^ false)) ]

    member this.Click() =
        clickCount <- clickCount + 1
        (this :> MyContextAppProps).myData <- $"from app {clickCount}"

    interface MyContextAppProps with
        member _.myData
            with get () = _myData
            and set (value) =
                _myData <- value
                provider.setValue value

    override this.render() =
        let props = this :> MyContextAppProps

        html
            $"""
    <div>
        <p>MyContextApp</p>
        <p>{props.myData}</p>
        <button @click={this.Click}>Click me</button>
        <slot></slot>
    </div>"""

    interface ReactiveElementHost
