namespace Firelight.Virtualizer

open Browser
open Browser.Types
open Fable.Core
open Fable.Core.JsInterop
open Firelight

/// A layout specifier accepted by the virtualizer.
[<AllowNullLiteral>]
type VirtualizerLayout = interface end

[<AllowNullLiteral; Global>]
type PinOptions [<ParamObject; Emit("$0")>] (index: int, ?block: string) =
    member val index: int = nativeOnly with get, set
    member val block: string option = nativeOnly with get, set

/// Options for the default flow layout. Direction is vertical unless changed.
[<AllowNullLiteral; Global>]
type FlowLayoutOptions [<ParamObject; Emit("$0")>] (?direction: string, ?pin: PinOptions) =
    member val direction: string option = nativeOnly with get, set
    member val pin: PinOptions option = nativeOnly with get, set
    interface VirtualizerLayout

[<AllowNullLiteral; Global>]
type GridItemSize [<ParamObject; Emit("$0")>] (width: string, height: string) =
    member val width: string = nativeOnly with get, set
    member val height: string = nativeOnly with get, set

[<AllowNullLiteral; Global>]
type GridFlexOptions [<ParamObject; Emit("$0")>] (preserve: string) =
    member val preserve: string = nativeOnly with get, set

/// Grid layout options. CSS sizes such as "100px" are accepted for itemSize,
/// gap, and padding; use "auto" in a gap where supported by the layout.
[<AllowNullLiteral; Global>]
type GridLayoutOptions
    [<ParamObject; Emit("$0")>]
    (
        ?direction: string,
        ?pin: PinOptions,
        ?itemSize: U2<string, GridItemSize>,
        ?gap: string,
        ?padding: string,
        ?flex: U2<bool, GridFlexOptions>,
        ?justify: string
    ) =
    member val direction: string option = nativeOnly with get, set
    member val pin: PinOptions option = nativeOnly with get, set
    member val itemSize: U2<string, GridItemSize> option = nativeOnly with get, set
    member val gap: string option = nativeOnly with get, set
    member val padding: string option = nativeOnly with get, set
    member val flex: U2<bool, GridFlexOptions> option = nativeOnly with get, set
    member val justify: string option = nativeOnly with get, set

type RenderItem<'T> = delegate of item: 'T * index: int -> TemplateResult
type KeyFunction<'T> = delegate of item: 'T * index: int -> obj

/// Options passed to the virtualize child-part directive.
[<AllowNullLiteral; Global>]
type VirtualizeConfig<'T>
    [<ParamObject; Emit("$0")>]
    (items: 'T[], renderItem: RenderItem<'T>, ?keyFunction: KeyFunction<'T>, ?scroller: bool, ?layout: VirtualizerLayout)
    =
    member val items: 'T[] = nativeOnly with get, set
    member val renderItem: RenderItem<'T> = nativeOnly with get, set
    member val keyFunction: KeyFunction<'T> option = nativeOnly with get, set
    member val scroller: bool option = nativeOnly with get, set
    member val layout: VirtualizerLayout option = nativeOnly with get, set

/// A proxy for an item that may not currently exist in the DOM.
[<AllowNullLiteral; Global>]
type ScrollIntoViewOptions [<ParamObject; Emit("$0")>] (?behavior: string, ?block: string, ?``inline``: string) =
    member val behavior: string option = nativeOnly with get, set
    member val block: string option = nativeOnly with get, set
    member val ``inline``: string option = nativeOnly with get, set

[<AllowNullLiteral>]
type VirtualizerChild =
    abstract scrollIntoView: ?options: ScrollIntoViewOptions -> unit

/// Handle stored on a directive's host element under virtualizerRef.
[<AllowNullLiteral>]
type VirtualizerHandle =
    abstract element: index: int -> VirtualizerChild option
    abstract layoutComplete: JS.Promise<unit>

/// Fired when the DOM range or visible range changes.
[<AllowNullLiteral>]
type RangeChangedEvent =
    inherit Event
    abstract first: int
    abstract last: int

[<AllowNullLiteral>]
type VisibilityChangedEvent =
    inherit Event
    abstract first: int
    abstract last: int

/// The <lit-virtualizer> custom element. Call LitVirtualizer.defineElement()
/// before rendering its tag; the directive does not need registration.
[<AbstractClass; Import("LitVirtualizer", "@lit-labs/virtualizer/LitVirtualizer.js")>]
type LitVirtualizer<'T>() =
    inherit LitElement()
    member val items: 'T[] = nativeOnly with get, set
    member val renderItem: RenderItem<'T> = nativeOnly with get, set
    member val keyFunction: KeyFunction<'T> = nativeOnly with get, set
    member val layout: VirtualizerLayout = nativeOnly with get, set
    member val scroller: bool = nativeOnly with get, set
    member _.element(index: int) : VirtualizerChild option = nativeOnly
    member _.layoutComplete: JS.Promise<unit> option = nativeOnly
    member _.scrollToIndex(index: int, ?position: string) : unit = nativeOnly

[<Erase>]
type Virtualizer =
    /// Apply viewport virtualization to the parent of this child expression.
    [<Import("virtualize", "@lit-labs/virtualizer/virtualize.js")>]
    static member inline virtualize(config: VirtualizeConfig<'T>) : DirectiveResult = nativeOnly

    /// Create a flow layout specifier, for example with horizontal direction.
    [<Import("flow", "@lit-labs/virtualizer/layouts/flow.js")>]
    static member inline flow(?config: FlowLayoutOptions) : VirtualizerLayout = nativeOnly

    /// Create a grid layout specifier.
    [<Import("grid", "@lit-labs/virtualizer/layouts/grid.js")>]
    static member inline grid(?config: GridLayoutOptions) : VirtualizerLayout = nativeOnly

    [<Import("virtualizerRef", "@lit-labs/virtualizer/virtualize.js")>]
    static member inline virtualizerRef: symbol = nativeOnly

    /// Get the handle for a virtualize directive from its parent element.
    [<Emit("$0[$1]")>]
    static member inline private getInner(element: HTMLElement, key: symbol) : VirtualizerHandle option = nativeOnly

    static member inline get(element: HTMLElement) : VirtualizerHandle option =
        Virtualizer.getInner (element, Virtualizer.virtualizerRef)

    // The global registry rather than window.customElements: the same object in a browser, but
    // Lit SSR's DOM shim provides a global customElements in Node and no window.
    [<Emit("customElements")>]
    static member inline private customElements: CustomElementRegistry = nativeOnly

    /// Register <lit-virtualizer>. Call once before rendering the element; a second call does nothing.
    static member inline defineElement() : unit =
        if Virtualizer.customElements.get ("lit-virtualizer") |> Option.isNone then
            Virtualizer.customElements.define ("lit-virtualizer", jsConstructor<LitVirtualizer<obj>>)
