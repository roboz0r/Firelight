namespace Firelight
// reactive-element.d.ts
open Fable.Core
open Fable.Core.JsInterop
open Browser.Types
open System.Collections.Generic

module PropertyDeclaration =
    type HasChanged<'T> = delegate of value: 'T * oldValue: 'T -> bool

[<AllowNullLiteral>]
type AttributeConverter = // TODO: Implement this type
    interface end

/// <summary>
/// Defines options for a property accessor.
/// <see cref="PropertyDeclaration&lt;'Type, 'TypeHint&gt;" />
/// </summary>
[<AllowNullLiteral>]
type PropertyDeclaration = interface end

/// <summary>
/// Defines options for a property accessor.
/// </summary>
/// <seealso href="https://lit.dev/docs/components/properties/#property-options"/>
[<AllowNullLiteral>]
[<Global>]
type PropertyDeclaration<'Type>
    [<ParamObject; Emit("$0")>]
    (
        ?state: bool,
        ?attribute: U2<bool, string>,
        ?noAccessor: bool,
        ?reflect: bool,
        ?hasChanged: PropertyDeclaration.HasChanged<'Type>,
        ?``type``: obj,
        ?converter: AttributeConverter
    ) =

    interface PropertyDeclaration

    /// <summary>
    /// When set to `true`, indicates the property is internal private state. The
    /// property should not be set by users. When using TypeScript, this property
    /// should be marked as `private` or `protected`, and it is also a common
    /// practice to use a leading `_` in the name. The property is not added to
    /// `observedAttributes`.
    /// </summary>
    member val state: bool option = jsNative with get, set

    /// <summary>
    /// Indicates how and whether the property becomes an observed attribute.
    /// If the value is `false`, the property is not added to `observedAttributes`.
    /// If true or absent, the lowercased property name is observed (e.g. `fooBar`
    /// becomes `foobar`). If a string, the string value is observed (e.g
    /// `attribute: 'foo-bar'`).
    /// </summary>
    member val attribute: U2<bool, string> option = jsNative with get

    /// <summary>
    /// Indicates the type of the property. This is used only as a hint for the
    /// `converter` to determine how to convert the attribute
    /// to/from a property.
    /// use `jsConstructor` to specify a value. e.g `jsConstructor<Boolean>`.
    /// </summary>
    member val ``type``: obj option = jsNative with get

    /// <summary>
    /// Indicates how to convert the attribute to/from a property. If this value
    /// is a function, it is used to convert the attribute value a the property
    /// value. If it's an object, it can have keys for `fromAttribute` and
    /// `toAttribute`. If no `toAttribute` function is provided and
    /// `reflect` is set to `true`, the property value is set directly to the
    /// attribute. A default `converter` is used if none is provided; it supports
    /// `Boolean`, `String`, `Number`, `Object`, and `Array`. Note,
    /// when a property changes and the converter is used to update the attribute,
    /// the property is never updated again as a result of the attribute changing,
    /// and vice versa.
    /// </summary>
    member val converter: AttributeConverter option = jsNative with get

    /// <summary>
    /// Indicates if the property should reflect to an attribute.
    /// If `true`, when the property is set, the attribute is set using the
    /// attribute name determined according to the rules for the `attribute`
    /// property option and the value of the property converted using the rules
    /// from the `converter` property option.
    /// </summary>
    member val reflect: bool option = jsNative with get

    /// <summary>
    /// A function that indicates if a property should be considered changed when
    /// it is set. The function should take the `newValue` and `oldValue` and
    /// return `true` if an update should be requested.
    /// </summary>
    member val hasChanged: PropertyDeclaration.HasChanged<'Type> option = jsNative with get

    /// <summary>
    /// Indicates whether an accessor will be created for this property. By
    /// default, an accessor will be generated for this property that requests an
    /// update when set. If this flag is `true`, no accessor will be created, and
    /// it will be the user's responsibility to call
    /// `this.requestUpdate(propertyName, oldValue)` to request an update when
    /// the property changes.
    /// </summary>
    member val noAccessor: bool option = jsNative with get

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
