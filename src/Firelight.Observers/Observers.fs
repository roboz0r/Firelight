namespace Firelight.Observers

open Browser.Types
open Fable.Core
open Firelight

/// Browser DOMRect values returned by observer entries.
[<AllowNullLiteral>]
type ObserverRect =
    abstract x: float
    abstract y: float
    abstract width: float
    abstract height: float
    abstract top: float
    abstract right: float
    abstract bottom: float
    abstract left: float

/// A DOM mutation reported by MutationObserver.
[<AllowNullLiteral>]
type MutationRecord =
    abstract ``type``: string
    abstract target: Node
    abstract addedNodes: NodeList
    abstract removedNodes: NodeList
    abstract attributeName: string option
    abstract oldValue: string option

/// Options passed to the browser's MutationObserver.observe.
[<AllowNullLiteral; Global>]
type MutationOptions
    [<ParamObject; Emit("$0")>]
    (
        ?attributes: bool,
        ?attributeOldValue: bool,
        ?attributeFilter: string[],
        ?characterData: bool,
        ?characterDataOldValue: bool,
        ?childList: bool,
        ?subtree: bool
    ) =
    member val attributes: bool option = nativeOnly with get, set
    member val attributeOldValue: bool option = nativeOnly with get, set
    member val attributeFilter: string[] option = nativeOnly with get, set
    member val characterData: bool option = nativeOnly with get, set
    member val characterDataOldValue: bool option = nativeOnly with get, set
    member val childList: bool option = nativeOnly with get, set
    member val subtree: bool option = nativeOnly with get, set

[<AllowNullLiteral>]
type IntersectionEntry =
    abstract target: Element
    abstract isIntersecting: bool
    abstract intersectionRatio: float
    abstract boundingClientRect: ObserverRect
    abstract intersectionRect: ObserverRect
    abstract rootBounds: ObserverRect option
    abstract time: float

[<AllowNullLiteral; Global>]
type IntersectionOptions
    [<ParamObject; Emit("$0")>]
    (?root: Element, ?rootMargin: string, ?threshold: U2<float, float[]>) =
    member val root: Element option = nativeOnly with get, set
    member val rootMargin: string option = nativeOnly with get, set
    member val threshold: U2<float, float[]> option = nativeOnly with get, set

[<AllowNullLiteral>]
type ResizeBoxSize =
    abstract blockSize: float
    abstract inlineSize: float

[<AllowNullLiteral>]
type ResizeEntry =
    abstract target: Element
    abstract contentRect: ObserverRect
    abstract borderBoxSize: ResizeBoxSize[]
    abstract contentBoxSize: ResizeBoxSize[]
    abstract devicePixelContentBoxSize: ResizeBoxSize[]

[<AllowNullLiteral; Global>]
type ResizeOptions [<ParamObject; Emit("$0")>] (?box: string) =
    member val box: string option = nativeOnly with get, set

[<AllowNullLiteral; Global>]
type PerformanceOptions
    [<ParamObject; Emit("$0")>]
    (?entryTypes: string[], ?``type``: string, ?buffered: bool, ?durationThreshold: float) =
    member val entryTypes: string[] option = nativeOnly with get, set
    member val ``type``: string option = nativeOnly with get, set
    member val buffered: bool option = nativeOnly with get, set
    member val durationThreshold: float option = nativeOnly with get, set

[<AllowNullLiteral>]
type PerformanceEntry =
    abstract name: string
    abstract entryType: string
    abstract startTime: float
    abstract duration: float

/// The observer's pending entry queue passed as the optional third performance callback argument.
[<AllowNullLiteral>]
type PerformanceObserverEntryList =
    abstract getEntries: unit -> PerformanceEntry[]
    abstract getEntriesByName: name: string * ?entryType: string -> PerformanceEntry[]
    abstract getEntriesByType: entryType: string -> PerformanceEntry[]

type MutationValueCallback<'T> = delegate of records: MutationRecord[] * observer: obj -> 'T
type IntersectionValueCallback<'T> = delegate of entries: IntersectionEntry[] * observer: obj -> 'T
type ResizeValueCallback<'T> = delegate of entries: ResizeEntry[] * observer: obj -> 'T

type PerformanceValueCallback<'T> =
    delegate of entries: PerformanceEntry[] * observer: obj * pendingEntries: PerformanceObserverEntryList option -> 'T

[<AllowNullLiteral; Global>]
type MutationControllerConfig<'T>
    [<ParamObject; Emit("$0")>]
    (config: MutationOptions, ?target: Element, ?callback: MutationValueCallback<'T>, ?skipInitial: bool) =
    member val config: MutationOptions = nativeOnly with get, set
    member val target: Element option = nativeOnly with get, set
    member val callback: MutationValueCallback<'T> option = nativeOnly with get, set
    member val skipInitial: bool option = nativeOnly with get, set

[<AllowNullLiteral; Global>]
type IntersectionControllerConfig<'T>
    [<ParamObject; Emit("$0")>]
    (?target: Element, ?config: IntersectionOptions, ?callback: IntersectionValueCallback<'T>, ?skipInitial: bool) =
    member val target: Element option = nativeOnly with get, set
    member val config: IntersectionOptions option = nativeOnly with get, set
    member val callback: IntersectionValueCallback<'T> option = nativeOnly with get, set
    member val skipInitial: bool option = nativeOnly with get, set

[<AllowNullLiteral; Global>]
type ResizeControllerConfig<'T>
    [<ParamObject; Emit("$0")>]
    (?target: Element, ?config: ResizeOptions, ?callback: ResizeValueCallback<'T>, ?skipInitial: bool) =
    member val target: Element option = nativeOnly with get, set
    member val config: ResizeOptions option = nativeOnly with get, set
    member val callback: ResizeValueCallback<'T> option = nativeOnly with get, set
    member val skipInitial: bool option = nativeOnly with get, set

[<AllowNullLiteral; Global>]
type PerformanceControllerConfig<'T>
    [<ParamObject; Emit("$0")>]
    (config: PerformanceOptions, ?callback: PerformanceValueCallback<'T>, ?skipInitial: bool) =
    member val config: PerformanceOptions = nativeOnly with get, set
    member val callback: PerformanceValueCallback<'T> option = nativeOnly with get, set
    member val skipInitial: bool option = nativeOnly with get, set

/// Watches mutations and requests a host update when records arrive.
[<AllowNullLiteral; Import("MutationController", "@lit-labs/observers/mutation-controller.js")>]
type MutationController<'T>(host: LitElement, config: MutationControllerConfig<'T>) =
    member _.value: 'T option = nativeOnly
    member val callback: MutationValueCallback<'T> option = nativeOnly with get, set
    member _.observe(target: Element) : unit = nativeOnly

    interface ReactiveController with
        member _.hostConnected() : unit = nativeOnly
        member _.hostDisconnected() : unit = nativeOnly
        member _.hostUpdate() : unit = nativeOnly
        member _.hostUpdated() : unit = nativeOnly

/// Watches intersection changes and requests a host update.
[<AllowNullLiteral; Import("IntersectionController", "@lit-labs/observers/intersection-controller.js")>]
type IntersectionController<'T>(host: LitElement, config: IntersectionControllerConfig<'T>) =
    member _.value: 'T option = nativeOnly
    member val callback: IntersectionValueCallback<'T> option = nativeOnly with get, set
    member _.observe(target: Element) : unit = nativeOnly
    member _.unobserve(target: Element) : unit = nativeOnly

    interface ReactiveController with
        member _.hostConnected() : unit = nativeOnly
        member _.hostDisconnected() : unit = nativeOnly
        member _.hostUpdate() : unit = nativeOnly
        member _.hostUpdated() : unit = nativeOnly

/// Watches size changes and requests a host update.
[<AllowNullLiteral; Import("ResizeController", "@lit-labs/observers/resize-controller.js")>]
type ResizeController<'T>(host: LitElement, config: ResizeControllerConfig<'T>) =
    member _.value: 'T option = nativeOnly
    member val callback: ResizeValueCallback<'T> option = nativeOnly with get, set
    member _.observe(target: Element) : unit = nativeOnly
    member _.unobserve(target: Element) : unit = nativeOnly
    /// Observe an element from a Lit element-part expression; cleanup is automatic.
    member _.target(?observe: bool) : DirectiveResult = nativeOnly

    interface ReactiveController with
        member _.hostConnected() : unit = nativeOnly
        member _.hostDisconnected() : unit = nativeOnly
        member _.hostUpdate() : unit = nativeOnly
        member _.hostUpdated() : unit = nativeOnly

/// Watches performance entries and requests a host update.
[<AllowNullLiteral; Import("PerformanceController", "@lit-labs/observers/performance-controller.js")>]
type PerformanceController<'T>(host: ReactiveControllerHost, config: PerformanceControllerConfig<'T>) =
    member _.value: 'T option = nativeOnly
    member val callback: PerformanceValueCallback<'T> option = nativeOnly with get, set
    member _.flush() : unit = nativeOnly
    member _.observe() : unit = nativeOnly

    interface ReactiveController with
        member _.hostConnected() : unit = nativeOnly
        member _.hostDisconnected() : unit = nativeOnly
        member _.hostUpdate() : unit = nativeOnly
        member _.hostUpdated() : unit = nativeOnly
