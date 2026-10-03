module Snippets.TemplateDirectives

open Fable.Core
open Browser.Types
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type VolumeControl() =
    inherit LitElement()

    let slider = createRef<HTMLInputElement> ()

    static member properties =
        PropertyDeclarations.create [ "level", PropertyDeclaration<int>(state = true) ]

    static member styles =
        css
            $$"""
        :host { display: grid; gap: 0.75rem; justify-items: start; }
        .track { width: 14rem; height: 0.75rem; border: 1px solid var(--border); border-radius: 1rem; overflow: hidden; }
        .bar { height: 100%; background: var(--accent); }
        .bar.loud { background: repeating-linear-gradient(45deg, var(--accent) 0 6px, var(--fg) 6px 12px); }
        button { font: inherit; padding: 0.2rem 0.6rem; }
        """

    member val level = 50 with get, set

    member this.Reset() =
        this.level <- 50
        // The ref's value is the rendered <input>, once there is one.
        slider.value |> Option.iter (fun input -> input.focus ())

    override this.render() =
        let bar = ClassInfo.create [ "loud", this.level > 80 ]
        let width = StyleInfo.create [ "width", Some(string this.level + "%") ]

        html
            $"""
        <label>
            Volume
            <input type="range" min="0" max="100" {ref slider} .value={string this.level}
                @input={fun (e: Event) -> this.level <- int (e.target :?> HTMLInputElement).value}>
        </label>
        <div class="track"><div class="bar {classMap bar}" style={styleMap width}></div></div>
        <button @click={fun _ -> this.Reset()}>Reset</button>"""

defineElement<VolumeControl> "my-volume-control"
