namespace MultiPage

open Browser.Types.URLPattern

type Page =
    | Home
    | About
    | User of id: string
    | NotFound

module MultiPageModel =
    let matchRoute (result: URLPatternResult) : Page =
        let groups = result.pathname.groups

        match groups.["page"] with
        | Some "about" -> About
        | _ -> NotFound

    let matchUser (result: URLPatternResult) : Page =
        match result.pathname.groups.["id"] with
        | Some id -> User id
        | None -> NotFound

    /// Route table shared by the app and its tests. The root needs its own
    /// entry because "/:page?" does not match "/" in URLPattern.
    let routes: (string * (URLPatternResult -> Page)) list =
        [ "/", (fun _ -> Home); "/:page", matchRoute; "/users/:id", matchUser ]

    let pageTitle =
        function
        | Home -> "Home"
        | About -> "About"
        | User id -> $"User {id}"
        | NotFound -> "Not Found"
