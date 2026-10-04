module Snippets.StopwatchTimers

open Fable.Core
open Firelight
open type Firelight.Lit

/// Milliseconds from a clock that only goes forward, unlike the time of day.
[<Emit("performance.now()")>]
let private now () : float = jsNative

/// Measures time while running. It re-renders its host every 100 ms, but only while the host is
/// on the page: the time is worked out from the clock, so it stays right while nothing renders.
type Stopwatch(host: ReactiveControllerHost) as this =
    let mutable counted = 0.0 // ms, before the latest start
    let mutable startedAt: float option = None
    let mutable timer: int option = None
    let mutable connected = false

    let startTicking () =
        if connected && timer.IsNone then
            timer <- Some(JS.setInterval (fun () -> host.requestUpdate ()) 100)

    let stopTicking () =
        timer |> Option.iter JS.clearInterval
        timer <- None

    do host.addController this

    member _.Running = startedAt.IsSome

    /// Milliseconds counted so far.
    member _.Elapsed =
        match startedAt with
        | Some t -> counted + now () - t
        | None -> counted

    member _.Start() =
        if startedAt.IsNone then
            startedAt <- Some(now ())
            startTicking ()
            host.requestUpdate ()

    member this.Stop() =
        counted <- this.Elapsed
        startedAt <- None
        stopTicking ()
        host.requestUpdate ()

    member this.Reset() =
        this.Stop()
        counted <- 0.0

    interface ReactiveController with
        member _.hostConnected() =
            connected <- true

            if startedAt.IsSome then
                startTicking ()

        member _.hostDisconnected() =
            connected <- false
            stopTicking ()
        member _.hostUpdate() = ()
        member _.hostUpdated() = ()

let private styles =
    css
        $$"""
    :host { display: grid; gap: 0.5rem; justify-items: start; }
    .time { font: 600 1.6rem var(--mono); }
    button { font: inherit; padding: 0.3rem 0.8rem; }
    """

let private startStop (watch: Stopwatch) =
    if watch.Running then
        html $"""<button @click={fun _ -> watch.Stop()}>Stop</button>"""
    else
        html $"""<button @click={fun _ -> watch.Start()}>Start</button>"""

/// Counts up.
[<AttachMembers>]
type StopwatchView() as this =
    inherit LitElement()

    let watch = Stopwatch(this)

    static member styles = styles

    override _.render() =
        let seconds = watch.Elapsed / 1000.0

        html
            $"""
        <span class="time">{seconds.ToString "0.0"} s</span>
        <div>{startStop watch} <button @click={fun _ -> watch.Reset()}>Reset</button></div>"""

/// Counts down from ten seconds, with the same controller.
[<AttachMembers>]
type CountdownView() as this =
    inherit LitElement()

    let watch = Stopwatch(this)
    let length = 10000.0

    static member styles = styles

    // Stop at zero. Stopping here is part of this update, not another one.
    override _.willUpdate(_) =
        if watch.Running && watch.Elapsed >= length then
            watch.Stop()

    override _.render() =
        let left = max 0.0 (length - watch.Elapsed)

        let status =
            if left = 0.0 then
                html $"""<span class="time">Done</span>"""
            else
                html $"""<span class="time">{ceil (left / 1000.0)} s left</span>"""

        html
            $"""
        {status}
        <progress max={length} value={length - left} aria-label="Countdown"></progress>
        <div>{startStop watch} <button @click={fun _ -> watch.Reset()}>Reset</button></div>"""

defineElement<StopwatchView> "my-stopwatch"
defineElement<CountdownView> "my-countdown"
