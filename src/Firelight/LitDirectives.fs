[<AutoOpen>]
module Firelight.Directives

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

/// <summary>
/// The result of a directive Lit accepts as an element's content, such as <c>keyed</c>, <c>repeat</c> or
/// <c>until</c>. It's a <c>ChildRenderable</c>, so <c>render</c> or a template function can return it.
/// Directives that only work elsewhere, such as <c>ref</c>, <c>classMap</c> or <c>live</c>, return a plain
/// <c>DirectiveResult</c>.
/// </summary>
type ChildDirectiveResult =
    inherit DirectiveResult
    inherit ChildRenderable

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
/// A template for each item of <c>repeat</c>, of the item and its index, returning any renderable type.
/// F# converts a two-argument lambda to it.
type RepeatTemplate<'T, 'R when 'R :> ChildRenderable> = delegate of item: 'T * index: int -> 'R

/// A <c>repeat</c> template that returns a <c>ChildRenderable</c>: <c>ItemTemplate(fun item index -&gt; ...)</c>.
type ItemTemplate<'T> = RepeatTemplate<'T, ChildRenderable>

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
    static member inline repeat<'T, 'K, 'R when 'K: equality and 'R :> ChildRenderable>
        (items: seq<'T>, keyFn: KeyFn<'T, 'K>, template: RepeatTemplate<'T, 'R>)
        : ChildDirectiveResult =
        nativeOnly

    /// <summary>
    /// Repeats a series of values generated from an iterable, keeping each item's DOM with its key: the same
    /// as the overload with index, for functions of the item alone. A method fits here:
    /// <c>repeat (people, (fun p -&gt; p.Id), this.PersonView)</c>.
    /// </summary>
    /// <remarks>
    /// The other overload takes functions of the item and its index, <c>fun item index -&gt; ...</c>, which F#
    /// converts to its delegates. A method taking the item and index as a tuple converts to neither; wrap it
    /// in a lambda. Keys are compared with JavaScript's <c>===</c>: use a string or a number.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/templates/directives/#repeat"/>
    [<Import("repeat", "lit/directives/repeat.js")>]
    static member inline repeat<'T, 'K, 'R when 'K: equality and 'R :> ChildRenderable>
        (items: seq<'T>, keyFn: 'T -> 'K, template: 'T -> 'R)
        : ChildDirectiveResult =
        nativeOnly

    /// <summary>
    /// Returns an iterable containing the values in items interleaved with the joiner value.
    /// </summary>
    /// <remarks>
    /// For example <c>join (links, html $"&lt;span&gt; | &lt;/span&gt;")</c>.
    /// The result is a JavaScript generator: it can be enumerated once, so call <c>join</c> on each render.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/templates/directives/#join"/>
    [<Import("join", "lit/directives/join.js")>]
    static member inline join<'T, 'U>(items: seq<'T>, joiner: 'U) : seq<U2<'T, 'U>> = nativeOnly

    /// <summary>
    /// Renders <c>trueCase ()</c> when <c>condition</c> is true, otherwise <c>falseCase ()</c>.
    /// Only the chosen case runs.
    /// </summary>
    /// <remarks>
    /// Lit's <c>when</c>, renamed because <c>when</c> is an F# keyword. In F#,
    /// <c>if condition then ... else ...</c> does the same and reads better; <c>when'</c> is here for code
    /// ported from Lit.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/templates/directives/#when"/>
    [<Import("when", "lit/directives/when.js")>]
    static member inline when'(condition: bool, trueCase: unit -> 'T, falseCase: unit -> 'T) : 'T = nativeOnly

    /// <summary>
    /// Renders <c>trueCase ()</c> when <c>condition</c> is true, otherwise nothing.
    /// </summary>
    /// <remarks>
    /// Lit's <c>when</c> with no false case, for child content: when <c>condition</c> is false it returns
    /// <c>undefined</c>, which renders nothing. In F#, <c>if condition then html $"..." else nothing</c>
    /// does the same.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/templates/directives/#when"/>
    [<Import("when", "lit/directives/when.js")>]
    static member inline when'<'T when 'T :> ChildRenderable>(condition: bool, trueCase: unit -> 'T) : ChildRenderable =
        nativeOnly

    /// <summary>
    /// Renders the case whose key equals <c>value</c>, or <c>defaultCase ()</c> when none does.
    /// Only the chosen case runs.
    /// </summary>
    /// <remarks>
    /// Lit compares keys with JavaScript's <c>===</c>, so use strings, numbers or booleans as keys.
    /// F# unions and records are compared by reference and never match a different instance:
    /// <c>match</c> on them instead.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/templates/directives/#choose"/>
    [<Import("choose", "lit/directives/choose.js")>]
    static member inline choose(value: 'T, cases: seq<'T * (unit -> 'V)>, defaultCase: unit -> 'V) : 'V = nativeOnly

    /// <summary>
    /// Renders the case whose key equals <c>value</c>, or nothing when none does.
    /// Only the chosen case runs.
    /// </summary>
    /// <remarks>
    /// For child content: with no match, Lit returns <c>undefined</c>, which renders nothing.
    /// Lit compares keys with JavaScript's <c>===</c>, so use strings, numbers or booleans as keys.
    /// F# unions and records are compared by reference and never match a different instance:
    /// <c>match</c> on them instead.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/templates/directives/#choose"/>
    [<Import("choose", "lit/directives/choose.js")>]
    static member inline choose<'T, 'V when 'V :> ChildRenderable>
        (value: 'T, cases: seq<'T * (unit -> 'V)>)
        : ChildRenderable =
        nativeOnly

    /// <summary>
    /// The integers from 0 up to, but not including, <c>stop</c>.
    /// </summary>
    /// <remarks>
    /// The result is a JavaScript generator: it can be enumerated once. Call <c>range</c> again on each
    /// render rather than keeping the result.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/templates/directives/#range"/>
    [<Import("range", "lit/directives/range.js")>]
    static member inline range(stop: int) : seq<int> = nativeOnly

    /// <summary>
    /// The integers from <c>start</c> up to, but not including, <c>stop</c>, counting by <c>step</c>
    /// (default 1). A negative <c>step</c> counts down to, but not including, <c>stop</c>.
    /// </summary>
    /// <remarks>
    /// The result is a JavaScript generator: it can be enumerated once. Call <c>range</c> again on each
    /// render rather than keeping the result.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/templates/directives/#range"/>
    [<Import("range", "lit/directives/range.js")>]
    static member inline range(start: int, stop: int, ?step: int) : seq<int> = nativeOnly

    /// <summary>
    /// Calls <c>f</c> on each item of <c>items</c> as Lit renders them, and renders the results.
    /// </summary>
    /// <remarks>
    /// Lit's <c>map</c> is lazy and takes any iterable, so it works on an F# list, array or <c>seq</c>.
    /// <c>List.map</c> does the same job; for the item's index, use <c>List.mapi</c> or <c>Seq.mapi</c>.
    /// To keep each item's DOM with the item when the list changes, use <c>repeat</c>.
    /// The result is a JavaScript generator: it can be enumerated once. Call <c>map</c> again on each
    /// render rather than keeping the result.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/templates/directives/#map"/>
    [<Import("map", "lit/directives/map.js")>]
    static member inline map(items: seq<'T>, f: 'T -> 'R) : seq<'R> = nativeOnly

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
    static member inline ifDefined(value: Renderable option) : U2<ChildRenderable, nothing> = nativeOnly

    [<Import("ifDefined", "lit/directives/if-defined.js")>]
    static member inline ifDefined(value: string option) : U2<ChildRenderable, nothing> = nativeOnly

    [<Import("ifDefined", "lit/directives/if-defined.js")>]
    static member inline ifDefined(value: float option) : U2<ChildRenderable, nothing> = nativeOnly

    [<Import("ifDefined", "lit/directives/if-defined.js")>]
    static member inline ifDefined(value: int option) : U2<ChildRenderable, nothing> = nativeOnly

    [<Import("ifDefined", "lit/directives/if-defined.js")>]
    static member inline ifDefined(value: HTMLElement option) : U2<ChildRenderable, nothing> = nativeOnly

    /// <summary>
    /// Caches rendered DOM when changing templates rather than discarding the DOM. You can use this directive to optimize rendering performance when frequently switching between large templates.
    /// </summary>
    /// <remarks>
    /// When the value passed to cache changes between one or more TemplateResults, the rendered DOM nodes for a given template are cached when they're not in use. When the template changes, the directive caches the current DOM nodes before switching to the new value, and restores them from the cache when switching back to a previously-rendered value, rather than creating the DOM nodes anew.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/templates/directives/#cache"/>
    [<Import("cache", "lit/directives/cache.js")>]
    static member inline cache(value: ChildRenderable) : ChildDirectiveResult = nativeOnly

    /// <summary>
    /// Associates a renderable value with a unique key. When the key changes, the previous DOM is removed and disposed before rendering the next value, even if the value—such as a template—is the same.
    /// </summary>
    /// <remarks>
    /// keyed is useful when you're rendering stateful elements and you need to ensure that all state of the element is cleared when some critical data changes. It essentially opts-out of Lit's default DOM reuse strategy.
    /// keyed is also useful in some animation scenarios if you need to force a new element for "enter" or "exit" animations.
    ///
    /// Lit compares keys with JavaScript's <c>===</c>, so use a string or number, such as an id. An F# union or
    /// record built afresh on each render is a new key every time.
    /// </remarks>
    /// <param name="key">The key. When it differs from the last render's, the DOM is replaced.</param>
    /// <param name="value">What to render, usually a template.</param>
    /// <seealso href="https://lit.dev/docs/templates/directives/#keyed"/>
    [<Import("keyed", "lit/directives/keyed.js")>]
    static member inline keyed(key: 'K, value: 'V) : ChildDirectiveResult = nativeOnly

    /// <summary>
    /// Only re-evaluates the template when one of its dependencies changes, to optimize rendering performance by preventing unnecessary work.
    /// </summary>
    /// <remarks>
    /// Renders the value returned by valueFn, and only re-evaluates valueFn when one of the dependencies changes identity.
    /// Lit compares each dependency with the last render's using JavaScript's <c>===</c>. F# lists, records and
    /// maps are immutable, so a changed one is a new object and re-renders; one mutated in place doesn't.
    /// </remarks>
    /// <example><c>guard ([| rows; sortColumn |], fun () -> table rows sortColumn)</c></example>
    /// <seealso href="https://lit.dev/docs/templates/directives/#guard"/>
    /// <param name="dependencies">
    /// An array of dependencies that, when changed, will cause the directive to re-evaluate. F# boxes each
    /// element, so they may have different types.
    /// </param>
    /// <param name="valueFn">
    /// A function that returns the value to render when the dependencies change.
    /// </param>
    [<Import("guard", "lit/directives/guard.js")>]
    static member inline guard(dependencies: obj[], valueFn: unit -> 'T) : ChildDirectiveResult = nativeOnly

    /// <summary>
    /// Sets an attribute or property if it differs from the live DOM value rather than the last-rendered value.
    /// </summary>
    /// <remarks>
    /// For a value the user can change, such as an input's <c>.value={live text}</c>: when the state still holds
    /// the last rendered value, Lit would otherwise skip the update and leave what the user typed.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/templates/directives/#live"/>
    [<Import("live", "lit/directives/live.js")>]
    static member inline live(value: 'T) : DirectiveResult = nativeOnly

    /// <summary>
    /// Renders the content of a `&lt;template&gt;` element.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/templates/directives/#templatecontent"/>
    [<Import("templateContent", "lit/directives/template-content.js")>]
    static member inline templateContent(templateElement: HTMLTemplateElement) : ChildDirectiveResult = nativeOnly

    /// <summary>
    /// Renders a string as HTML rather than text.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/templates/directives/#unsafehtml"/>
    [<Import("unsafeHTML", "lit/directives/unsafe-html.js")>]
    static member inline unsafeHTML(html: string) : ChildDirectiveResult = nativeOnly

    /// <summary>
    /// Renders a string as SVG rather than text.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/templates/directives/#unsafesvg"/>
    [<Import("unsafeSVG", "lit/directives/unsafe-svg.js")>]
    static member inline unsafeSVG(svg: string) : ChildDirectiveResult = nativeOnly

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
    ///
    /// With <c>open type Firelight.Lit</c>, this <c>ref</c> hides F#'s <c>ref</c> function for reference cells:
    /// write <c>Operators.ref 0</c> for one of those. The name stays Lit's on purpose: under any other name,
    /// <c>{ref field}</c> written from Lit's docs would compile to an F# reference cell, which Lit ignores
    /// in an element binding without an error.
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
    ///
    /// With <c>open type Firelight.Lit</c>, this <c>ref</c> hides F#'s <c>ref</c> function for reference cells:
    /// write <c>Operators.ref 0</c> for one of those. The name stays Lit's on purpose: under any other name,
    /// <c>{ref field}</c> written from Lit's docs would compile to an F# reference cell, which Lit ignores
    /// in an element binding without an error.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/templates/directives/#ref"/>
    [<Import("ref", "lit/directives/ref.js")>]
    static member inline ref<'T when 'T :> Element>(callback: 'T option -> unit) : DirectiveResult = nativeOnly

    /// <summary>
    /// Renders what <c>promise</c> resolves to once it resolves.
    /// </summary>
    /// <remarks>
    /// Until then the part keeps what it showed before: nothing on the first render, or the last result
    /// when a new promise replaces one that resolved. To clear it while waiting, use
    /// <c>until (promise, nothing)</c>.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/templates/directives/#until"/>
    [<Import("until", "lit/directives/until.js")>]
    static member inline until(promise: Promise<'T>) : ChildDirectiveResult = nativeOnly

    /// <summary>
    /// Renders <c>placeholder</c> until <c>promise</c> resolves, then what it resolves to.
    /// </summary>
    /// <example><c>until (loadProfile id, html $"&lt;p&gt;Loading…&lt;/p&gt;")</c></example>
    /// <seealso href="https://lit.dev/docs/templates/directives/#until"/>
    [<Import("until", "lit/directives/until.js")>]
    static member inline until(promise: Promise<'T>, placeholder: 'U) : ChildDirectiveResult = nativeOnly

    /// <summary>
    /// Renders the highest-priority of <c>values</c> available so far. The first has the highest priority. A
    /// plain value is available at once and a promise once it resolves, so put the placeholder last.
    /// </summary>
    /// <remarks>
    /// The arguments are boxed, so they may have different types. For one promise, with or without a
    /// placeholder, the typed overloads read better.
    /// </remarks>
    /// <seealso href="https://lit.dev/docs/templates/directives/#until"/>
    [<Import("until", "lit/directives/until.js")>]
    static member inline until([<ParamArray>] values: obj[]) : ChildDirectiveResult = nativeOnly

    /// <summary>
    /// Appends values from an `AsyncIterable` into the DOM as they are yielded.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/templates/directives/#asyncappend"/>
    [<Import("asyncAppend", "lit/directives/async-append.js")>]
    static member inline asyncAppend(iterable: AsyncIterable<ChildRenderable>) : ChildDirectiveResult = nativeOnly

    /// <summary>
    /// Appends values from an `AsyncIterable` into the DOM as they are yielded.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/templates/directives/#asyncappend"/>
    [<Import("asyncAppend", "lit/directives/async-append.js")>]
    static member inline asyncAppend(iterable: AsyncIterable<'T>, mapper: 'T -> ChildRenderable) : ChildDirectiveResult =
        nativeOnly

    /// <summary>
    /// Appends values from an `AsyncIterable` into the DOM as they are yielded.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/templates/directives/#asyncappend"/>
    [<Import("asyncAppend", "lit/directives/async-append.js")>]
    static member inline asyncAppend
        (iterable: AsyncIterable<'T>, mapper: 'T -> int -> ChildRenderable)
        : ChildDirectiveResult =
        nativeOnly

    /// <summary>
    /// Renders the latest value from an `AsyncIterable` into the DOM as it is yielded.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/templates/directives/#asyncreplace"/>
    [<Import("asyncReplace", "lit/directives/async-replace.js")>]
    static member inline asyncReplace(iterable: AsyncIterable<ChildRenderable>) : ChildDirectiveResult = nativeOnly

    /// <summary>
    /// Renders the latest value from an `AsyncIterable` into the DOM as it is yielded.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/templates/directives/#asyncreplace"/>
    [<Import("asyncReplace", "lit/directives/async-replace.js")>]
    static member inline asyncReplace(iterable: AsyncIterable<'T>, mapper: 'T -> ChildRenderable) : ChildDirectiveResult =
        nativeOnly

    /// <summary>
    /// Renders the latest value from an `AsyncIterable` into the DOM as it is yielded.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/templates/directives/#asyncreplace"/>
    [<Import("asyncReplace", "lit/directives/async-replace.js")>]
    static member inline asyncReplace
        (iterable: AsyncIterable<'T>, mapper: 'T -> int -> ChildRenderable)
        : ChildDirectiveResult =
        nativeOnly
