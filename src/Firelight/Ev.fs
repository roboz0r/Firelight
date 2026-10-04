namespace Firelight

open Browser.Types
open Fable.Core.JsInterop

/// <summary>
/// Typed event handlers for <c>@event</c> bindings in templates.
/// </summary>
/// <remarks>
/// <para>
/// A hole's type is <c>obj</c>, so in <c>@keydown={fun e -> ...}</c> F# can't infer the type of <c>e</c>, and an
/// annotation is needed. These functions supply it: <c>@keydown={Ev.keyboard (fun e -> ... e.key ...)}</c>.
/// Each event-typed function, from <c>Ev.event</c> to <c>Ev.custom</c>, is <c>inline</c> and returns the handler
/// it is given, so the JavaScript is just the lambda. <c>Ev.value</c>, <c>Ev.checked'</c> and <c>Ev.valueAs</c>
/// wrap your handler in one that reads the element's value first, and <c>Ev.slot</c> in one that passes the
/// <c>&lt;slot&gt;</c> itself.
/// </para>
/// <para>
/// Nothing checks that the event name matches the type: <c>@click={Ev.keyboard (fun e -> ...)}</c> compiles,
/// because the name is fixed text in the template that only Lit reads. Pick the function for the event you bind.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// html $"""&lt;input @keydown={Ev.keyboard (fun e -&gt; if e.key = "Enter" then this.Save())}&gt;"""
/// </code>
/// </example>
[<RequireQualifiedAccess>]
module Ev =
    /// <summary>
    /// A handler for any event, typed as the base <c>Event</c>: for events that have no more specific
    /// interface here, such as <c>change</c>, or when the handler only needs <c>preventDefault</c>.
    /// </summary>
    /// <remarks>Nothing checks the bound event's name against the type; see <see cref="T:Firelight.Ev"/>.</remarks>
    let inline event (handler: Event -> unit) : Event -> unit = handler

    /// <summary>
    /// A <c>MouseEvent</c> handler: <c>click</c>, <c>dblclick</c>, <c>mousedown</c>, <c>mouseup</c>,
    /// <c>mousemove</c>, <c>mouseenter</c>, <c>mouseleave</c>, <c>contextmenu</c> and the other mouse events.
    /// </summary>
    /// <remarks>Nothing checks the bound event's name against the type; see <see cref="T:Firelight.Ev"/>.</remarks>
    let inline mouse (handler: MouseEvent -> unit) : MouseEvent -> unit = handler

    /// <summary>
    /// A <c>PointerEvent</c> handler: <c>pointerdown</c>, <c>pointerup</c>, <c>pointermove</c>,
    /// <c>pointerenter</c>, <c>pointerleave</c>, <c>pointercancel</c> and the other pointer events.
    /// </summary>
    /// <remarks>Nothing checks the bound event's name against the type; see <see cref="T:Firelight.Ev"/>.</remarks>
    let inline pointer (handler: PointerEvent -> unit) : PointerEvent -> unit = handler

    /// <summary>
    /// A <c>KeyboardEvent</c> handler: <c>keydown</c> and <c>keyup</c>.
    /// </summary>
    /// <remarks>Nothing checks the bound event's name against the type; see <see cref="T:Firelight.Ev"/>.</remarks>
    let inline keyboard (handler: KeyboardEvent -> unit) : KeyboardEvent -> unit = handler

    /// <summary>
    /// A <c>FocusEvent</c> handler: <c>focus</c>, <c>blur</c>, <c>focusin</c> and <c>focusout</c>.
    /// </summary>
    /// <remarks>Nothing checks the bound event's name against the type; see <see cref="T:Firelight.Ev"/>.</remarks>
    let inline focus (handler: FocusEvent -> unit) : FocusEvent -> unit = handler

    /// <summary>
    /// An <c>InputEvent</c> handler: <c>input</c> and <c>beforeinput</c> on editable elements.
    /// </summary>
    /// <remarks>
    /// To read the field's new value, <c>Ev.value</c> is shorter. Nothing checks the bound
    /// event's name against the type; see <see cref="T:Firelight.Ev"/>.
    /// </remarks>
    let inline input (handler: InputEvent -> unit) : InputEvent -> unit = handler

    /// <summary>
    /// A <c>WheelEvent</c> handler: <c>wheel</c>.
    /// </summary>
    /// <remarks>Nothing checks the bound event's name against the type; see <see cref="T:Firelight.Ev"/>.</remarks>
    let inline wheel (handler: WheelEvent -> unit) : WheelEvent -> unit = handler

    /// <summary>
    /// A <c>DragEvent</c> handler: <c>dragstart</c>, <c>drag</c>, <c>dragenter</c>, <c>dragover</c>,
    /// <c>dragleave</c>, <c>drop</c> and <c>dragend</c>.
    /// </summary>
    /// <remarks>Nothing checks the bound event's name against the type; see <see cref="T:Firelight.Ev"/>.</remarks>
    let inline drag (handler: DragEvent -> unit) : DragEvent -> unit = handler

    /// <summary>
    /// A <c>TouchEvent</c> handler: <c>touchstart</c>, <c>touchmove</c>, <c>touchend</c> and <c>touchcancel</c>.
    /// </summary>
    /// <remarks>Nothing checks the bound event's name against the type; see <see cref="T:Firelight.Ev"/>.</remarks>
    let inline touch (handler: TouchEvent -> unit) : TouchEvent -> unit = handler

    /// <summary>
    /// A <c>SubmitEvent</c> handler: <c>submit</c> on a <c>&lt;form&gt;</c>. Its <c>submitter</c> is the
    /// button that submitted the form.
    /// </summary>
    /// <remarks>Nothing checks the bound event's name against the type; see <see cref="T:Firelight.Ev"/>.</remarks>
    let inline submit (handler: SubmitEvent -> unit) : SubmitEvent -> unit = handler

    /// <summary>
    /// A handler for a <c>CustomEvent</c> whose <c>detail</c> is a <c>'T</c>, such as one raised with
    /// <c>Event.customEvent</c>: <c>@count-changed={Ev.custom&lt;int&gt; (fun e -&gt; ...)}</c>.
    /// </summary>
    /// <remarks>
    /// <c>e.detail</c> is a <c>'T option</c>, <c>None</c> when the event has no detail. Nothing checks the event's
    /// name, or that its detail is a <c>'T</c>; see <see cref="T:Firelight.Ev"/>.
    /// </remarks>
    let inline custom<'T> (handler: CustomEvent<'T> -> unit) : CustomEvent<'T> -> unit = handler

    /// <summary>
    /// A handler that receives the <c>value</c> of the element the binding is on, as a string: for
    /// <c>@input</c> or <c>@change</c> on an <c>&lt;input&gt;</c>, <c>&lt;select&gt;</c> or <c>&lt;textarea&gt;</c>.
    /// </summary>
    /// <remarks>
    /// It reads <c>value</c> from the event's <c>currentTarget</c>, the element Lit bound the listener to, so
    /// bind it on the field itself, not on a parent such as a <c>&lt;form&gt;</c>. A custom element that has a
    /// string <c>value</c> property works too. Nothing checks the bound event's name or the element; see
    /// <see cref="T:Firelight.Ev"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// html $"""&lt;input .value={this.name} @input={Ev.value (fun v -&gt; this.name &lt;- v)}&gt;"""
    /// </code>
    /// </example>
    let inline value ([<InlineIfLambda>] handler: string -> unit) : Event -> unit =
        fun (e: Event) -> handler (e.currentTarget :?> HTMLInputElement).value

    /// <summary>
    /// A handler that receives whether the checkbox or radio button the binding is on is checked: for
    /// <c>@change</c> on an <c>&lt;input type="checkbox"&gt;</c> or <c>&lt;input type="radio"&gt;</c>.
    /// </summary>
    /// <remarks>
    /// Named with a quote because <c>checked</c> is reserved in F#. It reads <c>checked</c> from the event's
    /// <c>currentTarget</c>, the element Lit bound the listener to. Nothing checks the bound event's name or the
    /// element; see <see cref="T:Firelight.Ev"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// html $"""&lt;input type="checkbox" .checked={done} @change={Ev.checked' (fun on -&gt; setDone on)}&gt;"""
    /// </code>
    /// </example>
    let inline checked' ([<InlineIfLambda>] handler: bool -> unit) : Event -> unit =
        fun (e: Event) -> handler (e.currentTarget :?> HTMLInputElement).``checked``

    /// <summary>
    /// A handler that receives the <c>value</c> property of the element the binding is on as a <c>'T</c>, for
    /// elements whose <c>value</c> isn't a string, such as a slider component whose <c>value</c> is a number:
    /// <c>@input={Ev.valueAs&lt;float&gt; setVolume}</c>.
    /// </summary>
    /// <remarks>
    /// It reads <c>value</c> from the event's <c>currentTarget</c> and unboxes it: it doesn't parse or convert
    /// anything, and nothing checks that the value is a <c>'T</c>. A native <c>&lt;input type="number"&gt;</c>'s
    /// <c>value</c> is still a string, so <c>Ev.valueAs&lt;float&gt;</c> on it passes your handler a string; use
    /// <c>Ev.value</c> and parse it. Nothing checks the bound event's name or the element; see
    /// <see cref="T:Firelight.Ev"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// html $"""&lt;wa-slider .value={volume} @input={Ev.valueAs&lt;float&gt; (fun v -&gt; setVolume v)}&gt;&lt;/wa-slider&gt;"""
    /// </code>
    /// </example>
    let inline valueAs<'T> ([<InlineIfLambda>] handler: 'T -> unit) : Event -> unit =
        fun (e: Event) -> handler (unbox<'T> e.currentTarget?value)

    /// <summary>
    /// A handler that receives the <c>&lt;slot&gt;</c> the binding is on, for <c>@slotchange</c>, which fires when
    /// the nodes assigned to the slot change: <c>@slotchange={Ev.slot (fun slot -&gt; ...)}</c>.
    /// </summary>
    /// <remarks>
    /// It passes the event's <c>currentTarget</c>, the element Lit bound the listener to, as an
    /// <see cref="T:Firelight.HTMLSlotElement"/>, so bind it on the <c>&lt;slot&gt;</c> itself. <c>slotchange</c>
    /// bubbles, so on a parent element <c>currentTarget</c> is the parent, not a slot. Nothing checks the bound
    /// event's name or the element; see <see cref="T:Firelight.Ev"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// html $"""&lt;slot @slotchange={Ev.slot (fun slot -&gt; this.count &lt;- slot.assignedElements().Length)}&gt;&lt;/slot&gt;"""
    /// </code>
    /// </example>
    let inline slot ([<InlineIfLambda>] handler: HTMLSlotElement -> unit) : Event -> unit =
        fun (e: Event) -> handler (e.currentTarget :?> HTMLSlotElement)

    /// <summary>
    /// Adds <c>handler</c> as a listener for <c>eventName</c> on <c>target</c>, and returns a function that
    /// removes it: <c>let stop = Ev.listen window "keydown" (Ev.keyboard (fun e -&gt; ...))</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For listeners outside a component's template, such as on <c>window</c> or <c>document</c>: call it in
    /// <c>connectedCallback</c> or a controller's <c>hostConnected</c>, keep the remover, and call it in
    /// <c>disconnectedCallback</c> or <c>hostDisconnected</c>.
    /// </para>
    /// <para>
    /// <c>removeEventListener</c> only removes the very function it was given. A method, or a function bound
    /// with <c>let</c> in a class, becomes a new JavaScript function each time it's passed, so removing it
    /// directly removes nothing. The remover holds the one function <c>listen</c> added, so it always works.
    /// </para>
    /// <para>
    /// The handler's event type, <c>'E</c>, is unchecked: <c>listen</c> casts each event to it. Nothing checks
    /// it against the event's name.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// override this.connectedCallback() =
    ///     base.connectedCallback ()
    ///     stopListening &lt;- Ev.listen window "keydown" (Ev.keyboard (fun e -&gt; this.key &lt;- e.key))
    ///
    /// override this.disconnectedCallback() =
    ///     base.disconnectedCallback ()
    ///     stopListening ()
    /// </code>
    /// </example>
    let listen<'E when 'E :> Event> (target: EventTarget) (eventName: string) (handler: 'E -> unit) : unit -> unit =
        let listener (e: Event) = handler (unbox<'E> e)
        target.addEventListener (eventName, listener)
        fun () -> target.removeEventListener (eventName, listener)
