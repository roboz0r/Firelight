module App

open Browser
open Fable.Core
open Firelight
open Firelight.Motion
open type Firelight.Lit

type Card =
    {
        Id: int
        Title: string
        Detail: string
    }

// Spring stops are percentages of the dot's travel. They sit inside the track
// so the spring's overshoot is visible instead of being clamped at the edges.
[<Literal>]
let private SpringStart = 15.0

[<Literal>]
let private SpringEnd = 85.0

let private showcaseStyles =
    css
        $$"""
            :host { display: block; }
            .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(270px, 1fr)); gap: 18px; }
            section { background: white; border: 1px solid #dbe1ec; border-radius: 18px; padding: 22px; box-shadow: 0 10px 28px #26395b0a; }
            section.wide { grid-column: 1 / -1; }
            h2 { font-size: 1.12rem; margin: 0 0 7px; }
            p { color: #59657b; line-height: 1.5; margin: 0 0 18px; }
            button { border: 0; border-radius: 9px; background: #324cc8; color: white; padding: 10px 14px; font: inherit; cursor: pointer; }
            button.secondary { background: #e8ecfb; color: #233a9c; }
            button:disabled { opacity: .55; cursor: not-allowed; }
            button:focus-visible { outline: 3px solid #8294f5; outline-offset: 3px; }
            .buttons { display: flex; flex-wrap: wrap; gap: 9px; margin-bottom: 17px; }
            ol { list-style: none; margin: 0; padding: 0; display: grid; gap: 9px; }
            li { background: #eef1ff; border: 1px solid #d9dffe; border-radius: 11px; padding: 12px 14px; }
            li strong { display: block; color: #1f318d; }
            li span { color: #5b6684; font-size: .88rem; }
            .notice-area { min-height: 70px; }
            .notice { background: #e6f7f0; border: 1px solid #91d6b6; color: #13664a; border-radius: 11px; padding: 16px; }
            .track { height: 58px; background: #edf1f8; border-radius: 30px; padding: 8px; overflow: hidden; }
            .lane { position: relative; width: 100%; height: 100%; }
            .dot { position: absolute; top: 50%; display: grid; place-items: center; width: 42px; height: 42px; border-radius: 50%; background: #e77a4d; color: white; box-shadow: 0 4px 12px #b43a2140; will-change: left, transform; }
            .hint { font-size: .85rem; margin: 10px 0 0; }
    """

/// Lit Motion attaches an AnimateController to every animate() rendered by the
/// same host, so the layout demo is its own element to keep pause/play scoped
/// to these cards.
[<AttachMembers>]
type LayoutDemo() as this =
    inherit LitElement()

    let mutable cards =
        [
            {
                Id = 1
                Title = "First"
                Detail = "Keeps its DOM identity"
            }
            {
                Id = 2
                Title = "Second"
                Detail = "Slides to a new position"
            }
            {
                Id = 3
                Title = "Third"
                Detail = "Uses keyed repeat"
            }
        ]

    let mutable paused = false
    let mutable animationPending = false

    let controller =
        AnimateController(
            this,
            AnimateControllerOptions(
                defaultOptions =
                    MotionOptions(
                        keyframeOptions = MotionKeyframeOptions(duration = U2.Case1 500.0),
                        skipInitial = true
                    ),
                onComplete =
                    (fun () ->
                        animationPending <- false
                        this.requestUpdate ()
                    )
            )
        )

    static member styles = showcaseStyles

    member private _.Reorder() =
        if not animationPending then
            match cards with
            | first :: rest ->
                animationPending <- true
                cards <- rest @ [ first ]
                this.requestUpdate ()
            | [] -> ()

    member private _.TogglePlayback() =
        paused <- not paused
        // pause()/play() only affect active animations; startPaused covers
        // animations created by a later click on Rotate cards.
        controller.startPaused <- paused
        if paused then controller.pause () else controller.play ()
        this.requestUpdate ()

    override _.render() =
        let cardList =
            repeat (
                cards,
                KeyFn(fun card _ -> card.Id),
                ItemTemplate(fun card _ ->
                    html
                        $"""
                        <li {Motion.animate ()}>
                            <strong>{card.Title}</strong>
                            <span>{card.Detail}</span>
                        </li>
                    """
                )
            )

        html
            $"""
            <h2>Layout animation</h2>
            <p>Reorder the same keyed elements and watch Lit Motion animate their new positions.</p>
            <div class="buttons">
                <button ?disabled={animationPending} @click={fun _ -> this.Reorder()}>Rotate cards</button>
                <button class="secondary" aria-pressed={string paused} @click={fun _ -> this.TogglePlayback()}>{if paused then "Play animations" else "Pause animations"}</button>
            </div>
            <p class="hint">{if paused && animationPending then
                                 "Paused. Press Play before starting another transition."
                             elif paused then
                                 "Paused. Rotate the cards, then press Play to see them move."
                             elif animationPending then
                                 "Transition in progress."
                             else
                                 "Animations are playing."}</p>
            <ol>{cardList}</ol>
        """

defineElement<LayoutDemo> "motion-layout-demo"

[<AttachMembers>]
type MotionShowcase() as this =
    inherit LitElement()

    let mutable noticeVisible = true
    let mutable noticePending = false

    // This host has no AnimateController, so the notice sets its own timing.
    let noticeOptions =
        MotionOptions(
            keyframeOptions = MotionKeyframeOptions(duration = U2.Case1 500.0),
            skipInitial = true,
            ``in`` = Motion.fadeIn,
            ``out`` = Motion.fadeOut,
            onComplete =
                (fun _ ->
                    noticePending <- false
                    this.requestUpdate ()
                )
        )

    let spring =
        SpringController(
            this,
            // Damping ratio ~0.45: roughly 20% overshoot, which fits between
            // each stop and the track edge.
            SpringConfig(fromValue = SpringStart, toValue = SpringStart, stiffness = 180.0, damping = 12.0)
        )

    static member styles = showcaseStyles

    member private _.ToggleNotice() =
        if not noticePending then
            noticePending <- true
            noticeVisible <- not noticeVisible
            this.requestUpdate ()

    member private _.MoveSpring() =
        // Read the controller's target so hot reload cannot leave a separate
        // direction flag out of sync with the running spring.
        spring.toValue <-
            if spring.toValue = SpringEnd then
                SpringStart
            else
                SpringEnd

        this.requestUpdate ()

    override _.render() =
        // Guard only; the stops leave room for normal overshoot.
        let position = min 100.0 (max 0.0 spring.currentValue)
        let positionText = position.ToString("0") + "%"

        let notice: ChildRenderable =
            if noticeVisible then
                html
                    $"""
                    <div class="notice" {Motion.animate (noticeOptions)}>
                        The notice animates when it appears and disappears.
                    </div>
                """
            else
                nothing

        html
            $"""
            <div class="grid">
                <section class="wide"><motion-layout-demo></motion-layout-demo></section>
                <section>
                    <h2>Enter and exit</h2>
                    <p>Keyframe presets animate a conditional element into and out of the DOM.</p>
                    <div class="buttons"><button ?disabled={noticePending} @click={fun _ -> this.ToggleNotice()}>Toggle notice</button></div>
                    <div class="notice-area">{notice}</div>
                </section>
                <section>
                    <h2>Spring controller</h2>
                    <p>The spring updates its host while the value settles at a new target.</p>
                    <div class="buttons"><button @click={fun _ -> this.MoveSpring()}>Move dot</button></div>
                    <div class="track"><div class="lane"><div class="dot" style={"left: "
                                                                                 + string position
                                                                                 + "%; transform: translate(-"
                                                                                 + string position
                                                                                 + "%, -50%)"}>●</div></div></div>
                    <p class="hint">Current position: {positionText}</p>
                </section>
            </div>
        """

defineElement<MotionShowcase> "motion-showcase"

let root = document.getElementById "app"

if root <> null then
    render (
        html
            $"""
            <h1>Firelight Motion</h1>
            <p class="intro">A few ways to bring movement into a Lit component from F#.</p>
            <motion-showcase></motion-showcase>
        """,
        root
    )
    |> ignore
