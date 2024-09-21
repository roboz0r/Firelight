namespace GettingStarted

open Fable.Core
open Fable.Core.JsInterop
open Lit
open type Lit

type MyPageProps =
    abstract article: obj with get, set

[<AttachMembers>]
type MyPage() =
    inherit LitElement()

    static member properties: MyPageProps =
        PropertyDeclarations.create [ "article", (PropertyDeclaration<string>(attribute = !^ false)) ]

    member val article =
        {| title = "My Nifty Article"
           text = "Some witty text" |} with get, set

    member this.headerTemplate() =
        html $"""<header>{this.article.title}</header>"""

    member this.articleTemplate() =
        html $"""<article>{this.article.text}</article>"""

    member this.footerTemplate() =
        html $"""<footer>Your footer here.</footer>"""

    override this.render() =
        html
            $"""
            {this.headerTemplate ()}
            {this.articleTemplate ()}
            {this.footerTemplate ()}"""
