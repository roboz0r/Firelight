module Snippets.StylingParts

open Fable.Core
open Fable.Core.JsInterop
open Firelight
open type Firelight.Lit

/// <my-progress label="Upload" value="40"></my-progress>
/// Themed with --progress-color, and styled from outside through its label, track and bar parts.
[<AttachMembers>]
type Progress() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [
            "label", PropertyDeclaration<string>()
            "value", PropertyDeclaration<float>()
        ]

    static member styles =
        css
            $$"""
        :host { display: block; }
        :host([hidden]) { display: none; }
        .label { font-size: 0.875rem; }
        .track { height: 0.6rem; border-radius: 1rem; background: var(--border, #e8e1d8); overflow: hidden; }
        .bar { height: 100%; background: var(--progress-color, var(--accent, #c2410c)); }
        """

    member val label = "Progress" with get, set
    member val value = 0.0 with get, set

    override this.render() =
        let width = StyleInfo.create [ "width", Some $"{this.value}%%" ]

        html
            $"""
        <div class="label" part="label">{this.label}: {this.value}%%</div>
        <div class="track" part="track" role="progressbar" aria-label={this.label}
            aria-valuemin="0" aria-valuemax="100" aria-valuenow={this.value}>
            <div class="bar" part="bar" style={styleMap width}></div>
        </div>"""

defineElement<Progress> "my-progress"
