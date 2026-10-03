module Snippets.ThemeContext

open Fable.Core
open Fable.Core.JsInterop
open Firelight
open Firelight.Context
open type Firelight.Lit

type Theme =
    | Ember
    | Ocean

// A context is a symbol branded with the type of value it carries.
type ThemeContext =
    inherit Context<Theme>
    inherit symbol

let themeContext: ThemeContext = LitContext.createContext (JS.Symbol "theme")

/// Provides the current theme to everything inside it, however deeply nested.
[<AttachMembers>]
type ThemeProvider() =
    inherit LitElement()

    let mutable theme = Ember
    let provider = ContextProvider(jsThis, ContextProvider.Options(themeContext, theme))

    static member styles =
        css
            $$"""
        :host { display: grid; gap: 1rem; justify-items: start; }
        button { font: inherit; padding: 0.4rem 0.9rem; cursor: pointer; }
        """

    member this.Toggle() =
        theme <- if theme = Ember then Ocean else Ember
        provider.setValue theme
        this.requestUpdate ()

    override this.render() =
        html
            $"""
        <button @click={fun _ -> this.Toggle()}>Switch theme</button>
        <slot></slot>"""

/// Reads the theme from the nearest provider. Nothing in between passes it along.
[<AttachMembers>]
type ThemedBadge() =
    inherit LitElement()

    let theme =
        ContextConsumer(jsThis, ContextConsumer.Options(themeContext, subscribe = true))

    static member styles =
        css
            $$"""
        span { display: inline-block; padding: 0.25rem 0.75rem; border-radius: 1rem; color: white; }
        .Ember { background: #d9480f; }
        .Ocean { background: #1c7ed6; }
        """

    override _.render() =
        let name = string (theme.value |> Option.defaultValue Ember)
        html $"""<span class={name}>{name}</span>"""

defineElement<ThemeProvider> "my-theme-provider"
defineElement<ThemedBadge> "my-themed-badge"
