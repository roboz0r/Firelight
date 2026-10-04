[<AutoOpen>]
module Browser.Types

open Fable.Core
open Browser.Types

[<AllowNullLiteral>]
type ElementDefinitionOptions =
    /// String specifying the name of a built-in element to extend. Used to create a customized built-in element.
    abstract member extends: string option with get, set

[<AllowNullLiteral>]
type CustomElementRegistry =
    /// <summary>
    /// Adds a definition for a custom element to the custom element registry, mapping its name to the constructor which will be used to create it.
    /// </summary>
    /// <remarks>
    /// Use `Fable.Core.JsInterop.jsConstructor` to convert an F# type to a JavaScript constructor.
    /// </remarks>
    abstract member define: name: string * constructor: 'TConstructor * ?options: ElementDefinitionOptions -> unit
    /// Returns the constructor for a previously-defined custom element.
    abstract member get: name: string -> 'TConstructor option
    /// <summary>
    /// Returns the name for a previously-defined custom element.
    /// </summary>
    /// <remarks>
    /// Use `Fable.Core.JsInterop.jsConstructor` to convert an F# type to a JavaScript constructor.
    /// </remarks>
    abstract member getName: constructor: 'TConstructor -> string option
    /// Upgrades all shadow-containing custom elements in a Node subtree, even before they are connected to the main document.
    abstract member upgrade: root: Node -> unit
    /// Returns a Promise that fulfills with the custom element's constructor when the named element is defined.
    abstract member whenDefined: name: string -> JS.Promise<'TConstructor>

type Window with
    /// Returns the CustomElementRegistry for the current document.
    [<Emit("$0.customElements")>]
    member _.customElements: CustomElementRegistry = nativeOnly

[<AllowNullLiteral>]
type HTMLTemplateElement =
    inherit HTMLElement
    abstract member content: DocumentFragment with get
    abstract member shadowRootMode: string option with get, set
    abstract member shadowRootDelegatesFocus: bool option with get, set
    abstract member shadowRootClonable: bool option with get, set
    abstract member shadowRootSerializable: bool option with get, set

type HTMLAnchorElement with
    /// Returns a string containing the Unicode serialization of the origin of the &lt;a&gt; element's href.
    [<Emit("$0.origin")>]
    member _.origin: string = nativeOnly

open System
open System.Runtime.InteropServices

type Browser.Types.Event with
    /// <summary>
    /// A <c>CustomEvent</c> named <c>typeName</c> that carries <c>detail</c>. It bubbles and is composed unless
    /// you say otherwise, so it leaves the component's shadow root and reaches the page.
    /// </summary>
    /// <remarks>
    /// Pass <c>cancelable = true</c> for an event that asks permission: <c>dispatchEvent</c> then returns
    /// <c>false</c> when a listener called <c>preventDefault</c>.
    /// </remarks>
    /// <param name="typeName">The event's name, such as <c>"count-changed"</c>.</param>
    /// <param name="detail">The value listeners read as <c>e.detail</c>.</param>
    /// <param name="bubbles">Whether it travels up through the element's ancestors. Default true.</param>
    /// <param name="composed">Whether it crosses shadow root boundaries. Default true.</param>
    /// <param name="cancelable">Whether a listener's <c>preventDefault</c> cancels it. Default false.</param>
    static member inline customEvent
        (
            typeName: string,
            detail: 'T,
            [<Optional; DefaultParameterValue(true)>] bubbles: bool,
            [<Optional; DefaultParameterValue(true)>] composed: bool,
            [<Optional; DefaultParameterValue(false)>] cancelable: bool
        ) =
        let eventInit =
            Fable.Core.JsInterop.jsOptions<CustomEventInit<'T>>(fun o ->
                o.detail <- Some detail
                o.bubbles <- bubbles
                o.composed <- composed
                o.cancelable <- cancelable
            )

        CustomEvent.Create(typeName, eventInit)

// With `open Browser`, `Event` names Browser.Dom's `Event` value, an `EventType`, which hides the
// extension above. The same member on `EventType` makes `Event.customEvent` work either way.
type Browser.Types.EventType with
    /// <summary>
    /// A <c>CustomEvent</c> named <c>typeName</c> that carries <c>detail</c>; the same as the static
    /// <c>Event.customEvent</c>, for when <c>open Browser</c> makes <c>Event</c> the browser's value.
    /// </summary>
    /// <param name="typeName">The event's name, such as <c>"count-changed"</c>.</param>
    /// <param name="detail">The value listeners read as <c>e.detail</c>.</param>
    /// <param name="bubbles">Whether it travels up through the element's ancestors. Default true.</param>
    /// <param name="composed">Whether it crosses shadow root boundaries. Default true.</param>
    /// <param name="cancelable">Whether a listener's <c>preventDefault</c> cancels it. Default false.</param>
    member inline _.customEvent
        (
            typeName: string,
            detail: 'T,
            [<Optional; DefaultParameterValue(true)>] bubbles: bool,
            [<Optional; DefaultParameterValue(true)>] composed: bool,
            [<Optional; DefaultParameterValue(false)>] cancelable: bool
        ) =
        Browser.Types.Event.customEvent (typeName, detail, bubbles, composed, cancelable)
