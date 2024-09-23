module Todo.Styling

open System
open Fable.Core
open Fable.Core.JsInterop
open Browser
open Browser.Types
open Lit
open type Lit.Lit

let private openSans =
    css
        $$"""
    @font-face {
        font-family: 'Open Sans';
        src: url(fonts/open-sans/OpenSans-VariableFont_wdth,wght.ttf) format('truetype');
    }
    """

let theme =
    css
        $$"""
    {{LitTailwind.cssBundle}}
    {{openSans}}
    :host {
        font-family: 'Open Sans', sans-serif;
    }
    """
