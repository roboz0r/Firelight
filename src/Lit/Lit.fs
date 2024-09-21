namespace Lit

open System
open Browser.Types
open Fable.Core
open Fable.Core.JsInterop
open System.Collections.Generic

// LitElement should inherit HTMLElement but HTMLElement
// is still implemented as interface in Fable.Browser
[<Import("LitElement", "lit")>]
[<AbstractClass>]
type LitElement() =
    inherit ReactiveElement()
    // Lifecycle
    // https://lit.dev/docs/api/LitElement/#LitElement/lifecycle

    /// <summary>
    /// Invoked when the component is added to the document's DOM.
    /// </summary>
    /// <remarks>
    /// In `connectedCallback()` you should setup tasks that should only
    /// occur when the element is connected to the document. The most
    /// common of these is adding event listeners to nodes external to
    /// the element, like a keydown event handler added to the window.
    ///
    /// Typically, anything done in `connectedCallback()` should be undone
    /// when the element is disconnected, in `disconnectedCallback()`.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/api/LitElement/#LitElement.connectedCallback"/>
    abstract member connectedCallback: unit -> unit
    default _.connectedCallback() : unit = nativeOnly

    /// <summary>
    /// Invoked when the component is removed from the document's DOM.
    /// </summary>
    /// <remarks>
    /// This callback is the main signal to the element that it may no
    /// longer be used. disconnectedCallback() should ensure that nothing is
    /// holding a reference to the element (such as event listeners added to
    /// nodes external to the element), so that it is free to be garbage collected.
    /// An element may be re-connected after being disconnected.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/api/LitElement/#LitElement.disconnectedCallback"/>
    abstract member disconnectedCallback: unit -> unit
    default _.disconnectedCallback() : unit = nativeOnly

    // Rendering
    // https://lit.dev/docs/api/LitElement/#LitElement/rendering

    member _.createRenderRoot: unit -> U2<Element, ShadowRoot> = nativeOnly

    /// <summary>
    /// Invoked on each update to perform rendering tasks. This method
    /// may return any value renderable by lit-html's `ChildPart` - typically
    /// a `TemplateResult`. Setting properties inside this method will *not*
    /// trigger the element to update.
    /// </summary>
    abstract member render: unit -> TemplateResult

    member _.renderOptions: RenderOptions = nativeOnly

    // Updates
    // https://lit.dev/docs/api/LitElement/#LitElement/updates
    member _.update(_changedProperties: Dictionary<string, obj>) : unit = nativeOnly

type LitEventHandler<'TEvent when 'TEvent :> Event> = delegate of ev: 'TEvent -> unit

/// <summary>
/// Allows your to specify event options for an event listener.
/// </summary>
/// <seealso href="https://lit.dev/docs/components/events/#event-options-decorator"/>
[<AllowNullLiteral>]
[<Global>]
type LitEventListener<'TEvent when 'TEvent :> Event>
    [<ParamObject; Emit("$0")>]
    (handleEvent: LitEventHandler<'TEvent>, ?capture: bool, ?once: bool, ?passive: bool) =
    member val handleEvent: LitEventHandler<'TEvent> = nativeOnly with get
    member val capture: bool option = nativeOnly with get, set
    member val once: bool option = nativeOnly with get, set
    member val passive: bool option = nativeOnly with get, set

    interface AddEventListenerOptions with
        member this.capture
            with get (): bool = nativeOnly
            and set (v: bool): unit = nativeOnly

        member this.once
            with get (): bool = nativeOnly
            and set (v: bool): unit = nativeOnly

        member this.passive
            with get (): bool = nativeOnly
            and set (v: bool): unit = nativeOnly




[<Erase>]
type Lit =
    [<Import("html", "lit")>]
    static member inline private htmlInner(strs: string[], [<ParamArray>] args: obj[]) : HTMLTemplateResult = nativeOnly

    [<Import("css", "lit")>]
    static member inline private cssInner(strs: string[], [<ParamArray>] args: obj[]) : CSSResult = nativeOnly

    [<Import("svg", "lit")>]
    static member inline private svgInner(strs: string[], [<ParamArray>] args: obj[]) : SVGTemplateResult = nativeOnly

    [<Import("mathml", "lit")>]
    static member inline private mathmlInner(strs: string[], [<ParamArray>] args: obj[]) : MathMLTemplateResult =
        nativeOnly

    /// <summary>
    /// Interprets a template literal as an SVG fragment that can efficiently render to and update a container.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/api/templates/#svg"/>
    static member inline svg(fmt: FormattableString) : SVGTemplateResult =
        Lit.svgInner (fmt.GetStrings(), fmt.GetArguments())

    /// <summary>
    /// Interprets a template literal as an HTML template that can efficiently render to and update a container.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/api/templates/#html"/>
    // static member inline html(fmt: FormattableString) : HTMLTemplateResult = transform Lit.htmlInner fmt
    static member inline html(fmt: FormattableString) : HTMLTemplateResult =
        Lit.htmlInner (fmt.GetStrings(), fmt.GetArguments())

    /// <summary>
    /// A template literal tag which can be used with LitElement's styles property to set element styles.
    /// </summary>
    /// <remarks>
    /// For security reasons, only literal string values and number may be used in embedded expressions.
    /// To incorporate non-literal values `unsafeCSS` may be used inside an expression.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/api/styles/#css"/>
    static member inline css(fmt: FormattableString) : CSSResult =
        Lit.cssInner (fmt.GetStrings(), fmt.GetArguments())

    /// <summary>
    /// Interprets a template literal as MathML fragment that can efficiently render to and update a container.
    /// </summary>
    static member inline mathml(fmt: FormattableString) : MathMLTemplateResult =
        Lit.mathmlInner (fmt.GetStrings(), fmt.GetArguments())

    /// <summary>
    /// Wrap a value for interpolation in a {@linkcode css} tagged template literal.
    /// </summary>
    /// <remarks>
    /// This is unsafe because untrusted CSS text can be used to phone home
    /// or exfiltrate data to an attacker controlled site. Take care to only use
    /// this with trusted input.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/api/styles/#unsafeCSS"/>
    [<Import("unsafeCSS", "lit")>]
    static member inline unsafeCSS(value: obj) : CSSResult = nativeOnly

    /// <summary>
    /// A sentinel value that signals a ChildPart to fully clear its content.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/api/templates/#nothing"/>
    [<Import("nothing", "lit")>]
    static member inline nothing: TemplateResult = nativeOnly

    /// <summary>
    /// A sentinel value that signals a ChildPart to retain its content from the previous render.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/templates/custom-directives/#signaling-no-change"/>
    [<Import("noChange", "lit")>]
    static member inline noChange: TemplateResult = nativeOnly

    /// <summary>
    /// Renders a value, usually a lit-html TemplateResult, to the container.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/api/templates/#render"/>
    [<Import("render", "lit")>]
    static member inline render
        (template: TemplateResult, container: HTMLElement, ?renderOptions: RenderOptions)
        : RootPart =
        nativeOnly

    /// <summary>
    /// Renders a value, usually a lit-html TemplateResult, to the container.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/api/templates/#render"/>
    [<Import("render", "lit")>]
    static member inline render
        (template: TemplateResult, container: DocumentFragment, ?renderOptions: RenderOptions)
        : RootPart =
        nativeOnly

    /// Maps the name to the `LitElement` as an autonomous custom element.
    [<RequiresExplicitTypeArguments>]
    static member inline DefineCustomElement<'T when 'T :> LitElement>(name: string) =
        customElements.define (name, jsConstructor<'T>)
