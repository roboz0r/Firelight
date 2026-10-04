namespace Firelight.Context

open Fable.Core
open Browser.Types
open Firelight

module ContextProvider =
    [<AllowNullLiteral>]
    [<Global>]
    type Options<'C, 'ValueType when 'C :> Context<'ValueType>>
        [<ParamObject; Emit("$0")>]
        (context: 'C, ?initialValue: 'ValueType) =
        member val context: 'C = nativeOnly with get, set
        member val initialValue: 'ValueType option = nativeOnly with get, set

open ContextProvider

[<AllowNullLiteral>]
type ContextProviderEvent<'C, 'ValueType when 'C :> Context<'ValueType>> =
    inherit Event
    abstract context: 'C

[<AllowNullLiteral>]
type ContextProviderEventStatic =
    /// <param name="context">the context which this provider can provide</param>
    [<EmitConstructor>]
    abstract Create: context: 'C -> ContextProviderEvent<'C, 'ValueType>

/// <summary>
/// A ReactiveController which adds context provider behavior to a
/// custom element.
///
/// This controller simply listens to the <c>context-request</c> event when
/// the host is connected to the DOM and registers the received callbacks
/// against its observable Context implementation.
///
/// The host is the component, a <c>ReactiveElement</c> such as any <c>LitElement</c>: pass <c>jsThis</c>, or
/// the F# <c>this</c> with <c>as this</c> on the type. Lit needs an element, to listen for requests on.
/// </summary>
[<AllowNullLiteral>]
[<Import("ContextProvider", "@lit/context")>]
type ContextProvider<'T, 'ValueType, 'HostElement when 'T :> Context<'ValueType> and 'HostElement :> ReactiveElement>
    (host: 'HostElement, options: Options<'T, 'ValueType>) =
    inherit ValueNotifier<'ValueType>(?defaultValue = options.initialValue)
    member _.host: 'HostElement = nativeOnly

    member _.onContextRequest(ev: ContextEvent<Context<'ValueType>, 'ValueType>) : unit = nativeOnly
    /// When we get a provider request event, that means a child of this element
    /// has just woken up. If it's a provider of our context, then we may need to
    /// re-parent our subscriptions, because is a more specific provider than us
    /// for its subtree.
    member _.onProviderRequest(ev: ContextProviderEvent<Context<'ValueType>, 'ValueType>) : unit = nativeOnly
    member _.hostConnected() : unit = nativeOnly

    interface ReactiveController with
        member this.hostConnected() : unit = nativeOnly
        member this.hostDisconnected() : unit = nativeOnly
        member this.hostUpdate() : unit = nativeOnly
        member this.hostUpdated() : unit = nativeOnly
