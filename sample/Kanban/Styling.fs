module Kanban.Styling

open Firelight
open type Firelight.Lit

let theme =
    css
        $$"""
    :host {
        display: block;
        font-family: system-ui, -apple-system, sans-serif;
    }
    """
