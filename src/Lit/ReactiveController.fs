namespace Lit
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
