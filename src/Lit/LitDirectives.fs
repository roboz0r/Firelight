[<AutoOpen>]
module Lit.Directives

open System
open Fable.Core
open Fable.Core.JsInterop
open Browser.Types
open Fable.Core.JS

/// A key-value set of class names to truthy values.
type ClassInfo = interface end

/// <summary>
/// A key-value set of CSS properties and values.
/// </summary>
/// <remarks>
/// The key should be either a valid CSS property name string, like
/// `'background-color'`, or a valid JavaScript camel case property name
/// for CSSStyleDeclaration like `backgroundColor`.
/// </remarks>
type StyleInfo = interface end

/// A generated directive function doesn't evaluate the directive, but just
/// returns a DirectiveResult object that captures the arguments.
type DirectiveResult = interface end

type Ref<'T when 'T :> Element> =
    abstract value: 'T option

[<Erase>]
module ClassInfo =
    /// Each key in the object is treated as a class name, if the value is true,
    /// the class is added to the element's classList; if the value is false, the class is removed.
    let inline create (classes: #seq<(string * bool)>) : ClassInfo = !!(createObj !!classes)

[<Erase>]
module StyleInfo =
    /// Each key in the object is treated as a style property name, the value is treated as the value for that property.
    /// On subsequent renders, any previously set style properties that are `None` are removed.
    let inline create (styles: #seq<(string * string option)>) : StyleInfo = !!(createObj !!styles)

type KeyFn<'T, 'K when 'K: equality> = delegate of item: 'T * index: int -> 'K
type ItemTemplate<'T> = delegate of item: 'T * index: int -> Renderable

type Lit with
    /// <summary>
    /// A directive that applies dynamic CSS classes.
    /// This must be used in the class attribute and must be the only part used in the attribute.
    /// It takes each property in the classInfo argument and adds the property name to the
    /// element's classList if the property value is true; if the property value is false,
    /// the property name is removed from the element's class.
    /// </summary>
    /// <remarks>
    /// The `classMap` must be the only expression in the `class` attribute, but it can be combined with static values.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/templates/directives/#classmap"/>
    [<Import("classMap", "lit/directives/class-map.js")>]
    static member inline classMap: classInfo: ClassInfo -> DirectiveResult = nativeOnly

    /// <summary>
    /// A directive that applies CSS properties to an element.
    /// `styleMap` can only be used in the `style` attribute and must be the only expression in the attribute. It takes the property names in the `styleInfo` object and adds the properties to the inline style of the element.
    /// </summary>
    /// <remarks>
    /// Property names with dashes (-) are assumed to be valid CSS property names and set on the element's style object using `setProperty()`. Names without dashes are assumed to be camelCased JavaScript property names and set on the element's style object using property assignment, allowing the style object to translate JavaScript-style names to CSS property names.
    /// For example `styleMap({backgroundColor: 'red', 'border-top': '5px', '--size': '0'})` sets the `background-color`, `border-top` and `--size` properties.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/templates/directives/#stylemap"/>
    [<Import("styleMap", "lit/directives/style-map.js")>]
    static member inline styleMap: styleInfo: StyleInfo -> DirectiveResult = nativeOnly

    /// <summary>
    /// Repeats a series of values (usually `TemplateResults`) generated from an iterable, and updates those items efficiently when the iterable changes. When the `keyFn` is provided, key-to-DOM association is maintained between updates by moving generated DOM when required, and is generally the most efficient way to use repeat since it performs minimum unnecessary work for insertions and removals.
    /// </summary>
    /// <remarks>
    /// If you're not using a key function, you should consider using `map()`.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/templates/directives/#repeat"/>
    [<Import("repeat", "lit/directives/repeat.js")>]
    static member inline repeat(items: seq<'T>, keyFn: KeyFn<'T, 'K>, template: ItemTemplate<'T>) : DirectiveResult =
        nativeOnly

    /// <summary>
    /// Returns an iterable containing the values in items interleaved with the joiner value.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/templates/directives/#join"/>
    [<Import("join", "lit/directives/join.js")>]
    static member inline join<'T, 'U>(items: seq<'T> option, joiner: 'U) : seq<U2<'T, 'U>> = nativeOnly

    /// <summary>
    /// Sets an attribute if the value is defined and removes the attribute if undefined.
    /// </summary>
    /// <remarks>
    /// For AttributeParts, sets the attribute if the value is defined and removes the attribute
    /// if the value is undefined (`None`). For other part types, this directive is a no-op.
    ///
    /// When more than one expression exists in a single attribute value, the attribute will be
    /// removed if any expression uses `ifDefined` and evaluates to `undefined/null`. This is especially
    /// useful for setting URL attributes, when the attribute should not be set if required parts of
    /// the URL are not defined, to prevent 404's.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/templates/directives/#ifdefined"/>
    [<Import("ifDefined", "lit/directives/if-defined.js")>]
    static member inline ifDefined<'T when 'T :> Renderable>(value: 'T option) : U2<'T, nothing> = nativeOnly

    [<Import("ifDefined", "lit/directives/if-defined.js")>]
    static member inline ifDefined(value: string option) : U2<Renderable, nothing> = nativeOnly

    [<Import("ifDefined", "lit/directives/if-defined.js")>]
    static member inline ifDefined(value: float option) : U2<Renderable, nothing> = nativeOnly

    [<Import("ifDefined", "lit/directives/if-defined.js")>]
    static member inline ifDefined(value: int option) : U2<Renderable, nothing> = nativeOnly

    [<Import("ifDefined", "lit/directives/if-defined.js")>]
    static member inline ifDefined(value: HTMLElement option) : U2<Renderable, nothing> = nativeOnly

    /// <summary>
    /// Caches rendered DOM when changing templates rather than discarding the DOM. You can use this directive to optimize rendering performance when frequently switching between large templates.
    /// </summary>
    /// <remarks>
    /// When the value passed to cache changes between one or more TemplateResults, the rendered DOM nodes for a given template are cached when they're not in use. When the template changes, the directive caches the current DOM nodes before switching to the new value, and restores them from the cache when switching back to a previously-rendered value, rather than creating the DOM nodes anew.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/templates/directives/#cache"/>
    [<Import("cache", "lit/directives/cache.js")>]
    static member inline cache(value: TemplateResult option) : DirectiveResult = nativeOnly

    /// <summary>
    /// Associates a renderable value with a unique key. When the key changes, the previous DOM is removed and disposed before rendering the next value, even if the value—such as a template—is the same.
    /// </summary>
    /// <remarks>
    /// keyed is useful when you're rendering stateful elements and you need to ensure that all state of the element is cleared when some critical data changes. It essentially opts-out of Lit's default DOM reuse strategy.
    /// keyed is also useful in some animation scenarios if you need to force a new element for "enter" or "exit" animations.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/templates/directives/#keyed"/>
    [<Import("keyed", "lit/directives/keyed.js")>]
    static member inline keyed(key: obj option, value: obj option) : DirectiveResult = nativeOnly

    /// <summary>
    /// Only re-evaluates the template when one of its dependencies changes, to optimize rendering performance by preventing unnecessary work.
    /// </summary>
    /// <remarks>
    /// Renders the value returned by valueFn, and only re-evaluates valueFn when one of the dependencies changes identity.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/templates/directives/#guard"/>
    /// <param name="dependencies">
    /// An array of dependencies that, when changed, will cause the directive to re-evaluate
    /// </param>
    /// <param name="valueFn">
    /// A function that returns the value to render when the dependencies change.
    /// </param>
    [<Import("guard", "lit/directives/guard.js")>]
    static member inline guard(dependencies: array<obj option>, valueFn: unit -> obj option) : DirectiveResult =
        nativeOnly

    /// <summary>
    /// Sets an attribute or property if it differs from the live DOM value rather than the last-rendered value.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/templates/directives/#live"/>
    [<Import("live", "lit/directives/live.js")>]
    static member inline live(value: obj option) : DirectiveResult = nativeOnly

    /// <summary>
    /// Renders the content of a `&lt;template&gt;` element.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/templates/directives/#templatecontent"/>
    [<Import("templateContent", "lit/directives/template-content.js")>]
    static member inline templateContent(templateElement: HTMLTemplateElement) : DirectiveResult = nativeOnly

    /// <summary>
    /// Renders a string as HTML rather than text.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/templates/directives/#unsafehtml"/>
    [<Import("unsafeHTML ", "lit/directives/unsafe-html.js")>]
    static member inline unsafeHTML(html: string) : DirectiveResult = nativeOnly

    /// <summary>
    /// Renders a string as SVG rather than text.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/templates/directives/#unsafesvg"/>
    [<Import("unsafeSVG", "lit/directives/unsafe-svg.js")>]
    static member inline unsafeSVG(svg: string) : DirectiveResult = nativeOnly

    /// <summary>
    /// Creates a reference cell that can be used to access an element in the DOM.
    /// Use the `ref` directive to set the reference on an element.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/templates/directives/#ref"/>
    [<Import("createRef", "lit/directives/ref.js")>]
    static member inline createRef<'T when 'T :> Element>() : Ref<'T> = nativeOnly

    /// <summary>
    /// When placed on an element in the template, the ref directive will retrieve a reference to that element once rendered.
    /// The ref directive can be used to access the element in the DOM, allowing you to manipulate it directly.
    /// </summary>
    /// <remarks>
    /// After rendering, the `Ref`'s `value` property will be set to the element, where it can be accessed in post-render lifecycle like `updated`.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/templates/directives/#ref"/>
    [<Import("ref", "lit/directives/ref.js")>]
    static member inline ref<'T when 'T :> Element>(_ref: Ref<'T>) : DirectiveResult = nativeOnly

    /// <summary>
    /// The passed callback will be called each time the referenced element changes.
    /// </summary>
    /// <remarks>
    /// If a ref callback is rendered to a different element position or is removed in a subsequent render, it will first be
    /// called with `undefined`, followed by another call with the new element it was rendered to (if any). Note that in a
    /// `LitElement`, the callback will be called bound to the host element automatically.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/templates/directives/#ref"/>
    [<Import("ref", "lit/directives/ref.js")>]
    static member inline ref<'T when 'T :> Element>(callback: 'T option -> unit) : DirectiveResult = nativeOnly

    /// <summary>
    /// Renders placeholder content until one or more promises resolve.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/templates/directives/#until"/>
    [<Import("until", "lit/directives/until.js")>]
    static member inline until([<ParamArray>] values: U2<Promise<Renderable>, Renderable>[]) : DirectiveResult =
        nativeOnly

    /// <summary>
    /// Appends values from an `AsyncIterable` into the DOM as they are yielded.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/templates/directives/#asyncappend"/>
    [<Import("asyncAppend", "lit/directives/async-append.js")>]
    static member inline asyncAppend(iterable: AsyncIterable<Renderable>) : DirectiveResult = nativeOnly

    /// <summary>
    /// Appends values from an `AsyncIterable` into the DOM as they are yielded.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/templates/directives/#asyncappend"/>
    [<Import("asyncAppend", "lit/directives/async-append.js")>]
    static member inline asyncAppend(iterable: AsyncIterable<'T>, mapper: 'T -> Renderable) : DirectiveResult =
        nativeOnly

    /// <summary>
    /// Appends values from an `AsyncIterable` into the DOM as they are yielded.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/templates/directives/#asyncappend"/>
    [<Import("asyncAppend", "lit/directives/async-append.js")>]
    static member inline asyncAppend(iterable: AsyncIterable<'T>, mapper: 'T -> int -> Renderable) : DirectiveResult =
        nativeOnly

    /// <summary>
    /// Renders the latest value from an `AsyncIterable` into the DOM as it is yielded.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/templates/directives/#asyncreplace"/>
    [<Import("asyncReplace", "lit/directives/async-replace.js")>]
    static member inline asyncReplace(iterable: AsyncIterable<Renderable>) : DirectiveResult = nativeOnly

    /// <summary>
    /// Renders the latest value from an `AsyncIterable` into the DOM as it is yielded.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/templates/directives/#asyncreplace"/>
    [<Import("asyncReplace", "lit/directives/async-replace.js")>]
    static member inline asyncReplace(iterable: AsyncIterable<'T>, mapper: 'T -> Renderable) : DirectiveResult =
        nativeOnly

    /// <summary>
    /// Renders the latest value from an `AsyncIterable` into the DOM as it is yielded.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/templates/directives/#asyncreplace"/>
    [<Import("asyncReplace", "lit/directives/async-replace.js")>]
    static member inline asyncReplace(iterable: AsyncIterable<'T>, mapper: 'T -> int -> Renderable) : DirectiveResult =
        nativeOnly
