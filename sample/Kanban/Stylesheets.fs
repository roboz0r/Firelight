module Kanban.Stylesheets

open Browser
open Browser.Types
open Firelight
open type Firelight.Lit

/// Hrefs of same-origin stylesheets found in document.head at module load time.
/// Capturing them once handles both dev (static path) and production (Vite-hashed filename).
let private hrefs =
    let origin = window.location.origin
    let links = document.head.querySelectorAll "link[rel='stylesheet']"

    [|
        for i in 0 .. links.length - 1 do
            match links.item i with
            | :? HTMLLinkElement as link when link.href.StartsWith(origin) -> link.href.Substring(origin.Length)
            | _ -> ()
    |]

/// Renders <link> elements for each discovered stylesheet into the component's shadow DOM.
/// Call at the top of render() in any component that uses Tailwind classes.
let stylesheetLinks () : HTMLTemplateResult =
    let links =
        hrefs |> Array.map (fun href -> html $"<link rel=\"stylesheet\" href={href}>")

    html $"{links}"
