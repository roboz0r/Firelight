module Site.Routing.RouteExplorer

open Fable.Core
open Fable.Core.JsInterop
open Browser
open Browser.Types.URLPattern
open Firelight
open Firelight.Router
open type Firelight.Lit

importPolyfill ()

// Every page the router knows about, as a type. Matching a URL produces one of these.
type Route =
    | Home
    | User of id: int
    | UserPost of userId: int * slug: string
    | NotFound

/// Vite's `base`: "/Firelight/" on GitHub Pages.
let basePath: string = emitJsExpr () "import.meta.env.BASE_URL"
let root = basePath + "client-side-routing/"

let private group (name: string) (result: URLPatternResult) =
    result.pathname.groups.[name] |> Option.defaultValue ""

// (\d+) only matches digits, so "users/abc" falls through to the catch-all.
// The catch-all keeps every address under root inside the app; links to anywhere else
// (other pages of the site) match no route, so the browser navigates to them normally.
let router =
    [
        root + "{index.html}?", (fun _ -> Home)
        root + "users/:id(\\d+)", (fun r -> User(int (group "id" r)))
        root + "users/:id(\\d+)/posts/:slug", (fun r -> UserPost(int (group "id" r), group "slug" r))
        root + "*", (fun _ -> NotFound)
    ]
    |> createRouter NotFound

let private examples =
    [
        ""
        "users/42"
        "users/42/posts/hello-lit"
        "users/7/posts/routing-in-fsharp"
        "users/abc"
    ]

let private exampleLink (path: string) =
    html $"""<li><a href={root + path}>/{path}</a></li>"""

[<AttachMembers>]
type RouteExplorer() as this =
    inherit LightDomElement()

    let routing = RouterController(this, router)

    override _.render() =
        let description =
            match routing.route with
            | Home -> "the list of examples"
            | User id -> $"the profile of user {id}"
            | UserPost(userId, slug) -> $"post \"{slug}\" by user {userId}"
            | NotFound -> "a not-found page"

        // Navigate goes to an address from code, as a click on a link to it would.
        let nextUser =
            match routing.route with
            | User id
            | UserPost(id, _) -> id + 1
            | Home
            | NotFound -> 1

        let goToNextUser _ =
            routing.Navigate(root + $"users/{nextUser}")

        html
            $"""
        <ul class="route-links">{examples |> List.map exampleLink}</ul>
        <p><button type="button" @click={goToNextUser}>Go to user {nextUser}</button></p>
        <dl class="route-result">
            <dt>Address</dt><dd><code>{window.location.pathname}</code></dd>
            <dt>Route value</dt><dd><code>{sprintf "%A" routing.route}</code></dd>
            <dt>Renders</dt><dd>{description}</dd>
        </dl>"""

defineElement<RouteExplorer> "fl-route-explorer"
