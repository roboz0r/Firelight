namespace Firelight
// reactive-controller.d.ts
open Fable.Core
open Fable.Core.JsInterop
open System

/// <summary>
/// An object that can host Reactive Controllers and call their lifecycle callbacks.
/// </summary>
/// <seealso href="https://lit.dev/docs/api/controllers/#ReactiveController"/>
[<AllowNullLiteral>]
[<Interface>]
type ReactiveController =
    /// <summary>
    /// Called when the host is connected to the component tree. For custom
    /// element hosts, this corresponds to the <c>connectedCallback()</c> lifecycle,
    /// which is only called when the component is connected to the document.
    /// </summary>
    abstract member hostConnected: unit -> unit
    /// <summary>
    /// Called when the host is disconnected from the component tree. For custom
    /// element hosts, this corresponds to the <c>disconnectedCallback()</c> lifecycle,
    /// which is called the host or an ancestor component is disconnected from the
    /// document.
    /// </summary>
    abstract member hostDisconnected: unit -> unit
    /// <summary>
    /// Called during the client-side host update, just before the host calls
    /// its own update.
    ///
    /// Code in <c>update()</c> can depend on the DOM as it is not called in
    /// server-side rendering.
    /// </summary>
    abstract member hostUpdate: unit -> unit
    /// <summary>
    /// Called after a host update, just before the host calls firstUpdated and
    /// updated. It is not called in server-side rendering.
    /// </summary>
    abstract member hostUpdated: unit -> unit

/// <summary>
/// An object that can host Reactive Controllers and call their lifecycle callbacks.
/// </summary>
/// <seealso href="https://lit.dev/docs/api/controllers/#ReactiveControllerHost"/>
[<AllowNullLiteral>]
[<Interface>]
type ReactiveControllerHost =
    /// <summary>
    /// Adds a controller to the host, which sets up the controller's lifecycle
    /// methods to be called with the host's lifecycle.
    /// </summary>
    abstract member addController: controller: ReactiveController -> unit
    /// <summary>
    /// Removes a controller from the host.
    /// </summary>
    abstract member removeController: controller: ReactiveController -> unit
    /// <summary>
    /// Requests a host update which is processed asynchronously. The update can
    /// be waited on via the <c>updateComplete</c> property.
    /// </summary>
    abstract member requestUpdate: unit -> unit
    /// <summary>
    /// Returns a Promise that resolves when the host has completed updating.
    /// The Promise value is a boolean that is <c>true</c> if the element completed the
    /// update without triggering another update. The Promise result is <c>false</c> if
    /// a property was set inside <c>updated()</c>. If the Promise is rejected, an
    /// exception was thrown during the update.
    /// </summary>
    /// <returns>
    /// A promise of a boolean that indicates if the update resolved
    /// without triggering another update.
    /// </returns>
    abstract member updateComplete: JS.Promise<bool> with get

/// <summary>
/// A reactive controller whose four callbacks do nothing until you override them, so a controller
/// overrides only the ones it needs. It implements <c>ReactiveController</c>.
/// </summary>
/// <remarks>
/// It doesn't register itself: call <c>host.addController this</c> in your constructor, after your fields.
/// <c>addController</c> on a host that's already connected calls <c>hostConnected</c> at once, and from the
/// base constructor that would run before your class had set its fields.
/// </remarks>
/// <example>
/// <code>
/// type Ticker(host: ReactiveControllerHost) as this =
///     inherit ReactiveControllerBase()
///     do host.addController this
///     override _.hostConnected() = ...
///     override _.hostDisconnected() = ...
/// </code>
/// </example>
[<AbstractClass>]
type ReactiveControllerBase() =
    /// <summary>Called when the host joins a document, in its <c>connectedCallback</c>. Does nothing unless overridden.</summary>
    abstract hostConnected: unit -> unit
    default _.hostConnected() = ()
    /// <summary>Called when the host leaves the document, in its <c>disconnectedCallback</c>. Does nothing unless overridden.</summary>
    abstract hostDisconnected: unit -> unit
    default _.hostDisconnected() = ()
    /// <summary>Called at each host update, before the host renders. Not called on the server. Does nothing unless overridden.</summary>
    abstract hostUpdate: unit -> unit
    default _.hostUpdate() = ()
    /// <summary>Called at each host update, after the host has rendered. Not called on the server. Does nothing unless overridden.</summary>
    abstract hostUpdated: unit -> unit
    default _.hostUpdated() = ()

    interface ReactiveController with
        member this.hostConnected() = this.hostConnected ()
        member this.hostDisconnected() = this.hostDisconnected ()
        member this.hostUpdate() = this.hostUpdate ()
        member this.hostUpdated() = this.hostUpdated ()
