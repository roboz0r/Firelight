namespace Firelight.Context

open Fable.Core
open Firelight

[<AllowNullLiteral>]
type HTMLElementEventMap<'ValueType> =
    /// A 'context-request' event can be emitted by any element which desires
    /// a context value to be injected by an external provider.
    abstract ``context-request``: ContextEvent<Context<'ValueType>, 'ValueType> with get, set
    /// A 'context-provider' event can be emitted by any element which hosts
    /// a context provider to indicate it is available for use.
    abstract ``context-provider``: ContextProviderEvent<Context<'ValueType>, 'ValueType> with get, set

[<Erase>]
type LitContext =
    /// <summary>
    /// Creates a typed Context.
    ///
    /// Contexts are compared with strict equality.
    ///
    /// If you want two separate <c>createContext()</c> calls to referer to the same
    /// context, then use a key that will by equal under strict equality like a
    /// string for <c>Symbol.for()</c>:
    ///
    /// <code lang="fsharp">
    /// // true
    /// createContext(JS.Symbol.for("my-context")) === createContext(JS.Symbol.for("my-context"))
    /// </code>
    ///
    /// If you want a context to be unique so that it's guaranteed to not collide
    /// with other contexts, use a key that's unique under strict equality, like
    /// a <c>Symbol()</c> or object.:
    ///
    /// <code lang="fsharp">
    /// // false
    /// createContext(JS.Symbol("my-context")) === createContext(JS.Symbol("my-context"))
    /// </code>
    /// </summary>
    /// <param name="key">a context key value</param>
    /// <returns>the context key value cast to <c>Context&lt;K, ValueType&gt;</c></returns>
    [<Import("createContext", "@lit/context")>]
    static member createContext<'C, 'ValueType when 'C :> Context<'ValueType> and 'C :> symbol>(key: symbol) : 'C =
        nativeOnly

    /// <summary>
    /// Creates a typed Context.
    ///
    /// Contexts are compared with strict equality.
    ///
    /// If you want two separate <c>createContext()</c> calls to referer to the same
    /// context, then use a key that will by equal under strict equality like a
    /// string for <c>Symbol.for()</c>:
    ///
    /// <code lang="fsharp">
    /// // true
    /// createContext("my-context") === createContext("my-context")
    /// </code>
    ///
    /// If you want a context to be unique so that it's guaranteed to not collide
    /// with other contexts, use a key that's unique under strict equality, like
    /// a <c>Symbol()</c> or object.:
    ///
    /// <code lang="fsharp">
    /// // false
    /// createContext({||}) === createContext({||})
    /// </code>
    /// </summary>
    /// <param name="key">a context key value</param>
    /// <returns>the context key value cast to <c>Context&lt;K, ValueType&gt;</c></returns>
    [<Import("createContext", "@lit/context")>]
    static member createContextWithKey<'K, 'C, 'ValueType when 'C :> Context<'ValueType>>(key: symbol) : 'C = nativeOnly
