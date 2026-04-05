module Todo.Styling

open Firelight
open type Firelight.Lit

let theme =
    css
        $$"""
    @font-face {
        font-family: 'Open Sans';
        src: url(fonts/open-sans/OpenSans-VariableFont_wdth,wght.ttf) format('truetype');
    }
    :host {
        display: block;
        font-family: 'Open Sans', sans-serif;
    }
    """
