namespace Firelight
// reactive-element.d.ts
open Fable.Core
open Fable.Core.JsInterop
open Browser.Types
open System.Collections.Generic
open System.ComponentModel

module PropertyDeclaration =
    type HasChanged<'T> = delegate of value: 'T * oldValue: 'T -> bool

/// <summary>
/// Converts a property to and from its attribute's text, for types Lit's default conversion doesn't
/// handle: <c>PropertyDeclaration&lt;int64&gt;(converter = AttributeConverter(fromAttribute = ..., toAttribute = ...))</c>.
/// </summary>
/// <remarks>
/// <c>fromAttribute</c> gets the attribute's text, <c>None</c> when the attribute was removed, and returns the
/// property's value. <c>toAttribute</c> runs only for a property with <c>reflect = true</c>, and returns the
/// attribute's text, <c>None</c> to remove the attribute. Leave either out to use Lit's default for that direction.
/// </remarks>
/// <seealso href="https://lit.dev/docs/components/properties/#conversion-converter"/>
[<AllowNullLiteral>]
[<Global>]
type AttributeConverter<'T>
    [<ParamObject; Emit("$0")>]
    (?fromAttribute: string option -> 'T, ?toAttribute: 'T -> string option) =
    /// Converts the attribute's text, <c>None</c> when it was removed, to the property's value.
    member val fromAttribute: (string option -> 'T) option = jsNative with get, set
    /// Converts the property's value to the attribute's text, or <c>None</c> to remove the attribute.
    member val toAttribute: ('T -> string option) option = jsNative with get, set

/// <summary>
/// The <c>attribute</c> option of a property: the attribute's name, or <c>false</c> for no attribute.
/// Write a string or a Boolean: <c>attribute = "warn-at"</c>, <c>attribute = false</c>.
/// </summary>
/// <remarks>
/// It's a string or a Boolean at run time. F# converts either to it in a method argument; <c>!^</c> from
/// <c>Fable.Core.JsInterop</c> also works, as it did when the option was a <c>U2&lt;bool, string&gt;</c>.
/// </remarks>
[<Erase>]
type PropertyAttribute =
    private
    | PropertyAttribute of obj

    /// The attribute's name, such as <c>"warn-at"</c>.
    static member inline op_Implicit(name: string) : PropertyAttribute = !!name
    /// <c>false</c> for no attribute; <c>true</c> for the default, the property's name in lower case.
    static member inline op_Implicit(enabled: bool) : PropertyAttribute = !!enabled
    /// For <c>!^"name"</c>.
    static member inline op_ErasedCast(name: string) : PropertyAttribute = !!name
    /// For <c>!^false</c>.
    static member inline op_ErasedCast(enabled: bool) : PropertyAttribute = !!enabled

/// <summary>
/// Options for a reactive property, as Lit reads them. Create one with <c>PropertyDeclaration&lt;'T&gt;(...)</c>,
/// from <c>open type Firelight.Lit</c>.
/// </summary>
[<AllowNullLiteral>]
type PropertyDeclaration = interface end

/// <summary>
/// The options of a reactive property whose value is a <c>'Type</c>. Create one with
/// <c>PropertyDeclaration&lt;'Type&gt;(...)</c>, from <c>open type Firelight.Lit</c>.
/// </summary>
/// <seealso href="https://lit.dev/docs/components/properties/#property-options"/>
[<AllowNullLiteral>]
type PropertyDeclaration<'Type> =
    inherit PropertyDeclaration

    /// <summary>
    /// When set to `true`, indicates the property is internal private state. The
    /// property should not be set by users. The property is not added to
    /// `observedAttributes`.
    /// </summary>
    abstract state: bool option

    /// <summary>
    /// Indicates how and whether the property becomes an observed attribute.
    /// If the value is `false`, the property is not added to `observedAttributes`.
    /// If true or absent, the lowercased property name is observed (e.g. `fooBar`
    /// becomes `foobar`). If a string, the string value is observed (e.g
    /// `attribute: 'foo-bar'`).
    /// </summary>
    abstract attribute: U2<bool, string> option

    /// <summary>
    /// The type Lit's default converter converts the attribute's text to: <c>Number</c>, <c>Boolean</c>,
    /// <c>Object</c> or <c>Array</c>, or none for text.
    /// </summary>
    abstract ``type``: obj option

    /// <summary>
    /// Converts the attribute to and from the property, in place of Lit's default converter.
    /// </summary>
    abstract converter: AttributeConverter<'Type> option

    /// <summary>
    /// Indicates if the property should reflect to an attribute.
    /// </summary>
    abstract reflect: bool option

    /// <summary>
    /// When `true`, the property's initial default value is not treated as a
    /// change when the property is reflected to an attribute, and removing the attribute restores it.
    /// </summary>
    abstract useDefault: bool option

    /// <summary>
    /// A function that indicates if a property should be considered changed when
    /// it is set. The function should take the `newValue` and `oldValue` and
    /// return `true` if an update should be requested.
    /// </summary>
    abstract hasChanged: PropertyDeclaration.HasChanged<'Type> option

    /// <summary>
    /// Indicates whether an accessor will be created for this property. If this flag is `true`, no
    /// accessor is created, and it's the element's job to call `this.requestUpdate(propertyName, oldValue)`.
    /// </summary>
    abstract noAccessor: bool option

/// <summary>
/// Used by <c>PropertyDeclaration&lt;'T&gt;(...)</c>. Public only because inline members call it; not for direct use.
/// </summary>
[<EditorBrowsable(EditorBrowsableState.Never)>]
module PropertyDeclarationInternals =
    [<Emit("$0 === undefined")>]
    let private isUndefined (value: obj) : bool = jsNative

    /// The options object, with the arguments that were given.
    [<Global>]
    type Options<'Type>
        [<ParamObject; Emit("$0")>]
        (
            ?state: bool,
            ?attribute: PropertyAttribute,
            ?noAccessor: bool,
            ?reflect: bool,
            ?useDefault: bool,
            ?hasChanged: PropertyDeclaration.HasChanged<'Type>,
            ?``type``: obj,
            ?converter: AttributeConverter<'Type>
        ) =
        class
        end

    /// <summary>
    /// Sets <c>type</c> from the property's F# type, named by <c>typeName</c>, unless the options already have
    /// one: <c>Number</c> for the numeric types Fable compiles to JavaScript numbers, <c>Boolean</c> for
    /// <c>bool</c>.
    /// </summary>
    let withInferredType (options: obj) (typeName: string) : obj =
        // undefined, not null: an explicit ``type`` = null is a choice to keep, as Lit's text conversion.
        if isUndefined options?``type`` then
            match typeName with
            | "System.Double"
            | "System.Single"
            | "System.Int32"
            | "System.UInt32"
            | "System.Int16"
            | "System.UInt16"
            | "System.SByte"
            | "System.Byte" -> options?``type`` <- jsConstructor<Number>
            | "System.Boolean" -> options?``type`` <- jsConstructor<Boolean>
            | _ -> ()

        options

type PropertyDeclarations = interface end

type PropertyDeclarations<'Type, 'TypeHint> =
    inherit PropertyDeclarations

    [<Emit("$0[\"$1\"]")>]
    abstract member Item: key: string -> PropertyDeclaration<'Type> option

[<Erase>]
module PropertyDeclarations =
    /// <summary>
    /// Creates a map of property declarations for a LitElement component class.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/components/properties/#property-options"/>
    let inline create (props: (string * PropertyDeclaration) seq) : 'TPropertyDeclarations = !!(createObj !!props)

/// <summary>
/// Which of a form control's validity problems <c>ElementInternals.setValidity</c> reports; leave out the ones that
/// don't apply.
/// </summary>
/// <seealso href="https://developer.mozilla.org/en-US/docs/Web/API/ElementInternals/setValidity"/>
[<AllowNullLiteral>]
[<Global>]
type ValidityStateFlags
    [<ParamObject; Emit("$0")>]
    (
        ?valueMissing: bool,
        ?typeMismatch: bool,
        ?patternMismatch: bool,
        ?tooLong: bool,
        ?tooShort: bool,
        ?rangeUnderflow: bool,
        ?rangeOverflow: bool,
        ?stepMismatch: bool,
        ?badInput: bool,
        ?customError: bool
    ) =
    class
    end

/// <summary>
/// What a custom element reports to its form and to assistive technology, from
/// <c>this.attachInternals ()</c>: its value, its validity and its ARIA role and states.
/// </summary>
/// <remarks>
/// A form-associated element also declares <c>static member formAssociated = true</c> (with
/// <c>[&lt;AttachMembers&gt;]</c>). Only the most used ARIA properties are here; set others with <c>?</c>.
/// </remarks>
/// <seealso href="https://developer.mozilla.org/en-US/docs/Web/API/ElementInternals"/>
[<AllowNullLiteral>]
type ElementInternals =
    /// The form the element belongs to, if any.
    abstract form: HTMLFormElement option
    /// The labels that name the element.
    abstract labels: NodeList
    /// The element's shadow root, if it has one.
    abstract shadowRoot: ShadowRoot option
    /// <summary>Sets what the form submits under the element's <c>name</c>; <c>null</c> submits nothing.</summary>
    abstract setFormValue: value: string -> unit
    /// <summary>Sets what the form submits, and the state the browser restores with <c>formStateRestoreCallback</c>.</summary>
    abstract setFormValue: value: string * state: string -> unit
    /// <summary>
    /// Marks the element invalid for the problems <c>flags</c> sets, with <c>message</c>; <c>anchor</c> is the
    /// element the browser points at when it reports the problem. With no problem set,
    /// <c>setValidity (ValidityStateFlags())</c>, the element is valid again.
    /// </summary>
    abstract setValidity: flags: ValidityStateFlags * ?message: string * ?anchor: HTMLElement -> unit
    /// The element's validity, as a native control's <c>validity</c>.
    abstract validity: ValidityState
    /// The message <c>setValidity</c> set.
    abstract validationMessage: string
    /// Whether the element takes part in constraint validation.
    abstract willValidate: bool
    /// Whether the element is valid; fires <c>invalid</c> on it if not.
    abstract checkValidity: unit -> bool
    /// Whether the element is valid; if not, fires <c>invalid</c> and shows the message.
    abstract reportValidity: unit -> bool
    /// <summary>The element's ARIA role, such as <c>"switch"</c>.</summary>
    abstract role: string with get, set
    /// <summary><c>"true"</c>, <c>"false"</c> or <c>"mixed"</c>.</summary>
    abstract ariaChecked: string with get, set
    abstract ariaDisabled: string with get, set
    abstract ariaExpanded: string with get, set
    abstract ariaLabel: string with get, set
    abstract ariaPressed: string with get, set
    abstract ariaSelected: string with get, set
    abstract ariaInvalid: string with get, set
    abstract ariaRequired: string with get, set
    abstract ariaValueNow: string with get, set
    abstract ariaValueMin: string with get, set
    abstract ariaValueMax: string with get, set
    abstract ariaValueText: string with get, set

/// A string representing one of the supported dev mode warning categories.
[<StringEnum>]
type WarningKind =
    | [<CompiledName("change-in-update")>] ChangeInUpdate
    | Migration

[<AllowNullLiteral>]
type PropertyDescriptor = interface end


[<Import("ReactiveElement", "@lit/reactive-element")>]
type ReactiveElement() =

    // Attributes
    // https://lit.dev/docs/api/LitElement/#LitElement/attributes

    abstract member attributeChangedCallback: name: string * _old: string option * value: string option -> unit
    default _.attributeChangedCallback(name: string, _old: string option, value: string option) = nativeOnly

    static member observedAttributes: string[] = nativeOnly

    // Controllers
    // https://lit.dev/docs/api/LitElement/#LitElement/controllers

    member _.addController: controller: ReactiveController -> unit = nativeOnly
    member _.removeController: controller: ReactiveController -> unit = nativeOnly


    // Dev mode
    // https://lit.dev/docs/api/LitElement/#LitElement/dev-mode
#if DEBUG
    static member disableWarning(warningKind: WarningKind) : unit = nativeOnly
    static member enableWarning(warningKind: WarningKind) : unit = nativeOnly
    static member enabledWarning() : WarningKind[] = nativeOnly
#endif

    // Other
    // https://lit.dev/docs/api/LitElement/#LitElement/other

    static member addInitializer(initializer: Initializer) : unit = nativeOnly
    static member finalize() : unit = nativeOnly
    static member finalized: bool = nativeOnly

    // Properties
    // https://lit.dev/docs/api/LitElement/#LitElement/properties
    static member createProperty: name: string * options: PropertyDeclaration -> unit = nativeOnly
    static member elementProperties: obj = nativeOnly

    static member getPropertyDescriptor
        : name: string * key: string * options: PropertyDeclaration -> PropertyDescriptor option =
        nativeOnly

    static member getPropertyOptions: name: string -> PropertyDeclaration option = nativeOnly

    static member properties
        with get (): PropertyDeclarations = nativeOnly
        and set (v: PropertyDeclarations) = nativeOnly

    // Rendering
    // https://lit.dev/docs/api/LitElement/#LitElement/rendering

    /// Node or ShadowRoot into which element DOM should be rendered. Defaults to an open shadowRoot.
    member _.renderRoot: U2<HTMLElement, ShadowRoot> = nativeOnly

    /// Dispatches an event from this element, e.g. `this.dispatchEvent (Event.customEvent ("changed", value))`.
    /// Returns false if a listener called preventDefault on a cancelable event.
    /// (The element is an HTMLElement at runtime; Fable.Browser models HTMLElement as an interface,
    /// so it can't be inherited here.)
    member _.dispatchEvent(event: Event) : bool = nativeOnly

    static member shadowRootOptions: ShadowRootInit = nativeOnly

    // Styles
    // https://lit.dev/docs/api/LitElement/#LitElement/styles

    /// Memoized list of all element styles. Created lazily on user subclasses when finalizing the class.
    static member elementStyles: CSSResultOrNative[] = nativeOnly

    static member finalizeStyles(?styles: CSSResultGroup) : CSSResultOrNative[] = nativeOnly

    /// <summary>
    /// Array of styles to apply to the element. The styles should be defined
    /// using the css tag function, via constructible stylesheets, or imported
    /// from native CSS module scripts.
    /// </summary>
    /// <remarks>
    /// Note on Content Security Policy: Element styles are implemented with `&lt;style&gt;`
    /// tags when the browser doesn't support adopted StyleSheets. To use such `&lt;style&gt;`
    /// tags with the style-src CSP directive, the style-src value must either include `'unsafe-inline'`
    /// or `nonce-&lt;base64-value&gt;` with `&lt;base64-value&gt;` replaced be a server-generated nonce.
    /// To provide a nonce to use on generated `&lt;style&gt;` elements, set `window.litNonce` to
    /// a server-generated nonce in your page's HTML, before loading application code:
    /// <code>
    /// &lt;script&gt;
    ///   // Generated and unique per request:
    ///  window.litNonce = 'a1b2c3d4';
    /// &lt;/script&gt;
    /// </code>
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/api/LitElement/#LitElement.styles"/>
    static member styles
        with get (): CSSResultGroup = nativeOnly
        and set (v: CSSResultGroup) = nativeOnly

    // Updates
    // https://lit.dev/docs/api/LitElement/#LitElement/updates

    member _.enableUpdating(_requestedUpdate: bool) : unit = nativeOnly

    /// <summary>
    /// Called after the component's DOM has been updated the first time, immediately before `updated()` is called.
    /// </summary>
    /// <remarks>
    /// <p>Updates? Yes. Property changes inside this method schedule a new update cycle.</p>
    /// <p>Call super? Not necessary.</p>
    /// <p>Called on server? No.</p>
    /// </remarks>
    /// <param name="changedProperties">Map with keys that are the names of changed properties and values that are the corresponding previous values.</param>
    /// <seealso href="https://lit.dev/docs/components/lifecycle/#firstupdated"/>
    abstract member firstUpdated: changedProperties: Dictionary<string, obj> -> unit
    default _.firstUpdated(changedProperties: Dictionary<string, obj>) : unit = nativeOnly

    /// <summary>
    /// To await additional conditions before fulfilling the `updateComplete` promise, override the
    /// `getUpdateComplete()` method. For example, it may be useful to await the update of a child
    /// element. First await `super.getUpdateComplete()`, then any subsequent state.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/components/lifecycle/#getUpdateComplete"/>
    abstract member getUpdateComplete: unit -> JS.Promise<bool>
    default _.getUpdateComplete() : JS.Promise<bool> = nativeOnly

    member _.hasUpdated: bool = nativeOnly
    member _.isUpdatePending: bool = nativeOnly
    member _.performUpdate() : JS.Promise<obj> option = nativeOnly
    member _.requestUpdate(_name: string, _oldValue: obj, ?options: PropertyDeclaration) : unit = nativeOnly
    member _.requestUpdate() : unit = nativeOnly

    abstract member scheduleUpdate: unit -> unit
    default _.scheduleUpdate() : unit = nativeOnly

    /// <summary>
    /// Called to determine whether an update cycle is required.
    /// </summary>
    /// <remarks>
    /// <p>Updates? No. Property changes inside this method do not trigger an element update.</p>
    /// <p>Call super? Not necessary.</p>
    /// <p>Called on server? No.</p>
    /// </remarks>
    /// <param name="changedProperties">Map with keys that are the names of changed properties and values that are the corresponding previous values.</param>
    /// <seealso href="https://lit.dev/docs/components/lifecycle/#shouldupdate"/>
    abstract member shouldUpdate: changedProperties: Dictionary<string, obj> -> bool
    default _.shouldUpdate(changedProperties: Dictionary<string, obj>) : bool = nativeOnly

    /// <summary>
    /// Returns a promise that will resolve when the element has finished updating.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/components/lifecycle/#updatecomplete"/>
    member _.updateComplete: JS.Promise<bool> = nativeOnly


    /// <summary>
    /// Called whenever the component’s update finishes and the element's DOM has been updated and rendered.
    /// </summary>
    /// <remarks>
    /// <p>Updates? Yes. Property changes inside this method schedule a new update cycle.</p>
    /// <p>Call super? Not necessary.</p>
    /// <p>Called on server? No.</p>
    /// </remarks>
    /// <param name="changedProperties">Map with keys that are the names of changed properties and values that are the corresponding previous values.</param>
    /// <seealso href="https://lit.dev/docs/components/lifecycle/#updated"/>
    abstract member updated: changedProperties: Dictionary<string, obj> -> unit
    default _.updated(changedProperties: Dictionary<string, obj>) : unit = nativeOnly

    /// <summary>
    /// Called before `update()` to compute values needed during the update.
    /// </summary>
    /// <remarks>
    /// <p>Updates? No. Property changes inside this method do not trigger an element update.</p>
    /// <p>Call super? Not necessary.</p>
    /// <p>Called on server? Yes.</p>
    /// </remarks>
    /// <param name="changedProperties">Map with keys that are the names of changed properties and values that are the corresponding previous values.</param>
    /// <seealso href="https://lit.dev/docs/components/lifecycle/#shouldupdate"/>
    abstract member willUpdate: changedProperties: Dictionary<string, obj> -> unit
    default _.willUpdate(changedProperties: Dictionary<string, obj>) : unit = nativeOnly

    member _.shadowRoot: ShadowRoot = nativeOnly
    member _.isConnected: bool = nativeOnly

    interface ReactiveControllerHost with
        member this.addController(controller: ReactiveController) = nativeOnly
        member this.removeController(controller: ReactiveController) = nativeOnly
        member this.requestUpdate() = nativeOnly
        member this.updateComplete = nativeOnly

and Initializer = delegate of element: ReactiveElement -> unit

/// Members that make common calls on a component shorter.
[<AutoOpen>]
module ReactiveElementExtensions =
    type ReactiveElement with
        /// <summary>
        /// Dispatches <c>event</c> from this element, as <c>dispatchEvent</c> does, without its result:
        /// <c>this.dispatch (Event.customEvent ("count-changed", this.count))</c>.
        /// </summary>
        /// <remarks>
        /// <c>dispatchEvent</c> returns whether the event went uncancelled. For a cancelable event, whose
        /// answer you need, call <c>dispatchEvent</c> instead.
        /// </remarks>
        member inline this.dispatch(event: Event) : unit = this.dispatchEvent event |> ignore

        /// <summary>
        /// The component as the <c>HTMLElement</c> it is in the browser, for the element's own DOM members:
        /// <c>this.element.addEventListener</c>, <c>this.element.tabIndex</c>, <c>this.element.hasAttribute "open"</c>.
        /// </summary>
        /// <remarks>
        /// <c>LitElement</c> extends <c>HTMLElement</c> in JavaScript, but Fable's browser bindings declare
        /// <c>HTMLElement</c> as an interface, which an F# class can't inherit. This is a cast that compiles to
        /// <c>this</c>.
        /// </remarks>
        member inline this.element: HTMLElement = unbox<HTMLElement> this

        /// <summary>
        /// The element's <c>ElementInternals</c>, for a form-associated component or ARIA defaults. Call it once,
        /// in the constructor: the browser throws on a second call.
        /// </summary>
        /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/attachInternals"/>
        member inline this.attachInternals() : ElementInternals = Fable.Core.JsInterop.emitJsExpr this "$0.attachInternals()"
