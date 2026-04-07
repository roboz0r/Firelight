module MultiPage.Tests.RouterTests

open Fable.Pyxpecto
open Browser.Types.URLPattern
open Firelight.Router
open MultiPage

let private router =
    createRouter NotFound [ "/:page?", MultiPageModel.matchRoute; "/users/:id", MultiPageModel.matchUser ]

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
    ]

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
