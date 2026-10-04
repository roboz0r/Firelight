module Snippets.LibrarySwitches

open Fable.Core
open Fable.Core.JsInterop
open Firelight
open type Firelight.Lit

// Registers <wa-switch>. Only the components you import are bundled.
importSideEffects "@awesome.me/webawesome/dist/components/switch/switch.js"

// Web Awesome's design tokens. An app loads them once for the whole document; this page doesn't,
// so the demo adopts them in its own shadow root, inside the wrapper with the theme's class.
[<ImportDefault("@awesome.me/webawesome/dist/styles/themes/default.css?inline")>]
let private themeCss: string = jsNative

[<AttachMembers>]
type SwitchBindings() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [ "on", PropertyDeclaration<bool>(state = true) ]

    static member styles =
        [|
            unsafeCSS themeCss
            css
                $$"""
            .wa-theme-default {
                display: grid; gap: 0.75rem; justify-items: start;
                color: inherit; background: none; color-scheme: inherit;
            }
            /* The switch's colours come from this site's palette, which follows its theme
               button. An app that loads Web Awesome's theme for the whole document adds the
               wa-dark class to <html> for dark mode instead. */
            wa-switch {
                --width: 2.75rem;
                --wa-form-control-activated-color: var(--accent);
                --wa-form-control-background-color: var(--bg);
                --wa-form-control-border-color: var(--muted);
                --wa-color-surface-default: var(--accent-fg);
                --wa-form-control-label-color: currentColor;
                --wa-form-control-value-color: currentColor;
            }
            wa-switch::part(thumb) { box-shadow: 0 1px 3px rgb(0 0 0 / 0.35); }
            p { margin: 0; }
            button { font: inherit; padding: 0.3rem 0.6rem; }
            """
        |]

    member val on = false with get, set

    override this.render() =
        html
            $"""
        <div class="wa-theme-default">
            <wa-switch ?checked={this.on} @change={Ev.checked' (fun on -> this.on <- on)}>
                Bound to the attribute
            </wa-switch>
            <wa-switch .checked={this.on} @change={Ev.checked' (fun on -> this.on <- on)}>
                Bound to the property
            </wa-switch>
            <p>The F# value is <strong>{if this.on then "on" else "off"}</strong>.</p>
            <button @click={fun _ -> this.on <- false}>Turn both off</button>
        </div>"""

defineElement<SwitchBindings> "my-switch-bindings"
