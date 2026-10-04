module Snippets.ScrollMarker

open Fable.Core
open Firelight
open Firelight.Observers
open type Firelight.Lit

/// Says whether the marker inside its scrolling box is in view.
[<AttachMembers>]
type ScrollMarker() as this =
    inherit LitElement()

    // target = null: observe only the element that target () marks in the template, not the host.
    let marker =
        IntersectionController(
            this,
            IntersectionControllerConfig(
                target = null,
                callback = IntersectionValueCallback(fun entries _ -> entries |> Array.exists _.isIntersecting)
            )
        )

    static member styles =
        css
            $$"""
        :host { display: grid; gap: 0.5rem; }
        p { margin: 0; }
        .box { height: 7rem; overflow-y: auto; border: 1px solid var(--border, #ccc); border-radius: 0.5rem; padding: 0 0.75rem; }
        .spacer { height: 14rem; padding-top: 0.5rem; }
        .marker { margin-bottom: 0.75rem; padding: 0.4rem 0.75rem; border-radius: 0.4rem; background: #1971c2; color: white; }
        """

    override _.render() =
        let inView = marker.value = Some true

        html
            $"""
        <p role="status">The marker is {if inView then "in view" else "out of view"}.</p>
        <div class="box" tabindex="0" aria-label="Scrolling box">
            <div class="spacer">Scroll down.</div>
            <div class="marker" {marker.target ()}>Marker</div>
        </div>"""

defineElement<ScrollMarker> "my-scroll-marker"
