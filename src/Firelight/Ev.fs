namespace Firelight

open Browser.Types

/// <summary>
/// Typed event handlers for <c>@event</c> bindings in templates.
/// </summary>
/// <remarks>
/// <para>
/// A hole's type is <c>obj</c>, so in <c>@keydown={fun e -> ...}</c> F# can't infer the type of <c>e</c>, and an
/// annotation is needed. These functions supply it: <c>@keydown={Ev.keyboard (fun e -> ... e.key ...)}</c>.
/// Each event-typed function, from <c>Ev.event</c> to <c>Ev.custom</c>, is <c>inline</c> and returns the handler
/// it is given, so the JavaScript is just the lambda. <c>Ev.value</c> and <c>Ev.checked'</c> wrap your handler in
/// one that reads the element's value first.
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
