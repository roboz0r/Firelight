namespace Firelight

open Fable.Core
open Browser.Types

/// <summary>
/// A <c>&lt;slot&gt;</c> element in a component's shadow root: the place where the page's children of the
/// component show. Fable.Browser.Dom has no <c>HTMLSlotElement</c>, so Firelight binds it.
/// </summary>
/// <remarks>
/// <para>
/// Get one from an event with <c>Ev.slot</c>, bound to <c>@slotchange</c> on the slot, or from the render root
/// with <c>this.query&lt;HTMLSlotElement&gt; "slot[name=footer]"</c>, or type a ref with it:
/// <c>createRef&lt;HTMLSlotElement&gt; ()</c>.
/// </para>
/// <para>
/// It lives in the <c>Firelight</c> namespace, not <c>Browser.Types</c>, so it stays one type whichever of
/// <c>Browser</c>, <c>Browser.Dom</c> and <c>Browser.Types</c> are open as well.
/// </para>
/// </remarks>
/// <seealso href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLSlotElement"/>
[<AllowNullLiteral>]
type HTMLSlotElement =
    inherit HTMLElement

    /// <summary>
    /// The slot's <c>name</c> attribute: children with a matching <c>slot</c> attribute go to it. <c>""</c> for
    /// the default slot.
    /// </summary>
    abstract name: string with get, set

    /// <summary>
    /// The nodes assigned to the slot, text nodes included: <c>slot.assignedNodes ()</c>. Empty when nothing is
    /// assigned, even if the slot shows fallback content.
    /// </summary>
    /// <param name="flatten">
    /// With <c>true</c>, a <c>&lt;slot&gt;</c> among the assigned nodes is replaced by the nodes assigned to it, and
    /// a slot with nothing assigned returns its fallback content: <c>slot.assignedNodes (flatten = true)</c>.
    /// </param>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLSlotElement/assignedNodes"/>
    [<ParamObject>]
    abstract assignedNodes: ?flatten: bool -> Node[]

    /// <summary>
    /// The elements assigned to the slot, without text nodes: <c>slot.assignedElements ()</c>. Empty when nothing
    /// is assigned, even if the slot shows fallback content.
    /// </summary>
    /// <param name="flatten">
    /// With <c>true</c>, a <c>&lt;slot&gt;</c> among the assigned elements is replaced by the elements assigned to it,
    /// and a slot with nothing assigned returns its fallback elements: <c>slot.assignedElements (flatten = true)</c>.
    /// </param>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLSlotElement/assignedElements"/>
    [<ParamObject>]
    abstract assignedElements: ?flatten: bool -> Element[]

    /// <summary>
    /// Assigns <c>nodes</c>, elements that are children of the host, to the slot, replacing what was assigned to
    /// it: <c>slot.assign (first, second)</c>, or <c>slot.assign ()</c> to empty it. The DOM takes only elements
    /// and text nodes, so the overloads take <c>Element</c>, <c>Text</c> or <c>U2&lt;Element, Text&gt;</c>.
    /// </summary>
    /// <remarks>
    /// Only for a shadow root with manual slot assignment: <c>slotAssignment: "manual"</c> in the component's
    /// <c>static member shadowRootOptions</c>. Fable.Browser.Dom's <c>ShadowRootInit</c> has no field for it, so set
    /// it with <c>o?slotAssignment &lt;- "manual"</c> in <c>jsOptions</c>. With the default, named assignment, the
    /// browser assigns children by their <c>slot</c> attribute, and <c>assign</c> has no effect.
    /// </remarks>
    /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLSlotElement/assign"/>
    abstract assign: [<System.ParamArray>] nodes: Element[] -> unit

    /// <summary>Assigns text nodes that are children of the host to the slot: <c>slot.assign (text)</c>.</summary>
    /// <remarks>See the <c>Element</c> overload. For elements and text nodes together, pass <c>U2</c> values.</remarks>
    abstract assign: [<System.ParamArray>] nodes: Text[] -> unit

    /// <summary>
    /// Assigns elements and text nodes together, in order: <c>slot.assign (U2.Case1 heading, U2.Case2 text)</c>.
    /// </summary>
    /// <remarks>See the <c>Element</c> overload.</remarks>
    abstract assign: [<System.ParamArray>] nodes: U2<Element, Text>[] -> unit

    /// <summary>Empties the slot: <c>slot.assign ()</c>.</summary>
    /// <remarks>See the <c>Element</c> overload.</remarks>
    abstract assign: unit -> unit

/// <summary>
/// <c>assignedSlot</c> on elements and text nodes, which Fable.Browser.Dom doesn't have.
/// </summary>
[<AutoOpen>]
module SlotExtensions =
    type Element with
        /// <summary>
        /// The <c>&lt;slot&gt;</c> the element is assigned to, or <c>None</c> when it isn't in one, or the slot is in
        /// a closed shadow root.
        /// </summary>
        /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/API/Element/assignedSlot"/>
        [<Emit("$0.assignedSlot")>]
        member _.assignedSlot: HTMLSlotElement option = nativeOnly

    type Text with
        /// <summary>
        /// The <c>&lt;slot&gt;</c> the text node is assigned to, or <c>None</c> when it isn't in one, or the slot is
        /// in a closed shadow root.
        /// </summary>
        /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/API/Text/assignedSlot"/>
        [<Emit("$0.assignedSlot")>]
        member _.assignedSlot: HTMLSlotElement option = nativeOnly
