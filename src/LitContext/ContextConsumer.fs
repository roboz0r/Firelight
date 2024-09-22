namespace Lit.Context

open Fable.Core
open Lit

module ContextConsumer =
    [<AllowNullLiteral>]
    [<Global>]
    type Options<'C, 'ValueType when 'C :> Context<'ValueType>>
        [<ParamObject; Emit("$0")>]
        (context: 'C, ?callback: ('ValueType -> ((unit -> unit)) option -> unit), ?subscribe: bool) =
        member val context: 'C = nativeOnly with get, set
        member val callback: ('ValueType -> ((unit -> unit)) option -> unit) option = nativeOnly with get, set
        member val subscribe: bool option = nativeOnly with get, set

open ContextConsumer

/// <summary>
/// A ReactiveController which adds context consuming behavior to a custom
/// element by dispatching <c>context-request</c> events.
///
/// When the host element is connected to the document it will emit a
/// <c>context-request</c> event with its context key. When the context request
/// is satisfied the controller will invoke the callback, if present, and
/// trigger a host update so it can respond to the new value.
///
/// It will also call the dispose method given by the provider when the
/// host element is disconnected.
/// </summary>
[<AllowNullLiteral>]
[<Import("ContextConsumer", "@lit/context")>]
type ContextConsumer<'C, 'ValueType, 'HostElement
    when 'C :> Context<'ValueType> and 'HostElement :> ReactiveControllerHost and 'HostElement :> LitElement> // TODO: This should be HTMLElement
    (host: 'HostElement, options: Options<'C, 'ValueType>) =
    member val host: 'HostElement = nativeOnly with get, set
    member val value: 'ValueType option = nativeOnly with get, set
    // abstract value: ContextType<'C, 'ValueType> option with get, set
    member _.hostConnected() : unit = nativeOnly
    member _.hostDisconnected() : unit = nativeOnly

    interface ReactiveController with
        member this.hostConnected() : unit = nativeOnly
        member this.hostDisconnected() : unit = nativeOnly
        member this.hostUpdate() : unit = nativeOnly
        member this.hostUpdated() : unit = nativeOnly
