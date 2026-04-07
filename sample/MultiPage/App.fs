module App

open Fable.Core
open Browser
open Firelight
open Firelight.Router
open type Firelight.Lit
open MultiPage

[<AttachMembers>]
type MultiPageApp() as this =
    inherit LitElement()

    let router =
        [
            "/", (fun _ -> Home)
            "/:page?", MultiPageModel.matchRoute
            "/users/:id", MultiPageModel.matchUser
        ]
        |> createRouter NotFound

    let routing = RouterController(this, router)

    static member styles =
        [|
            css
                $$"""
        :host {
            display: block;
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
            color: #0f172a;
        }
        nav {
            display: flex;
            gap: 1rem;
            padding: 1rem 1.5rem;
            border-bottom: 2px solid #0f172a;
        }
        nav a {
            color: #0f172a;
            text-decoration: none;
            font-weight: 600;
            font-size: 0.875rem;
            letter-spacing: 0.02em;
        }
        nav a:hover { text-decoration: underline; }
        .page {
            max-width: 680px;
            margin: 0 auto;
            padding: 2rem 1.5rem;
        }
        h1 { font-size: 1.5rem; font-weight: 800; letter-spacing: -0.03em; margin: 0 0 1rem; }
        p { color: #475569; line-height: 1.6; }
        .user-id { font-family: monospace; background: #f1f5f9; padding: 0.125rem 0.375rem; border-radius: 0.25rem; }
        """
        |]

    override _.render() =
        let page =
            match routing.route with
            | Home ->
                html
                    $"""
                <h1>Home</h1>
                <p>Welcome to the Firelight Router demo. Use the navigation above to explore pages.</p>
                <p>Try visiting <a href="/users/42">User 42</a> or <a href="/users/abc">User abc</a>.</p>"""
            | About ->
                html
                    $"""
                <h1>About</h1>
                <p>This demo shows client-side routing with the <code>RouterController</code>,
                   which integrates URL pattern matching into Lit's reactive lifecycle.</p>"""
            | User id ->
                html
                    $"""
                <h1>User Profile</h1>
                <p>Viewing user with id: <span class="user-id">{id}</span></p>
                <p><a href="/">← Back to Home</a></p>"""
            | NotFound ->
                html
                    $"""
                <h1>Not Found</h1>
                <p>The page you are looking for does not exist.</p>
                <p><a href="/">← Back to Home</a></p>"""

        html
            $"""
        <nav>
            <a href="/">Home</a>
            <a href="/about">About</a>
            <a href="/users/1">User 1</a>
            <a href="/not-found">Not Found</a>
        </nav>
        <div class="page">{page}</div>"""

let start () =
    defineElement<MultiPageApp> "multi-page-app"

    let el = document.getElementById "app"

    if el <> null then
        render (html $"<multi-page-app></multi-page-app>", el) |> ignore

do start ()
