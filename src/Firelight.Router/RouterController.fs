namespace Firelight.Router

open Firelight
open Browser
open Browser.Types

/// <summary>
/// A ReactiveController that manages client-side routing for a LitElement host.
/// </summary>
/// <remarks>
/// <para>
/// Tracks the current route by matching <c>window.location.href</c> against the
/// provided <c>Router</c>. Attaches <c>popstate</c> and <c>click</c> event
/// listeners when the host connects and removes them on disconnect. <c>Navigate</c> changes the
/// route from code.
/// </para>
/// <para>
/// <c>onRouteChange</c>, when given, is called with the route after each change: a link click, back or
/// forward, <c>Navigate</c>, and when the host connects. Use it to turn route changes into Elmish messages.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// let router = createRouter NotFound [ "/", fun _ -> Home; "/about", fun _ -> About ]
/// let routing = RouterController(this, router)
/// // In render: match routing.route with ...
/// // In a handler: routing.Navigate "/about"
/// </code>
/// </example>
type RouterController<'Route>(host: ReactiveControllerHost, router: Router<'Route>, ?onRouteChange: 'Route -> unit) as this
    =
    inherit ReactiveControllerBase()

    let mutable _route = router.OfLocation()
    let mutable _stopListening = ignore

    let dispatch (route: 'Route) =
        _route <- route
        onRouteChange |> Option.iter (fun f -> f route)
        host.requestUpdate ()

    do host.addController this

    /// The current matched route — always valid (initialised from the current URL on construction).
    member _.route = _route

    /// <summary>
    /// Goes to <c>url</c> from code, as a click on a link to it would: when one of the router's routes
    /// matches, it adds an entry to the history (or, with <c>replace = true</c>, replaces the current one) and
    /// the host renders the new route, without loading a page. Otherwise the browser loads <c>url</c>.
    /// </summary>
    /// <remarks>
    /// <c>url</c> may be relative to the current address. Going to the current address does nothing.
    /// </remarks>
    /// <param name="url">Where to go, such as <c>"/users/42"</c>.</param>
    /// <param name="replace">Replace the current history entry, so Back skips it. Default false.</param>
    member _.Navigate(url: string, ?replace: bool) =
        EventHandlers.navigate router dispatch (defaultArg replace false) (EventHandlers.resolve url)

    override _.hostConnected() =
        dispatch (router.OfLocation())

        let stopPopState =
            Ev.listen window "popstate" (EventHandlers.onPopState router dispatch)

        let stopClicks = Ev.listen document "click" (EventHandlers.onClick router dispatch)

        _stopListening <-
            fun () ->
                stopPopState ()
                stopClicks ()

    override _.hostDisconnected() =
        _stopListening ()
        _stopListening <- ignore
