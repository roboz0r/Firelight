namespace Lit.Context

open Fable.Core

/// A callback which is provided by a context requester and is called with the value satisfying the request.
/// This callback can be called multiple times by context providers as the requested value is changed.
type ContextCallback<'ValueType> = delegate of value: 'ValueType * ?unsubscribe: (unit -> unit) -> unit

[<AllowNullLiteral>]
[<Interface>]
type ContextRequest<'C, 'ValueType when 'C :> Context<'ValueType>> =
    abstract member context: 'C with get
    abstract member callback: ContextCallback<'ValueType> with get
    abstract member subscribe: bool option with get

/// <summary>
/// An event fired by a context requester to signal it desires a specified context with the given key.
///
/// A provider should inspect the <c>context</c> property of the event to determine if it has a value that can
/// satisfy the request, calling the <c>callback</c> with the requested value if so.
///
/// If the requested context event contains a truthy <c>subscribe</c> value, then a provider can call the callback
/// multiple times if the value is changed, if this is the case the provider should pass an <c>unsubscribe</c>
/// method to the callback which consumers can invoke to indicate they no longer wish to receive these updates.
///
/// If no <c>subscribe</c> value is present in the event, then the provider can assume that this is a 'one time'
/// request for the context and can therefore not track the consumer.
/// </summary>
/// <param name="context">the context key to request</param>
/// <param name="callback">the callback that should be invoked when the context with the specified key is available</param>
/// <param name="subscribe">when, true indicates we want to subscribe to future updates</param>
[<AllowNullLiteral>]
[<Import("ContextEvent", "@lit/context")>]
type ContextEvent<'C, 'ValueType when 'C :> Context<'ValueType>> =
    inherit Browser.Types.Event
    inherit ContextRequest<'C, 'ValueType>
    abstract member context: 'C with get
    abstract member callback: ContextCallback<'ValueType> with get
    abstract member subscribe: bool option with get
