namespace Lit

open System
open Browser
open Browser.Types
open Fable.Core
open Fable.Core.JsInterop
open System.Collections.Generic

/// <summary>
/// A sentinel value that signals a ChildPart to fully clear its content.
/// </summary>
/// <seealso href="https://lit.dev/docs/api/templates/#nothing"/>
type nothing =
    inherit ChildPartRenderable
    inherit symbol

/// <summary>
/// A sentinel value that signals a ChildPart to fully clear its content.
/// </summary>
/// <seealso href="https://lit.dev/docs/api/custom-directives/#noChange"/>
type noChange =
    inherit ChildPartRenderable
    inherit symbol

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
    /// a `HTMLTemplateResult`. Setting properties inside this method will *not*
    /// trigger the element to update.
    /// </summary>
    /// <remarks>
    /// <p>Updates? No. Property changes inside this method do not trigger an element update.</p>
    /// <p>Call super? Not necessary.</p>
    /// <p>Called on server? Yes.</p>
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/components/rendering/#renderable-values"/>
    /// <seealso href="https://lit.dev/docs/components/lifecycle/#render"/>
    abstract member render: unit -> ChildPartRenderable

    member _.renderOptions: RenderOptions = nativeOnly


    /// <summary>
    /// Called to update the component's DOM.
    /// </summary>
    /// <remarks>
    /// <p>Updates? No. Property changes inside this method do not trigger an element update.</p>
    /// <p>Call super? Yes. Without a super call, the element’s attributes and template will not update.</p>
    /// <p>Called on server? No.</p>
    /// </remarks>
    /// <param name="changedProperties">Map with keys that are the names of changed properties and values that are the corresponding previous values.</param>
    /// <seealso href="https://lit.dev/docs/components/lifecycle/#update"/>
    abstract member update: changedProperties: Dictionary<string, obj> -> unit
    default _.update(changedProperties: Dictionary<string, obj>) : unit = nativeOnly

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
    static member inline nothing: nothing = nativeOnly

    /// <summary>
    /// A sentinel value that signals a ChildPart to retain its content from the previous render.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/templates/custom-directives/#signaling-no-change"/>
    [<Import("noChange", "lit")>]
    static member inline noChange: noChange = nativeOnly

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

    /// <summary>
    /// Maps the name to the `LitElement` as an autonomous custom element.
    /// </summary>
    /// <remarks>
    /// Allows you to declare a custom element in HTML using the registered name.
    /// The name must be a <a href="https://developer.mozilla.org/en-US/docs/Web/API/CustomElementRegistry/define#valid_custom_element_names">valid custom element name</a>,
    /// which must start with ASCII lowercase (a-z), contain a hyphen, and not contain ASCII uppercase letters.
    /// </remarks>
    /// <typeparam name="T">The type of the custom element, which must inherit from `LitElement`.</typeparam>
    /// <param name="name">The name of the custom element. e.g. "my-element"</param>
    /// <seealso href="https://lit.dev/docs/tools/publishing/#self-define-elements"/>
    [<RequiresExplicitTypeArguments>]
    static member inline defineElement<'T when 'T :> LitElement>(name: string) =
        window.customElements.define (name, jsConstructor<'T>)
