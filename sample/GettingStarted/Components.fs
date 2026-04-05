namespace GettingStarted

open Firelight
open type Firelight.Lit

// Reusable component helpers.
// These return HTMLTemplateResult fragments for use inside a parent LitElement's render().
// Include Components.styles in your component's `static member styles` to apply the companion stylesheet.

module Components =

    // Companion stylesheet — include in any LitElement that uses these helpers.
    let styles =
        css
            $$"""
        /* ---- Input ---- */
        .input {
            display: flex;
            height: 2.5rem;
            width: 100%;
            border-radius: 0.5rem;
            border: 1px solid #d4d4d8;
            background: #fff;
            padding: 0.5rem 0.75rem;
            font-size: 0.875rem;
            color: #18181b;
            box-sizing: border-box;
            font-family: inherit;
            transition: box-shadow 0.15s;
        }
        .input::placeholder { color: #a1a1aa; }
        .input:focus-visible {
            outline: none;
            box-shadow: 0 0 0 2px #fff, 0 0 0 4px #18181b;
        }
        .input:disabled { cursor: not-allowed; opacity: 0.5; }

        /* ---- Button base ---- */
        .btn {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            white-space: nowrap;
            border-radius: 0.5rem;
            font-size: 0.875rem;
            font-weight: 500;
            font-family: inherit;
            cursor: pointer;
            transition: background-color 0.15s, border-color 0.15s, color 0.15s;
        }
        .btn:focus-visible {
            outline: none;
            box-shadow: 0 0 0 2px #fff, 0 0 0 4px #18181b;
        }
        .btn:disabled { pointer-events: none; opacity: 0.5; }

        /* ---- Button variants ---- */
        .btn-default     { background: #18181b; color: #fff;     border: 1px solid #18181b; }
        .btn-default:hover  { background: #3f3f46; border-color: #3f3f46; }
        .btn-default:active { background: #27272a; }

        .btn-destructive { background: #e11d48; color: #fff;     border: 1px solid #e11d48; }
        .btn-destructive:hover  { background: #f43f5e; border-color: #f43f5e; }
        .btn-destructive:active { background: #be123c; }

        .btn-outline     { background: transparent; color: #18181b; border: 1px solid #d4d4d8; }
        .btn-outline:hover  { background: #f4f4f5; }
        .btn-outline:active { background: #e4e4e7; }

        .btn-secondary   { background: #f4f4f5; color: #18181b; border: 1px solid #f4f4f5; }
        .btn-secondary:hover  { background: #e4e4e7; }
        .btn-secondary:active { background: #d4d4d8; }

        .btn-ghost       { background: transparent; color: #52525b; border: 1px solid transparent; }
        .btn-ghost:hover  { background: #f4f4f5; }
        .btn-ghost:active { background: #e4e4e7; }

        .btn-link        { background: transparent; color: #18181b; border: 1px solid transparent;
                           text-decoration: underline; text-underline-offset: 4px;
                           padding: 0; height: auto; }
        .btn-link:hover { opacity: 0.8; }

        /* ---- Button sizes ---- */
        .btn-md   { height: 2.5rem;  padding: 0.5rem 1rem; }
        .btn-sm   { height: 2rem;    padding: 0.25rem 0.75rem; font-size: 0.75rem; }
        .btn-lg   { height: 3rem;    padding: 0.75rem 2rem;    font-size: 1rem; }
        .btn-icon { height: 2.5rem;  width: 2.5rem; padding: 0; }
        """

    module Input =

        /// Renders an <input> element styled with the companion .input class.
        /// extraClasses — additional CSS classes (space-separated).
        /// inputType    — HTML input type (e.g. "text", "email", "password").
        /// props        — additional Lit attribute/property bindings as a template fragment.
        let input (extraClasses: string) (inputType: string) props =
            html $"""<input class="input {extraClasses}" type="{inputType}" {props} />"""

    module Button =

        type Variant =
            | Default
            | Destructive
            | Outline
            | Secondary
            | Ghost
            | Link

        module Variant =
            let toClass =
                function
                | Default -> "btn-default"
                | Destructive -> "btn-destructive"
                | Outline -> "btn-outline"
                | Secondary -> "btn-secondary"
                | Ghost -> "btn-ghost"
                | Link -> "btn-link"

        type Size =
            | Default
            | Small
            | Large
            | Icon

        module Size =
            let toClass =
                function
                | Default -> "btn-md"
                | Small -> "btn-sm"
                | Large -> "btn-lg"
                | Icon -> "btn-icon"

        /// Renders a <button> element.
        /// extraClasses — additional CSS classes (space-separated).
        /// variant      — visual style.
        /// size         — sizing preset.
        /// content      — inner content as a template fragment.
        /// props        — additional Lit attribute/property bindings as a template fragment.
        let button (extraClasses: string) (variant: Variant) (size: Size) content props =
            html
                $"""<button class="btn {Variant.toClass variant} {Size.toClass size} {extraClasses}" {props}>{content}</button>"""
