namespace GettingStarted

open Fable.Core
open Fable.Core.JsInterop
open Firelight
open type Firelight.Lit

[<AttachMembers>]
type MyPage() =
    inherit LitElement()

    // attribute = false means this property is never reflected to/from an HTML attribute.
    // It is only set programmatically, so the type hint in PropertyDeclaration is irrelevant.
    static member properties =
        PropertyDeclarations.create [ "article", PropertyDeclaration<obj>(attribute = !^false) ]

    member val article =
        {|
            title = "My Nifty Article"
            text = "Some witty text"
        |} with get, set

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
