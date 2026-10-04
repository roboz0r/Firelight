module MultiPage.Tests.RouterTests

open Fable.Pyxpecto
open Browser.Types.URLPattern
open Firelight.Router
open MultiPage

// URLPattern is a JavaScript API with no .NET implementation, so the router's matching can only
// run under Fable: `npm test` runs these tests in Node. Under .NET they are pending.
#if FABLE_COMPILER
// Node 22 has no URLPattern. At a module's top level this waits for the polyfill, so it must come
// before the router is created: ES modules run the modules they import first, so Main.fs is too late.
importPolyfill ()

// An explicit base URL rather than createRouter's window.location, which Node doesn't have.
let private router =
    createRouterWithBaseUrl NotFound MultiPageModel.routes "http://localhost"

let matchTests =
    testList "Router.Match" [
        testCase "root path matches Home"
        <| fun () ->
            let result = router.TryMatch "http://localhost/"
            Expect.isSome result "Should match root"
            Expect.equal result.Value Home "Should be Home"

        testCase "about path matches About"
        <| fun () ->
            let result = router.TryMatch "http://localhost/about"
            Expect.isSome result "Should match about"
            Expect.equal result.Value About "Should be About"

        testCase "user path matches User with id"
        <| fun () ->
            let result = router.TryMatch "http://localhost/users/42"
            Expect.isSome result "Should match user"
            Expect.equal result.Value (User "42") "Should be User 42"

        testCase "user path with string id"
        <| fun () ->
            let result = router.TryMatch "http://localhost/users/alice"
            Expect.isSome result "Should match user"
            Expect.equal result.Value (User "alice") "Should be User alice"

        testCase "unknown path falls through to notFound via Match"
        <| fun () ->
            let result = router.Match "http://localhost/unknown/deep/path"
            Expect.equal result NotFound "Should be NotFound"

        testCase "a path no route matches gives None from TryMatch"
        <| fun () ->
            let result = router.TryMatch "http://localhost/users/42/posts"
            Expect.isNone result "Should not match"
    ]
#else
let matchTests =
    ptestList "Router.Match (URLPattern is JavaScript only: run `npm test`)" [ ptestCase "matches routes" ignore ]
#endif

let pageTitleTests =
    testList "MultiPageModel.pageTitle" [
        testCase "Home title"
        <| fun () -> Expect.equal (MultiPageModel.pageTitle Home) "Home" "Home"

        testCase "About title"
        <| fun () -> Expect.equal (MultiPageModel.pageTitle About) "About" "About"

        testCase "User title"
        <| fun () -> Expect.equal (MultiPageModel.pageTitle (User "7")) "User 7" "User 7"

        testCase "NotFound title"
        <| fun () -> Expect.equal (MultiPageModel.pageTitle NotFound) "Not Found" "Not Found"
    ]

let all = testList "MultiPage" [ matchTests; pageTitleTests ]
