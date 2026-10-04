module Snippets.LifecycleLog

open Fable.Core
open Browser.Types
open Firelight
open type Firelight.Lit

let private names (changed: System.Collections.Generic.Dictionary<string, obj>) =
    String.concat ", " changed.Keys

/// The component under study. Each lifecycle method it overrides writes a line to the log.
[<AttachMembers>]
type LifecycleProbe() =
    inherit LitElement()

    // Lines written before the demo has said where to write them.
    let early = ResizeArray<string>()
    let mutable write: string -> unit = early.Add
    let log line = write line

    do log "constructor"

    static member properties =
        PropertyDeclarations.create [
            "count", PropertyDeclaration<int>(state = true)
            "held", PropertyDeclaration<bool>()
        ]

    static member styles = css $$"""button { font: inherit; padding: 0.3rem 0.8rem; }"""

    member val count = 0 with get, set
    member val held = false with get, set

    /// Where to write the log. A plain property: setting it doesn't need an update.
    member _.logTo
        with set (f: string -> unit) =
            write <- f
            early |> Seq.iter f
            early.Clear()

    override this.connectedCallback() =
        base.connectedCallback ()
        log "connectedCallback"

    override this.disconnectedCallback() =
        base.disconnectedCallback ()
        log "disconnectedCallback"

    override this.shouldUpdate(changed) =
        log $"shouldUpdate ({names changed}): {not this.held}"
        not this.held

    override this.willUpdate(changed) = log $"willUpdate ({names changed})"

    override this.update(changed) =
        log "update"
        base.update changed

    override this.firstUpdated(_) = log "firstUpdated"
    override this.updated(changed) = log $"updated ({names changed})"

    member this.AddOne() =
        this.count <- this.count + 1

        promise {
            let! _ = this.updateComplete
            log "updateComplete resolved"
        }
        |> Promise.start

    override this.render() =
        log "render"
        html $"""<button @click={fun _ -> this.AddOne()}>Count: {this.count}</button>"""

/// The controls and the log.
[<AttachMembers>]
type LifecycleDemo() as this =
    inherit LitElement()

    let logList = createRef<HTMLElement> ()

    // The probe writes during updates, its own and this one's (it disconnects while this renders),
    // so each line is added after the current update.
    let write (line: string) =
        JS.setTimeout (fun () -> this.lines <- this.lines @ [ line ]) 0 |> ignore

    static member properties =
        PropertyDeclarations.create [
            "lines", PropertyDeclaration<string list>(state = true)
            "shown", PropertyDeclaration<bool>(state = true)
            "held", PropertyDeclaration<bool>(state = true)
        ]

    static member styles =
        css
            $$"""
        :host { display: grid; gap: 0.75rem; width: min(100%, 26rem); }
        .controls { display: flex; flex-wrap: wrap; gap: 0.5rem 1rem; align-items: center; }
        button { font: inherit; padding: 0.3rem 0.8rem; }
        ol {
            margin: 0; padding: 0.5rem 0.5rem 0.5rem 2.5rem; height: 14rem; overflow: auto;
            font: 0.85rem var(--mono); background: var(--bg); border: 1px solid var(--border);
            border-radius: var(--radius);
        }
        """

    member val lines: string list = [] with get, set
    member val shown = true with get, set
    member val held = false with get, set

    // DOM work after an update: keep the newest line in view.
    override this.updated(changed) =
        if changed.ContainsKey "lines" then
            logList.value |> Option.iter (fun ol -> ol.scrollTop <- ol.scrollHeight)

    override this.render() =
        let probe =
            if this.shown then
                html $"""<my-lifecycle-probe .logTo={write} .held={this.held}></my-lifecycle-probe>"""
            else
                nothing

        html
            $"""
        <div class="controls">
            {probe}
            <label>
                <input type="checkbox" ?checked={this.held} @change={Ev.checked' (fun on -> this.held <- on)}>
                Hold updates
            </label>
        </div>
        <div class="controls">
            <button @click={fun _ -> this.shown <- not this.shown}>
                {if this.shown then "Remove it" else "Put it back"}
            </button>
            <button @click={fun _ -> this.lines <- []}>Clear the log</button>
        </div>
        <ol {ref logList} aria-label="Lifecycle log">{this.lines |> List.map (fun line -> html $"<li>{line}</li>")}</ol>"""

defineElement<LifecycleProbe> "my-lifecycle-probe"
defineElement<LifecycleDemo> "my-lifecycle-demo"
