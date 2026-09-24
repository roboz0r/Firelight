namespace Firelight.Signals

open System
open Browser
open Fable.Core
open Fable.Core.JsInterop
open Firelight

/// Options for comparing successive signal values. The default uses Object.is.
type SignalEquals<'T> = delegate of oldValue: 'T * newValue: 'T -> bool

[<AllowNullLiteral; Global>]
type SignalOptions<'T>
    [<ParamObject; Emit("$0")>]
    (?equals: SignalEquals<'T>) =
    member val equals: SignalEquals<'T> option = nativeOnly with get, set

/// A writable signal from the TC39 signal polyfill re-exported by @lit-labs/signals.
[<AllowNullLiteral; Import("State", "@lit-labs/signals")>]
type SignalState<'T>(initialValue: 'T, ?options: SignalOptions<'T>) =
    member _.get(): 'T = nativeOnly
    member _.set(value: 'T): unit = nativeOnly

/// A lazily evaluated signal derived from other signals.
[<AllowNullLiteral; Import("Computed", "@lit-labs/signals")>]
type SignalComputed<'T>(compute: unit -> 'T, ?options: SignalOptions<'T>) =
    member _.get(): 'T = nativeOnly

/// Settings for a SignalWatcher effect.
[<AllowNullLiteral; Global>]
type SignalEffectOptions
    [<ParamObject; Emit("$0")>]
    (?beforeUpdate: bool, ?manualDispose: bool) =
    member val beforeUpdate: bool option = nativeOnly with get, set
    member val manualDispose: bool option = nativeOnly with get, set

/// Bindings to @lit-labs/signals. Register an element with defineElement to
/// track signal reads during its Lit update lifecycle.
[<Erase>]
type LitSignals =
    [<Import("signal", "@lit-labs/signals")>]
    static member inline signal(value: 'T, ?options: SignalOptions<'T>): SignalState<'T> = nativeOnly

    [<Import("computed", "@lit-labs/signals")>]
    static member inline computed(compute: unit -> 'T, ?options: SignalOptions<'T>): SignalComputed<'T> = nativeOnly

    /// Subscribe a single template part to a signal without requesting a full element update.
    [<Import("watch", "@lit-labs/signals")>]
    static member inline watch(value: SignalState<'T>): DirectiveResult = nativeOnly

    [<Import("watch", "@lit-labs/signals")>]
    static member inline watch(value: SignalComputed<'T>): DirectiveResult = nativeOnly

    [<Import("html", "@lit-labs/signals")>]
    static member inline private htmlInner(strings: string[], [<ParamArray>] values: obj[]): HTMLTemplateResult = nativeOnly

    [<Import("svg", "@lit-labs/signals")>]
    static member inline private svgInner(strings: string[], [<ParamArray>] values: obj[]): SVGTemplateResult = nativeOnly

    /// HTML template tag that automatically applies watch to interpolated signals.
    static member inline html(fmt: FormattableString): HTMLTemplateResult =
        LitSignals.htmlInner(fmt.GetStrings(), fmt.GetArguments())

    /// SVG template tag that automatically applies watch to interpolated signals.
    static member inline svg(fmt: FormattableString): SVGTemplateResult =
        LitSignals.svgInner(fmt.GetStrings(), fmt.GetArguments())

    [<Import("SignalWatcher", "@lit-labs/signals")>]
    static member inline private signalWatcher(constructor: 'TConstructor): 'TConstructor = nativeOnly

    /// Register a LitElement subclass with SignalWatcher applied to it.
    /// Signal reads during updates then request another update when they change.
    [<RequiresExplicitTypeArguments>]
    static member inline defineElement<'T when 'T :> LitElement>(name: string): unit =
        window.customElements.define(name, LitSignals.signalWatcher(jsConstructor<'T>))

    /// Run an effect tied to a component registered with defineElement.
    /// Returns a function that disposes the effect. Effects are also disposed
    /// on disconnect unless manualDispose is true.
    [<Emit("$0.updateEffect($1, $2)")>]
    static member inline updateEffect(host: LitElement, callback: unit -> unit, ?options: SignalEffectOptions): unit -> unit = nativeOnly
