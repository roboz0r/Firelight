namespace Firelight.Motion

open Browser.Types
open Fable.Core
open Fable.Core.JsInterop
open Firelight

/// A Web Animations keyframe. Use MotionKeyframe.create for arbitrary CSS properties.
type MotionKeyframe = interface end

[<Erase>]
module MotionKeyframe =
    let inline create (properties: #seq<string * obj>) : MotionKeyframe = !!(createObj !!properties)

/// Web Animations timing options passed through to Element.animate.
[<AllowNullLiteral; Global>]
type MotionKeyframeOptions
    [<ParamObject; Emit("$0")>]
    (
        ?duration: U2<float, string>,
        ?delay: float,
        ?endDelay: float,
        ?easing: string,
        ?direction: string,
        ?fill: string,
        ?iterations: float,
        ?iterationStart: float,
        ?composite: string,
        ?iterationComposite: string
    ) =
    member val duration: U2<float, string> option = nativeOnly with get, set
    member val delay: float option = nativeOnly with get, set
    member val endDelay: float option = nativeOnly with get, set
    member val easing: string option = nativeOnly with get, set
    member val direction: string option = nativeOnly with get, set
    member val fill: string option = nativeOnly with get, set
    member val iterations: float option = nativeOnly with get, set
    member val iterationStart: float option = nativeOnly with get, set
    member val composite: string option = nativeOnly with get, set
    member val iterationComposite: string option = nativeOnly with get, set

/// The active animate directive supplied to animation callbacks.
[<AllowNullLiteral>]
type Animate =
    abstract element: HTMLElement
    abstract frames: MotionKeyframe[] option
    abstract finished: JS.Promise<unit>
    abstract isAnimating: unit -> bool

/// Options for the animate directive. Use in an element expression, for example
/// <div ${Motion.animate(MotionOptions(``in`` = Motion.fade))}></div>.
[<AllowNullLiteral; Global>]
type MotionOptions
    [<ParamObject; Emit("$0")>]
    (
        ?keyframeOptions: MotionKeyframeOptions,
        ?properties: string[],
        ?disabled: bool,
        ?guard: unit -> obj,
        ?id: obj,
        ?inId: obj,
        ?``in``: MotionKeyframe[],
        ?``out``: MotionKeyframe[],
        ?stabilizeOut: bool,
        ?skipInitial: bool,
        ?onStart: Animate -> unit,
        ?onComplete: Animate -> unit,
        ?onFrames: Animate -> MotionKeyframe[]
    ) =
    member val keyframeOptions: MotionKeyframeOptions option = nativeOnly with get, set
    member val properties: string[] option = nativeOnly with get, set
    member val disabled: bool option = nativeOnly with get, set
    member val guard: (unit -> obj) option = nativeOnly with get, set
    member val id: obj option = nativeOnly with get, set
    member val inId: obj option = nativeOnly with get, set
    member val ``in``: MotionKeyframe[] option = nativeOnly with get, set
    member val ``out``: MotionKeyframe[] option = nativeOnly with get, set
    member val stabilizeOut: bool option = nativeOnly with get, set
    member val skipInitial: bool option = nativeOnly with get, set
    member val onStart: (Animate -> unit) option = nativeOnly with get, set
    member val onComplete: (Animate -> unit) option = nativeOnly with get, set
    member val onFrames: (Animate -> MotionKeyframe[]) option = nativeOnly with get, set

[<AllowNullLiteral; Global>]
type AnimateControllerOptions
    [<ParamObject; Emit("$0")>]
    (?defaultOptions: MotionOptions, ?startPaused: bool, ?disabled: bool, ?onComplete: unit -> unit) =
    member val defaultOptions: MotionOptions option = nativeOnly with get, set
    member val startPaused: bool option = nativeOnly with get, set
    member val disabled: bool option = nativeOnly with get, set
    member val onComplete: (unit -> unit) option = nativeOnly with get, set

/// Controls all animate directives rendered by one Lit host.
[<AllowNullLiteral; Import("AnimateController", "@lit-labs/motion")>]
type AnimateController(host: ReactiveControllerHost, options: AnimateControllerOptions) =
    member val defaultOptions: MotionOptions = nativeOnly with get, set
    member val startPaused: bool = nativeOnly with get, set
    member val disabled: bool = nativeOnly with get, set
    member val onComplete: (unit -> unit) option = nativeOnly with get, set
    member _.isAnimating: bool = nativeOnly
    member _.isPlaying: bool = nativeOnly
    member _.play() : unit = nativeOnly
    member _.pause() : unit = nativeOnly
    member _.cancel() : unit = nativeOnly
    member _.finish() : unit = nativeOnly
    member _.togglePlay() : unit = nativeOnly
    member _.finished() : JS.Promise<unit> = nativeOnly

/// A two dimensional position used by SpringController2D.
[<AllowNullLiteral; Global>]
type Position2D [<ParamObject; Emit("$0")>] (x: float, y: float) =
    member val x: float = nativeOnly with get, set
    member val y: float = nativeOnly with get, set

[<AllowNullLiteral; Global>]
type SpringConfig
    [<ParamObject; Emit("$0")>]
    (
        ?fromValue: float,
        ?toValue: float,
        ?stiffness: float,
        ?damping: float,
        ?mass: float,
        ?initialVelocity: float,
        ?allowsOverdamping: bool,
        ?overshootClamping: bool,
        ?restVelocityThreshold: float,
        ?restDisplacementThreshold: float
    ) =
    member val fromValue: float option = nativeOnly with get, set
    member val toValue: float option = nativeOnly with get, set
    member val stiffness: float option = nativeOnly with get, set
    member val damping: float option = nativeOnly with get, set
    member val mass: float option = nativeOnly with get, set
    member val initialVelocity: float option = nativeOnly with get, set
    member val allowsOverdamping: bool option = nativeOnly with get, set
    member val overshootClamping: bool option = nativeOnly with get, set
    member val restVelocityThreshold: float option = nativeOnly with get, set
    member val restDisplacementThreshold: float option = nativeOnly with get, set

[<AllowNullLiteral; Global>]
type Spring2DConfig
    [<ParamObject; Emit("$0")>]
    (
        ?fromPosition: Position2D,
        ?toPosition: Position2D,
        ?stiffness: float,
        ?damping: float,
        ?mass: float,
        ?initialVelocity: Position2D,
        ?allowsOverdamping: bool,
        ?overshootClamping: bool,
        ?restVelocityThreshold: float,
        ?restDisplacementThreshold: float
    ) =
    member val fromPosition: Position2D option = nativeOnly with get, set
    member val toPosition: Position2D option = nativeOnly with get, set
    member val stiffness: float option = nativeOnly with get, set
    member val damping: float option = nativeOnly with get, set
    member val mass: float option = nativeOnly with get, set
    member val initialVelocity: Position2D option = nativeOnly with get, set
    member val allowsOverdamping: bool option = nativeOnly with get, set
    member val overshootClamping: bool option = nativeOnly with get, set
    member val restVelocityThreshold: float option = nativeOnly with get, set
    member val restDisplacementThreshold: float option = nativeOnly with get, set

[<AllowNullLiteral; Import("SpringController", "@lit-labs/motion/spring.js")>]
type SpringController(host: LitElement, ?config: SpringConfig) =
    member val fromValue: float = nativeOnly with get, set
    member val toValue: float = nativeOnly with get, set
    member _.currentValue: float = nativeOnly
    member _.currentVelocity: float = nativeOnly
    member _.isAtRest: bool = nativeOnly
    member _.isAnimating: bool = nativeOnly

[<AllowNullLiteral; Import("SpringController2D", "@lit-labs/motion/spring.js")>]
type SpringController2D(host: LitElement, ?config: Spring2DConfig) =
    member val toPosition: Position2D = nativeOnly with get, set
    member _.currentPosition: Position2D = nativeOnly
    member _.currentVelocity: Position2D = nativeOnly
    member _.isAtRest: bool = nativeOnly
    member _.isAnimating: bool = nativeOnly

[<Erase>]
type Motion =
    /// Animate an element between Lit renders.
    [<Import("animate", "@lit-labs/motion")>]
    static member inline animate(?options: MotionOptions) : DirectiveResult = nativeOnly

    /// Evaluate options at host update time.
    [<Import("animate", "@lit-labs/motion")>]
    static member inline animate(options: unit -> MotionOptions) : DirectiveResult = nativeOnly

    [<Import("flyBelow", "@lit-labs/motion")>]
    static member inline flyBelow: MotionKeyframe[] = nativeOnly

    [<Import("flyAbove", "@lit-labs/motion")>]
    static member inline flyAbove: MotionKeyframe[] = nativeOnly

    [<Import("flyLeft", "@lit-labs/motion")>]
    static member inline flyLeft: MotionKeyframe[] = nativeOnly

    [<Import("flyRight", "@lit-labs/motion")>]
    static member inline flyRight: MotionKeyframe[] = nativeOnly

    [<Import("fade", "@lit-labs/motion")>]
    static member inline fade: MotionKeyframe[] = nativeOnly

    [<Import("fadeIn", "@lit-labs/motion")>]
    static member inline fadeIn: MotionKeyframe[] = nativeOnly

    [<Import("fadeOut", "@lit-labs/motion")>]
    static member inline fadeOut: MotionKeyframe[] = nativeOnly

    [<Import("fadeInSlow", "@lit-labs/motion")>]
    static member inline fadeInSlow: MotionKeyframe[] = nativeOnly

    [<Import("none", "@lit-labs/motion")>]
    static member inline none: MotionKeyframe[] = nativeOnly
