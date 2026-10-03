namespace Firelight.Signals

open System
open Browser
open Fable.Core
open Fable.Core.JsInterop
open Firelight

/// Options for comparing successive signal values. The default uses Object.is.
type SignalEquals<'T> = delegate of oldValue: 'T * newValue: 'T -> bool

[<AllowNullLiteral; Global>]
type SignalOptions<'T> [<ParamObject; Emit("$0")>] (?equals: SignalEquals<'T>) =
    member val equals: SignalEquals<'T> option = nativeOnly with get, set

/// A writable signal from the TC39 signal polyfill re-exported by @lit-labs/signals.
[<AllowNullLiteral; Import("State", "@lit-labs/signals")>]
type SignalState<'T>(initialValue: 'T, ?options: SignalOptions<'T>) =
    member _.get() : 'T = nativeOnly
    member _.set(value: 'T) : unit = nativeOnly

/// A lazily evaluated signal derived from other signals.
[<AllowNullLiteral; Import("Computed", "@lit-labs/signals")>]
type SignalComputed<'T>(compute: unit -> 'T, ?options: SignalOptions<'T>) =
    member _.get() : 'T = nativeOnly

/// Settings for a SignalWatcher effect.
[<AllowNullLiteral; Global>]
type SignalEffectOptions [<ParamObject; Emit("$0")>] (?beforeUpdate: bool, ?manualDispose: bool) =
    member val beforeUpdate: bool option = nativeOnly with get, set
    member val manualDispose: bool option = nativeOnly with get, set

/// Bindings to @lit-labs/signals. Register an element with defineElement to
/// track signal reads during its Lit update lifecycle.
[<Erase>]
type LitSignals =
    [<Import("signal", "@lit-labs/signals")>]
    static member inline signal(value: 'T, ?options: SignalOptions<'T>) : SignalState<'T> = nativeOnly

    [<Import("computed", "@lit-labs/signals")>]
    static member inline computed(compute: unit -> 'T, ?options: SignalOptions<'T>) : SignalComputed<'T> = nativeOnly

    /// Subscribe a single template part to a signal without requesting a full element update.
    [<Import("watch", "@lit-labs/signals")>]
    static member inline watch(value: SignalState<'T>) : DirectiveResult = nativeOnly

    [<Import("watch", "@lit-labs/signals")>]
    static member inline watch(value: SignalComputed<'T>) : DirectiveResult = nativeOnly

    [<Import("html", "@lit-labs/signals")>]
    static member inline private htmlInner(strings: string[], [<ParamArray>] values: obj[]) : HTMLTemplateResult =
        nativeOnly

    [<Import("svg", "@lit-labs/signals")>]
    static member inline private svgInner(strings: string[], [<ParamArray>] values: obj[]) : SVGTemplateResult =
        nativeOnly

    /// <summary>
    /// HTML template tag that automatically applies watch to interpolated signals.
    /// </summary>
    /// <remarks>
    /// A format specifier or alignment in a hole, such as <c>{price:N2}</c>, has no effect: Lit gets the value
    /// itself. DEBUG builds throw instead. Format the value in F#: <c>{price.ToString "N2"}</c>.
    /// </remarks>
    static member inline html(fmt: FormattableString) : HTMLTemplateResult =
#if DEBUG
        TemplateChecks.noFormatSpecifiers "LitSignals.html" fmt
#endif
        LitSignals.htmlInner (fmt.GetStrings(), fmt.GetArguments())

    /// <summary>
    /// SVG template tag that automatically applies watch to interpolated signals.
    /// </summary>
    /// <remarks>
    /// A format specifier or alignment in a hole, such as <c>{price:N2}</c>, has no effect: Lit gets the value
    /// itself. DEBUG builds throw instead. Format the value in F#: <c>{price.ToString "N2"}</c>.
    /// </remarks>
    static member inline svg(fmt: FormattableString) : SVGTemplateResult =
#if DEBUG
        TemplateChecks.noFormatSpecifiers "LitSignals.svg" fmt
#endif
        LitSignals.svgInner (fmt.GetStrings(), fmt.GetArguments())

    [<Import("SignalWatcher", "@lit-labs/signals")>]
    static member inline private signalWatcher(constructor: 'TConstructor) : 'TConstructor = nativeOnly

    // The global registry rather than window.customElements, so the module also loads under Lit SSR in Node.
    [<Emit("customElements")>]
    static member inline private customElements: Browser.Types.CustomElementRegistry = nativeOnly

    /// Register a LitElement subclass with SignalWatcher applied to it.
    /// Signal reads during updates then request another update when they change.
    [<RequiresExplicitTypeArguments>]
    static member inline defineElement<'T when 'T :> LitElement>(name: string) : unit =
        LitSignals.customElements.define (name, LitSignals.signalWatcher (jsConstructor<'T>))

    /// Run an effect tied to a component registered with defineElement.
    /// Returns a function that disposes the effect. Effects are also disposed
    /// on disconnect unless manualDispose is true.
    [<Emit("$0.updateEffect($1, $2)")>]
    static member inline updateEffect
        (host: LitElement, callback: unit -> unit, ?options: SignalEffectOptions)
        : unit -> unit =
        nativeOnly
