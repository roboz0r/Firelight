namespace Firelight.Router

open Fable.Core
open Fable.Core.JsInterop
open System
open Browser.Types.URLPattern

type Route<'Route> =
    {
        Path: URLPattern
        OfResults: URLPatternResult -> 'Route
    }

type Router<'Route> internal (routes: Route<'Route>[], notFound: 'Route) =
    member _.TryMatch(url: string) =
        routes
        |> Array.tryPick (fun route -> route.Path.exec (url) |> Option.map (fun result -> route.OfResults result))

    member this.Match(url: string) =
        this.TryMatch(url) |> Option.defaultValue notFound

module internal DomUtils =
    open Browser.Types
    open Fable.Core

    // Define a debug flag using F#'s conditional compilation.
    // In production builds, this evaluates to 'false', and JS minifiers
    // will completely strip out the console.warn branches!
    let isDebug =
#if DEBUG
        true
#else
        false
#endif

    /// Escapes a string for use in a CSS selector
    [<Emit("CSS.escape($0)")>]
    let escape (str: string) : string = nativeOnly

    [<Emit("""(function(node, sel, debug) {
        try {
            if (node.shadowRoot) {
                let res = node.shadowRoot.querySelector(sel);
                if (res) return res;
            }
            if (typeof node.querySelector === 'function') {
                return node.querySelector(sel);
            }
            return null;
        } catch (e) { 
            if (debug) console.warn("Router queryContext failed for selector: " + sel, e);
            return null; 
        }
    })($0, $1, $2)""")>]
    let private queryContextImpl (node: EventTarget) (selector: string) (debug: bool) : HTMLElement = nativeOnly

    [<Emit("""(function findDeep(sel, root, debug) {
        try {
            if (typeof root.querySelector !== 'function') return null;
            let el = root.querySelector(sel);
            if (el) return el;
            
            let children = root.querySelectorAll('*');
            for (let i = 0; i < children.length; i++) {
                if (children[i].shadowRoot) {
                    el = findDeep(sel, children[i].shadowRoot, debug);
                    if (el) return el;
                }
            }
            return null;
        } catch (e) { 
            if (debug) console.warn("Router querySelectorDeep failed for selector: " + sel, e);
            return null; 
        }
    })($0, document, $1)""")>]
    let private querySelectorDeepImpl (selector: string) (debug: bool) : HTMLElement = nativeOnly

    let queryContext (node: EventTarget) (selector: string) : HTMLElement = queryContextImpl node selector isDebug

    let querySelectorDeep (selector: string) : HTMLElement = querySelectorDeepImpl selector isDebug

module EventHandlers =
    open Browser
    open Browser.Types
    open Fable.Core

    type MouseEvent with
        [<Emit("$0.composedPath()")>]
        member this.composedPath() : EventTarget[] = nativeOnly

    let onPopState (router: Router<'Route>) (dispatch: 'Route -> unit) =
        fun (_: PopStateEvent) ->
            let url = window.location.href
            url |> router.Match |> dispatch

    let origin =
        let location = window.location
        let origin = location.origin

        if String.IsNullOrEmpty origin then
            location.protocol + "//" + location.host
        else
            origin

    let onClick (router: Router<'Route>) (dispatch: 'Route -> unit) (ev: MouseEvent) =
        if ev.defaultPrevented then
            ()
        else
            let isNotNavigationClick = ev.button <> 0 || ev.metaKey || ev.ctrlKey || ev.shiftKey

            if isNotNavigationClick then
                ()
            else
                let composedPath = ev.composedPath ()

                let anchor =
                    composedPath
                    |> Seq.tryPick (
                        function
                        | :? HTMLAnchorElement as a -> Some a
                        | _ -> None
                    )

                match anchor with
                | Some a ->
                    let isNotInternalLink =
                        not (String.IsNullOrEmpty a.target)
                        || a.hasAttribute "download"
                        || a.getAttribute "rel" = "external"
                        || a.origin <> origin
                        || String.IsNullOrEmpty a.href
                        || a.href.StartsWith "mailto:"
                        || a.href.StartsWith "javascript:"
                        || a.href.StartsWith "tel:"

                    if isNotInternalLink then
                        ()
                    else
                        let hrefRaw = a.attributes.[unbox "href"].value
                        let isHashLink = hrefRaw.StartsWith "#"

                        if isHashLink then
                            ev.preventDefault ()

                            // Fix CSS selector syntax errors (e.g. #1-intro -> #\31 -intro)
                            // We only escape the part AFTER the '#'.
                            let safeSelector =
                                if hrefRaw.Length > 1 then
                                    "#" + DomUtils.escape (hrefRaw.Substring(1))
                                else
                                    hrefRaw

                            let targetElement =
                                // STAGE 1: Walk the composed path (Fastest, handles localized components with relative links)
                                let pathResult =
                                    composedPath
                                    |> Seq.tryPick (fun node ->
                                        let res = DomUtils.queryContext node safeSelector
                                        if not (isNull res) then Some res else None
                                    )

                                match pathResult with
                                | Some el -> Some el
                                | None ->
                                    // STAGE 2: Global Light DOM (Standard behavior, handles IDs on Host elements)
                                    let globalResult =
                                        try
                                            document.querySelector (safeSelector)
                                        with e ->
                                            if DomUtils.isDebug then
                                                Browser.Dom.console.warn ("Global query failed", e)

                                            null

                                    if not (isNull globalResult) then
                                        Some(globalResult :?> HTMLElement)
                                    else
                                        // STAGE 3: Deep Pierce (The Failsafe for completely disconnected Shadow DOMs)
                                        let deepResult = DomUtils.querySelectorDeep safeSelector
                                        if not (isNull deepResult) then Some deepResult else None

                            // Execute scroll if found
                            match targetElement with
                            | Some el ->
                                let opts =
                                    jsOptions<ScrollIntoViewOptions> (fun opts ->
                                        opts.block <- ScrollAlignment.Start
                                        opts.behavior <- ScrollBehavior.Smooth
                                    )

                                el.scrollIntoView opts
                                // Update URL hash without triggering a 'popstate' route change
                                history.replaceState (null, "", hrefRaw)
                            | None ->
                                if DomUtils.isDebug then
                                    Browser.Dom.console.warn (
                                        $"Router: Could not find target for hash link '{hrefRaw}'"
                                    )
                        else
                            ev.preventDefault ()
                            let location = window.location
                            let href = a.href

                            if href = location.href then
                                ()
                            else
                                history.pushState (null, "", href)
                                href |> router.Match |> dispatch
                | None -> ()

[<AutoOpen>]
module RouterExtensions =
    open Browser

    let createRouterWithBaseUrl notFound (routes: (string * (URLPatternResult -> 'View)) seq) (baseurl: string) =

        let routes =
            routes
            |> Seq.map (fun (pattern, view) ->
                {
                    Path = URLPattern.Create(pattern, baseurl)
                    OfResults = view
                }
            )
            |> Array.ofSeq

        Router(routes, notFound)


    let createRouter notFound (routes: (string * (URLPatternResult -> 'View)) seq) =
        let baseurl =
            let location = window.location

            if String.IsNullOrEmpty location.origin then
                location.protocol + "//" + location.host
            else
                location.origin

        createRouterWithBaseUrl notFound routes baseurl

    type Router<'Route> with
        member this.OfLocation() =
            let url = window.location.href
            url |> this.Match
