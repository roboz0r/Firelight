[<AutoOpen>]
module Lit.Directives

open Fable.Core
open Fable.Core.JsInterop

type ClassInfo = interface end
type StyleInfo = interface end
type DirectiveResult = interface end

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

type KeyFn<'T> = delegate of item: 'T * index: int -> obj
type ItemTemplate<'T> = delegate of item: 'T * index: int -> obj

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
    static member inline repeat<'T>(items: seq<'T>, keyFn: KeyFn<'T>, template: ItemTemplate<'T>) : DirectiveResult =
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
    static member inline ifDefined(value: obj option) : obj = nativeOnly
