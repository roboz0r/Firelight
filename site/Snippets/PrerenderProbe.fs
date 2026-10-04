module Snippets.PrerenderProbe

open Browser
open Browser.Types
open Fable.Core
open Firelight
open type Firelight.Lit

/// Prerendered at build time, then hydrated: it shows which side rendered it.
[<AttachMembers>]
type PrerenderProbe() =
    inherit LitElement()

    let mutable stopListening = ignore

    static member properties =
        PropertyDeclarations.create [
            "hydrated", PropertyDeclaration<bool>(state = true)
            "width", PropertyDeclaration<float>(state = true)
            "clicks", PropertyDeclaration<int>(state = true)
        ]

    static member styles =
        css
            $$"""
        :host { display: grid; gap: 0.75rem; justify-items: start; }
        dl { display: grid; grid-template-columns: auto auto; gap: 0.25rem 1rem; margin: 0; }
        dt { font-weight: 600; }
        dd { margin: 0; }
        button { font: inherit; padding: 0.3rem 0.8rem; }
        """

    member val hydrated = false with get, set
    member val width = 0.0 with get, set
    member val clicks = 0 with get, set

    // Browser APIs only here, never in the constructor, willUpdate or render.
    override this.connectedCallback() =
        base.connectedCallback ()
        stopListening <- Ev.listen window "resize" (Ev.event (fun _ -> this.width <- window.innerWidth))

        // The first update hydrates, so it must render what the server did. Afterwards, show what
        // only the browser knows.
        promise {
            let! _ = this.updateComplete
            this.hydrated <- true
            this.width <- window.innerWidth
        }
        |> Promise.start

    override this.disconnectedCallback() =
        base.disconnectedCallback ()
        stopListening ()

    override this.render() =
        let renderedBy =
            if this.hydrated then "your browser, after hydration" else "Lit SSR, at build time"

        let width =
            if this.hydrated then $"{this.width} px" else "unknown: there's no window at build time"

        html
            $"""
        <dl>
            <dt>Rendered by</dt><dd>{renderedBy}</dd>
            <dt>Window width</dt><dd>{width}</dd>
        </dl>
        <button @click={fun _ -> this.clicks <- this.clicks + 1}>Clicked {this.clicks} times</button>"""

defineElement<PrerenderProbe> "my-prerender-probe"
