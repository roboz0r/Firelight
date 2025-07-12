namespace Firelight.Context


open System
open Fable.Core
open Fable.Core.JS
open Browser.Types

type Disposer = unit -> unit

[<AllowNullLiteral>]
type CallbackInfo =
    abstract disposer: Disposer with get, set
    abstract consumerHost: Element with get, set

/// A simple class which stores a value, and triggers registered callbacks when
/// the value is changed via its setter.
///
/// An implementor might use other observable patterns such as MobX or Redux to
/// get behavior like this. But this is a pretty minimal approach that will
/// likely work for a number of use cases.
[<AllowNullLiteral>]
[<Import("ValueNotifier", "@lit/context/lib/value-notifier.js")>]
type ValueNotifier<'T> internal (?defaultValue: 'T) =
    member internal _.subscriptions: Map<ContextCallback<'T>, CallbackInfo> = nativeOnly
    member val value: 'T = nativeOnly with get, set
    member _.setValue(v: 'T, ?force: bool) : unit = nativeOnly
    member _.updateObservers() : unit = nativeOnly
    member _.addCallback(callback: ContextCallback<'T>, consumerHost: Element, ?subscribe: bool) : unit = nativeOnly
    member _.clearCallbacks() : unit = nativeOnly
