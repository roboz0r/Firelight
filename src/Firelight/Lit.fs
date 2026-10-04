namespace Firelight

open System
open Browser
open Browser.Types
open Fable.Core
open Fable.Core.JsInterop
open System.Collections.Generic
open System.ComponentModel

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
    abstract member render: unit -> ChildRenderable

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

/// Renders into the host element itself instead of a shadow root.
/// Light DOM lets document styles, ids, labels and hash anchors work without any copying or piercing.
[<AbstractClass; AttachMembers>]
type LightDomElement() =
    inherit LitElement()

    member this.createRenderRoot() : obj = this

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

/// <summary>
/// Checks made by the template tags. Public only because inline members call it; not for direct use.
/// </summary>
[<EditorBrowsable(EditorBrowsableState.Never)>]
module TemplateChecks =
    /// <summary>
    /// Throws when a hole in <c>fmt</c> has a format specifier or alignment, such as <c>{price:N2}</c>.
    /// The template tags pass Lit each hole's value, so the format would be dropped without a word.
    /// <c>html</c>, <c>svg</c>, <c>mathml</c> and <c>css</c> call this in DEBUG builds.
    /// </summary>
    /// <param name="tag">The tag's name, for the message.</param>
    /// <param name="fmt">The interpolated string passed to the tag.</param>
    let noFormatSpecifiers (tag: string) (fmt: FormattableString) : unit =
        // Fable compiles an interpolated string with a format in any hole to an object with `fmts`,
        // one entry per hole ("" for none); without one, `fmts` is undefined.
        let formats: string[] = fmt?fmts

        if not (isNull formats) then
            match formats |> Array.tryFindIndex (fun f -> f <> "") with
            | Some i ->
                let before = fmt.GetStrings().[i]

                let before =
                    if before.Length > 40 then
                        "..." + before.Substring(before.Length - 40)
                    else
                        before

                failwith (
                    tag
                    + ": the hole after \""
                    + before
                    + "\" has the format \""
                    + formats.[i]
                    + "\", which Lit ignores: it gets the value unformatted. "
                    + (if tag = "css" then
                           "css only takes css values and numbers, so pass the number itself, or format it "
                           + "and wrap the trusted text: {unsafeCSS (price.ToString \"N2\")}."
                       else
                           "Format the value in F# instead, such as {price.ToString \"N2\"}.")
                )
            | None -> ()

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
    /// <remarks>
    /// A format specifier or alignment in a hole, such as <c>{price:N2}</c>, has no effect: Lit gets the value
    /// itself. DEBUG builds throw instead. Format the value in F#: <c>{price.ToString "N2"}</c>.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/api/templates/#svg"/>
    static member inline svg(fmt: FormattableString) : SVGTemplateResult =
#if DEBUG
        TemplateChecks.noFormatSpecifiers "svg" fmt
#endif
        Lit.svgInner (fmt.GetStrings(), fmt.GetArguments())

    /// <summary>
    /// Interprets a template literal as an HTML template that can efficiently render to and update a container.
    /// </summary>
    /// <remarks>
    /// A format specifier or alignment in a hole, such as <c>{price:N2}</c>, has no effect: Lit gets the value
    /// itself. DEBUG builds throw instead. Format the value in F#: <c>{price.ToString "N2"}</c>.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/api/templates/#html"/>
    static member inline html(fmt: FormattableString) : HTMLTemplateResult =
#if DEBUG
        TemplateChecks.noFormatSpecifiers "html" fmt
#endif
        Lit.htmlInner (fmt.GetStrings(), fmt.GetArguments())

    /// <summary>
    /// A template literal tag which can be used with LitElement's styles property to set element styles.
    /// </summary>
    /// <remarks>
    /// For security reasons, only literal string values and number may be used in embedded expressions.
    /// To incorporate non-literal values `unsafeCSS` may be used inside an expression.
    ///
    /// A format specifier or alignment in a hole, such as <c>{size:N2}</c>, has no effect: Lit gets the value
    /// itself. DEBUG builds throw instead. Pass the number itself, or format trusted text and wrap it:
    /// <c>{unsafeCSS (size.ToString "N2")}</c>.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/api/styles/#css"/>
    static member inline css(fmt: FormattableString) : CSSResult =
#if DEBUG
        TemplateChecks.noFormatSpecifiers "css" fmt
#endif
        Lit.cssInner (fmt.GetStrings(), fmt.GetArguments())

    /// <summary>
    /// Interprets a template literal as MathML fragment that can efficiently render to and update a container.
    /// </summary>
    /// <remarks>
    /// A format specifier or alignment in a hole, such as <c>{price:N2}</c>, has no effect: Lit gets the value
    /// itself. DEBUG builds throw instead. Format the value in F#: <c>{price.ToString "N2"}</c>.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/api/templates/#mathml"/>
    static member inline mathml(fmt: FormattableString) : MathMLTemplateResult =
#if DEBUG
        TemplateChecks.noFormatSpecifiers "mathml" fmt
#endif
        Lit.mathmlInner (fmt.GetStrings(), fmt.GetArguments())

    /// <summary>
    /// Wrap a value for interpolation in a <see cref="css"/> tagged template literal.
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
    /// <remarks>
    /// Its type is a subtype of <c>HTMLTemplateResult</c>, <c>SVGTemplateResult</c> and <c>MathMLTemplateResult</c>,
    /// so <c>if c then html $"..." else nothing</c> needs no annotation. When <c>nothing</c> is the first branch,
    /// annotate the result as <c>ChildRenderable</c>. In an attribute binding, <c>nothing</c> removes the attribute.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/api/templates/#nothing"/>
    [<Import("nothing", "lit")>]
    static member inline nothing: nothing = nativeOnly

    /// <summary>
    /// A sentinel value that signals a ChildPart to retain its content from the previous render.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/templates/custom-directives/#signaling-no-change"/>
    [<Import("noChange", "lit")>]
    static member inline noChange: noChange = nativeOnly

    // TODO: Add overloads for renderable values
    // string, number, boolean, bigint, null/undefined (use option?), HTMLElement, or arrays/iterables of those
    // Make Renderable an erased union type with all the renderable types
    // Create a Renderable CE overloaded to yield a Renderable backed by a ResizeArray

    /// <summary>
    /// Renders a value, usually a lit-html TemplateResult, to the container.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/api/templates/#render"/>
    [<Import("render", "lit")>]
    static member inline render
        (value: ChildRenderable, container: HTMLElement, ?renderOptions: RenderOptions)
        : RootPart =
        nativeOnly

    /// <summary>
    /// Renders a value, usually a lit-html TemplateResult, to the container.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/api/templates/#render"/>
    [<Import("render", "lit")>]
    static member inline render
        (value: ChildRenderable, container: DocumentFragment, ?renderOptions: RenderOptions)
        : RootPart =
        nativeOnly

    /// <summary>
    /// The options of a reactive property whose value is a <c>'T</c>, for <c>PropertyDeclarations.create</c>:
    /// <c>"step", PropertyDeclaration&lt;float&gt;()</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unless you pass <c>type</c>, it comes from <c>'T</c>: <c>Number</c> for <c>float</c>, <c>int</c> and the other
    /// numeric types Fable compiles to JavaScript numbers (<c>float32</c>, <c>int16</c>, <c>uint16</c>,
    /// <c>uint32</c>, <c>sbyte</c>, <c>byte</c>), and <c>Boolean</c> for <c>bool</c>. So the attribute
    /// <c>step="0.5"</c> sets <c>step</c> to the number 0.5, and a present attribute sets a <c>bool</c> to true.
    /// Any other type, including <c>int64</c>, <c>decimal</c>, enums and options, gets no <c>type</c>: Lit sets the
    /// attribute's text as it is. Give those a <c>converter</c>.
    /// </para>
    /// <para>
    /// The name of the member that holds the value goes beside it in <c>PropertyDeclarations.create</c>.
    /// </para>
    /// </remarks>
    /// <param name="state">Internal state: renders on change, with no attribute.</param>
    /// <param name="attribute">The attribute's name, such as <c>"warn-at"</c>, or <c>false</c> for none.
    /// Default: the property's name in lower case.</param>
    /// <param name="noAccessor">Don't wrap the member's setter; call <c>requestUpdate</c> yourself.</param>
    /// <param name="reflect">Copy the property back to its attribute after each change.</param>
    /// <param name="useDefault">Keep the default out of the attribute, and restore it when the attribute is removed.</param>
    /// <param name="hasChanged">Decides whether a new value is a change. Default: JavaScript identity.</param>
    /// <param name="type">Lit's type hint, in place of the one from <c>'T</c>, such as <c>jsConstructor&lt;Globals.Array&gt;</c>.</param>
    /// <param name="converter">Converts the attribute to and from the property, in place of Lit's default.</param>
    /// <seealso href="https://lit.dev/docs/components/properties/#property-options"/>
    static member inline PropertyDeclaration<'T>
        (
            ?state: bool,
            ?attribute: PropertyAttribute,
            ?noAccessor: bool,
            ?reflect: bool,
            ?useDefault: bool,
            ?hasChanged: PropertyDeclaration.HasChanged<'T>,
            ?``type``: obj,
            ?converter: AttributeConverter<'T>
        ) : PropertyDeclaration<'T> =
        let options =
            PropertyDeclarationInternals.Options<'T>(
                ?state = state,
                ?attribute = attribute,
                ?noAccessor = noAccessor,
                ?reflect = reflect,
                ?useDefault = useDefault,
                ?hasChanged = hasChanged,
                ?``type`` = ``type``,
                ?converter = converter
            )

        !!(PropertyDeclarationInternals.withInferredType options (typeof<'T>.FullName))

    // The global registry rather than window.customElements: the same object in a browser, but
    // Lit SSR's DOM shim provides a global customElements in Node and no window.
    [<Emit("customElements")>]
    static member inline private customElements: CustomElementRegistry = nativeOnly

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
        Lit.customElements.define (name, jsConstructor<'T>)


    /// <summary>
    /// Applies the given styles to a `shadowRoot`.
    /// </summary>
    /// <remarks>
    /// When Shadow DOM is available but adoptedStyleSheets is not, styles are appended
    /// to the `shadowRoot` to mimic spec behavior. Note, when shimming is used, any styles
    /// that are subsequently placed into the shadowRoot should be placed before any
    /// shimmed adopted styles. This will match spec behavior that gives adopted sheets
    /// precedence over styles in shadowRoot.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/api/styles/#adoptStyles"/>
    [<Import("adoptStyles", "lit")>]
    static member inline adoptStyles(renderRoot: ShadowRoot, styles: CSSResultOrNative[]) : unit = nativeOnly

    [<Import("getCompatibleStyle", "lit")>]
    static member inline getCompatibleStyle(style: CSSResultOrNative) : CSSResultOrNative = nativeOnly

    /// Whether the current browser supports `adoptedStyleSheets`.
    [<Import("supportsAdoptingStyleSheets", "lit")>]
    static member inline supportsAdoptingStyleSheets: bool = nativeOnly
