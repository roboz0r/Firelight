module App

open Fable.Core
open Browser
open Firelight
open type Firelight.Lit
open GettingStarted
open GettingStarted.Components

[<AttachMembers>]
type ComponentsDemo() =
    inherit LitElement()

    static member styles = [| Components.styles |]

    override _.render() =
        let noProps = html $" "

        html
            $"""
        <div style="display:flex; flex-direction:column; gap:1.25rem;">
            <div style="display:flex; flex-wrap:wrap; gap:0.5rem; align-items:center;">
                {Button.button "" Button.Variant.Default Button.Size.Default (html $"Default") noProps}
                {Button.button "" Button.Variant.Destructive Button.Size.Default (html $"Destructive") noProps}
                {Button.button "" Button.Variant.Outline Button.Size.Default (html $"Outline") noProps}
                {Button.button "" Button.Variant.Secondary Button.Size.Default (html $"Secondary") noProps}
                {Button.button "" Button.Variant.Ghost Button.Size.Default (html $"Ghost") noProps}
                {Button.button "" Button.Variant.Link Button.Size.Default (html $"Link") noProps}
            </div>
            <div style="display:flex; flex-wrap:wrap; gap:0.5rem; align-items:center;">
                {Button.button "" Button.Variant.Default Button.Size.Small (html $"Small") noProps}
                {Button.button "" Button.Variant.Default Button.Size.Default (html $"Default") noProps}
                {Button.button "" Button.Variant.Default Button.Size.Large (html $"Large") noProps}
            </div>
            <div style="max-width:20rem;">
                {Input.input "" "text" (html $"placeholder=\"Email address\"")}
                <div style="height:0.5rem;"></div>
                {Input.input "" "password" (html $"placeholder=\"Password\" disabled")}
            </div>
        </div>"""

let start () =
    defineElement<ComponentsDemo> "components-demo"
    defineElement<Counter> "elmish-counter"
    defineElement<SimpleGreeting> "simple-greeting"
    defineElement<MyPage> "my-page"
    defineElement<MyReactive> "my-reactive"
    defineElement<CustomChangeDetection> "custom-change-detection"
    defineElement<ClassStyling> "class-styling"
    defineElement<SlottedStyling> "slotted-styling"
    defineElement<DynamicStyling> "dynamic-styling"
    defineElement<EventListeners> "event-listeners"
    defineElement<ClockElement> "clock-element"
    defineElement<MyContextElement> "my-context-element"
    defineElement<MyContextApp> "my-context-app"

    let el = document.getElementById "app"

    if el <> null then
        // $$""" strings treat single { and } as literal, so CSS braces need no escaping.
        render (
            html
                $$"""
            <style>
                *, *::before, *::after { box-sizing: border-box; }
                body { margin: 0; background: #f8fafc; color: #0f172a; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; }
                .gs-page { max-width: 680px; margin: 0 auto; padding: 3rem 1.5rem; }
                .gs-header { margin-bottom: 3rem; border-bottom: 2px solid #0f172a; padding-bottom: 1.5rem; }
                .gs-header h1 { font-size: 1.75rem; font-weight: 800; letter-spacing: -0.03em; margin: 0 0 0.25rem; }
                .gs-header p { color: #64748b; font-size: 0.875rem; margin: 0; }
                .gs-section { margin-bottom: 0; padding: 1.75rem 0; border-bottom: 1px solid #e2e8f0; }
                .gs-section:last-child { border-bottom: none; }
                .gs-tag { font-size: 0.7rem; font-weight: 700; letter-spacing: 0.1em; text-transform: uppercase; color: #94a3b8; margin-bottom: 0.875rem; }
                .gs-row { display: flex; flex-wrap: wrap; gap: 0.75rem; align-items: flex-start; }
            </style>

            <div class="gs-page">
                <header class="gs-header">
                    <h1>Firelight</h1>
                    <p>Lit web component bindings for F# via Fable &mdash; getting started examples</p>
                </header>

                <section class="gs-section">
                    <div class="gs-tag">Defining</div>
                    <div class="gs-row">
                        <simple-greeting></simple-greeting>
                        <simple-greeting name="Rob"></simple-greeting>
                    </div>
                </section>

                <section class="gs-section">
                    <div class="gs-tag">Rendering</div>
                    <my-page></my-page>
                </section>

                <section class="gs-section">
                    <div class="gs-tag">Reactive Properties</div>
                    <div class="gs-row">
                        <my-reactive></my-reactive>
                        <custom-change-detection></custom-change-detection>
                    </div>
                </section>

                <section class="gs-section">
                    <div class="gs-tag">Styles</div>
                    <div class="gs-row">
                        <class-styling></class-styling>
                        <class-styling class="blue"></class-styling>
                        <slotted-styling>
                            <p>Slotted content</p>
                            <span slot="hi">Named slot</span>
                        </slotted-styling>
                        <dynamic-styling></dynamic-styling>
                    </div>
                </section>

                <section class="gs-section">
                    <div class="gs-tag">Events</div>
                    <event-listeners name="World"></event-listeners>
                </section>

                <section class="gs-section">
                    <div class="gs-tag">Controllers</div>
                    <clock-element tickRate=2000></clock-element>
                </section>

                <section class="gs-section">
                    <div class="gs-tag">Context</div>
                    <my-context-app>
                        <my-context-element></my-context-element>
                    </my-context-app>
                </section>

                <section class="gs-section">
                    <div class="gs-tag">Components</div>
                    <components-demo></components-demo>
                </section>

                <section class="gs-section">
                    <div class="gs-tag">Elmish</div>
                    <elmish-counter start=10></elmish-counter>
                </section>
            </div>
            """,
            el
        )
        |> ignore

do start ()
