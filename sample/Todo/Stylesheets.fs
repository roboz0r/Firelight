module Todo.Stylesheets

open Fable.Core
open Browser

// The app's Tailwind CSS, imported as a string at build time and adopted by each component's
// shadow root, so the styles are there on first render. (The <link> in index.html styles the
// document itself, but doesn't reach inside shadow roots.) See docs/styling.md.
[<ImportDefault("./App.css?inline")>]
let private appCss: string = jsNative

let private makeSheet (cssText: string) =
    let sheet = CSSStyleSheet.Create()
    sheet.replaceSync cssText
    sheet

let allComponentStyles = [| makeSheet appCss |]
