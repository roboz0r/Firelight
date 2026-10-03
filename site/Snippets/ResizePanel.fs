module Snippets.ResizePanel

open Fable.Core
open Firelight
open Firelight.Observers
open type Firelight.Lit

/// Lays itself out by its own width, not the window's. Drag the bottom-right corner to resize it.
[<AttachMembers>]
type ResizePanel() as this =
    inherit LitElement()

    let width =
        ResizeController<float>(
            this,
            ResizeControllerConfig(
                callback =
                    ResizeValueCallback(fun entries _ ->
                        if entries.Length = 0 then
                            0.0
                        else
                            entries.[0].contentRect.width
                    )
            )
        )

    static member styles =
        css
            $$"""
        :host {
            display: block; resize: horizontal; overflow: auto; box-sizing: border-box; width: 100%;
            min-width: 10rem; max-width: 100%; padding: 1rem;
            border: 1px dashed var(--border); border-radius: 0.5rem;
        }
        .boxes { display: grid; gap: 0.5rem; }
        .wide .boxes { grid-template-columns: repeat(3, 1fr); }
        .box { padding: 0.75rem; border-radius: 0.5rem; background: var(--accent); color: var(--accent-fg); text-align: center; }
        """

    override _.render() =
        let px = width.value |> Option.defaultValue 0.0
        let layout = if px >= 300.0 then "wide" else "narrow"

        html
            $"""
        <div class={layout}>
            <p>{int px}px wide, so the <strong>{layout}</strong> layout</p>
            <div class="boxes"><div class="box">One</div><div class="box">Two</div><div class="box">Three</div></div>
        </div>"""

defineElement<ResizePanel> "my-resize-panel"
