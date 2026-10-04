module Snippets.LateProvider

open Fable.Core
open Fable.Core.JsInterop
open Browser
open Firelight
open Firelight.Context
open type Firelight.Lit

type GreetingContext =
    inherit Context<string>
    inherit symbol

let greetingContext: GreetingContext =
    LitContext.createContext (JS.Symbol "greeting")

// Keeps the requests nobody answered, and sends them again when a provider appears.
ContextRoot().attach document.body

/// Asks for the greeting as soon as it connects, before any provider exists.
[<AttachMembers>]
type LateConsumer() =
    inherit LitElement()

    // ContextRoot only keeps requests that subscribe.
    let greeting =
        ContextConsumer(jsThis, ContextConsumer.Options(greetingContext, subscribe = true))

    override _.render() =
        match greeting.value with
        | Some text -> html $"<p>{text}</p>"
        | None -> html $"<p>Waiting for a provider…</p>"

[<AttachMembers>]
type LateProvider() =
    inherit LitElement()

    let provider =
        ContextProvider(jsThis, ContextProvider.Options(greetingContext, "Hello from the provider"))

    override _.render() = html $"<slot></slot>"

/// Defines <my-late-provider> on click, so the provider upgrades after its consumer asked.
[<AttachMembers>]
type ProviderLoader() =
    inherit LitElement()

    let mutable loaded = false

    member this.Load() =
        defineElement<LateProvider> "my-late-provider"
        loaded <- true
        this.requestUpdate ()

    override this.render() =
        html $"""<button ?disabled={loaded} @click={fun _ -> this.Load()}>Define the provider</button>"""

defineElement<LateConsumer> "my-late-consumer"
defineElement<ProviderLoader> "my-provider-loader"
