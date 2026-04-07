namespace Firelight.Router

open Firelight
open Browser
open Browser.Types

/// A ReactiveController that manages client-side routing for a LitElement host.
///
/// Tracks the current route by matching `window.location.href` against the
/// provided `Router`. Automatically attaches `popstate` and `click` event
/// listeners when the host connects and removes them on disconnect.
///
/// Typical usage:
///   let router = createRouter NotFound [ "/", fun _ -> Home; "/about", fun _ -> About ]
///   let routing = RouterController(this, router)
///   // In render: match routing.route with ...
type RouterController<'Route>(host: ReactiveControllerHost, router: Router<'Route>) as this =
    let mutable _route = router.OfLocation()
    let mutable _popStateHandler: (Event -> unit) option = None
    let mutable _clickHandler: (Event -> unit) option = None

    let dispatch (route: 'Route) =
        _route <- route
        host.requestUpdate ()

    do host.addController this

    /// The current matched route — always valid (initialised from the current URL on construction).
    member _.route = _route

    interface ReactiveController with
        member _.hostConnected() =
            _route <- router.OfLocation()

            let popHandler: Event -> unit =
                let handler = EventHandlers.onPopState router dispatch
                fun ev -> handler (unbox ev)

            let clickHandler: Event -> unit =
                let handler = EventHandlers.onClick router dispatch
                fun ev -> handler (unbox ev)

            _popStateHandler <- Some popHandler
            _clickHandler <- Some clickHandler
            window.addEventListener ("popstate", popHandler)
            document.addEventListener ("click", clickHandler)
            host.requestUpdate ()

        member _.hostDisconnected() =
            _popStateHandler
            |> Option.iter (fun h -> window.removeEventListener ("popstate", h))

            _clickHandler
            |> Option.iter (fun h -> document.removeEventListener ("click", h))

            _popStateHandler <- None
            _clickHandler <- None

        member _.hostUpdate() = ()
        member _.hostUpdated() = ()
