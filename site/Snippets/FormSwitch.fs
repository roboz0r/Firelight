module Snippets.FormSwitch

open Fable.Core
open Fable.Core.JsInterop
open Browser
open Browser.Types
open Firelight
open type Firelight.Lit

/// The parts of the browser's ElementInternals this control uses.
type ElementInternals =
    abstract setFormValue: value: string -> unit
    abstract role: string with get, set
    abstract ariaChecked: string with get, set

[<Emit("$0.attachInternals()")>]
let attachInternals (element: HTMLElement) : ElementInternals = jsNative

/// An on/off switch that a <form> submits like a checkbox: `name=value` while it's on.
[<AttachMembers>]
type FormSwitch() as this =
    inherit LitElement()

    // LitElement isn't an HTMLElement to F#, though it is one in the browser.
    let host = unbox<HTMLElement> this
    let internals = attachInternals host

    do
        internals.role <- "switch"
        host.addEventListener ("click", fun _ -> this.Toggle())

        host.addEventListener (
            "keydown",
            fun e ->
                if (e :?> KeyboardEvent).key = " " then
                    e.preventDefault ()
                    this.Toggle()
        )

    /// Tells the browser this element takes part in forms.
    static member formAssociated = true

    static member properties =
        PropertyDeclarations.create [
            "checked", PropertyDeclaration<bool>(``type`` = jsConstructor<Boolean>)
            "value", PropertyDeclaration<string>()
        ]

    static member styles =
        css
            $$"""
        :host { display: inline-block; vertical-align: middle; border-radius: 1rem; cursor: pointer; }
        :host(:focus-visible) { outline: 2px solid var(--fg); outline-offset: 2px; }
        .track { display: block; width: 2.6rem; height: 1.5rem; border-radius: 1rem; background: var(--muted); }
        .track.on { background: var(--accent); }
        .thumb { display: block; width: 1.1rem; height: 1.1rem; margin: 0.2rem; border-radius: 50%;
                 background: var(--bg); transition: transform 0.15s; }
        .on .thumb { transform: translateX(1.1rem); }
        """

    member val ``checked`` = false with get, set
    member val value = "on" with get, set

    /// Tells the form and assistive technology the current state.
    member this.Report() =
        internals.ariaChecked <- string this.``checked``
        internals.setFormValue (if this.``checked`` then this.value else null)

    member this.Toggle() =
        this.``checked`` <- not this.``checked``
        // Before the event, so a listener that reads the form sees the new value.
        this.Report()
        host.dispatchEvent (Event.Create("change", jsOptions<EventInit> (fun o -> o.bubbles <- true)))
        |> ignore

    /// The browser calls this when the form is reset: go back to the `checked` attribute.
    member this.formResetCallback() = this.``checked`` <- host.hasAttribute "checked"

    override this.connectedCallback() =
        base.connectedCallback ()

        if not (host.hasAttribute "tabindex") then
            host.tabIndex <- 0

    // After any change, including from code that sets `checked` or `value`.
    override this.updated _ = this.Report()

    override this.render() =
        html $"""<span class="track {if this.``checked`` then "on" else ""}"><span class="thumb"></span></span>"""

defineElement<FormSwitch> "my-form-switch"
