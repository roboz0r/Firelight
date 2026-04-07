namespace Browser.Types.URLPattern

open Fable.Core

// https://developer.mozilla.org/en-US/docs/Web/API/URLPattern
// https://urlpattern.spec.whatwg.org/

[<AllowNullLiteral>]
type URLPatternGroups =
    [<EmitIndexer>]
    abstract Item: key: string -> string option

[<AllowNullLiteral>]
type URLPatternComponentResult =
    /// The input string that was matched against the `URLPattern`.
    abstract input: string
    /// <summary>
    /// The matched groups for the `URLPattern`.
    /// </summary>
    /// <remarks>
    /// If there is a match, this will contain the matched groups for each part of the URL.
    /// The keys of the object will be the names given to the capturing groups in the `URLPattern`, or a numeric index, starting at 0 if no name was given.
    /// For example, if the `URLPattern` is `/users/:id`, the matched groups will be `{ id: '123' }` for the input `/users/123`.
    /// If there is no match, this will be an empty object.
    /// </remarks>
    abstract groups: URLPatternGroups


/// An object with an inputs key containing the array of arguments passed into the function,
/// and keys for each of the URL parts containing the matched input, and matched groups for that part.
[<AllowNullLiteral>]
type URLPatternResult =
    /// Arguments passed to the exec function of URLPattern.
    abstract inputs: obj[]
    /// Pattern to match hash part of a URL.
    abstract hash: URLPatternComponentResult
    /// Pattern to match the hostname part of a URL.
    abstract hostname: URLPatternComponentResult
    /// Pattern to match the password part of a URL.
    abstract password: URLPatternComponentResult
    /// Pattern to match the pathname part of a URL.
    abstract pathname: URLPatternComponentResult
    /// Pattern to match the port part of a URL.
    abstract port: URLPatternComponentResult
    /// Pattern to match the protocol part of a URL.
    abstract protocol: URLPatternComponentResult
    /// Pattern to match the search part of a URL.
    abstract search: URLPatternComponentResult
    /// Pattern to match the username part of a URL.
    abstract username: URLPatternComponentResult

[<AllowNullLiteral; Global>]
type URLPatternInit
    [<ParamObject; Emit("$0")>]
    (
        ?protocol: string,
        ?username: string,
        ?password: string,
        ?hostname: string,
        ?port: string,
        ?pathname: string,
        ?search: string,
        ?hash: string,
        ?baseURL: string
    ) =
    /// Pattern to match the protocol part of a URL.
    member val protocol: string option = nativeOnly with get, set
    /// Pattern to match the username part of a URL.
    member val username: string option = nativeOnly with get, set
    /// Pattern to match the password part of a URL.
    member val password: string option = nativeOnly with get, set
    /// Pattern to match the hostname part of a URL.
    member val hostname: string option = nativeOnly with get, set
    /// Pattern to match the port part of a URL.
    member val port: string option = nativeOnly with get, set
    /// Pattern to match the pathname part of a URL.
    member val pathname: string option = nativeOnly with get, set
    /// Pattern to match the search part of a URL.
    member val search: string option = nativeOnly with get, set
    /// Pattern to match hash part of a URL.
    member val hash: string option = nativeOnly with get, set
    /// <summary>
    /// The base URL to use in cases where input is a relative pattern.
    /// </summary>
    /// <remarks>
    /// If the baseURL property is provided it will be parsed as a URL and used
    /// to populate any other properties that are missing. If the baseURL property
    /// is missing, then any other missing properties default to the pattern `*` wildcard, accepting any input.
    /// </remarks>
    member val baseURL: string option = nativeOnly with get, set

[<AllowNullLiteral>]
type URLPattern =
    /// Pattern to match hash part of a URL.
    abstract hash: string
    /// Indicates whether any of the URLPattern components contain regular expression capturing groups.
    abstract hasRegExpGroups: bool
    /// Pattern to match the hostname part of a URL.
    abstract hostname: string
    /// Pattern to match the password part of a URL.
    abstract password: string
    /// Pattern to match the pathname part of a URL.
    abstract pathname: string
    /// Pattern to match the port part of a URL.
    abstract port: string
    /// Pattern to match the protocol part of a URL.
    abstract protocol: string
    /// Pattern to match the search part of a URL.
    abstract search: string
    /// Pattern to match the username part of a URL.
    abstract username: string

    /// Returns an object with the matched parts of the URL or null if the URL does not match.
    abstract exec: input: string * ?baseURL: string -> URLPatternResult option

    /// Returns an object with the matched parts of the URL or null if the URL does not match.
    abstract exec: input: URLPatternInit -> URLPatternResult option

    /// Returns true if the URL matches the given pattern, false otherwise.
    abstract test: input: string * ?baseURL: string -> bool

    /// Returns true if the URL matches the given pattern, false otherwise.
    abstract test: input: URLPatternInit -> bool

[<AllowNullLiteral; Global>]
type URLPatternOptions [<ParamObject; Emit("$0")>] (?ignoreCase: bool) =
    /// Enables case-insensitive matching if set to true. If omitted or set to false, matching will be case-sensitive.
    member val ignoreCase: bool option = nativeOnly with get, set

[<AllowNullLiteral>]
type URLPatternType =
    [<EmitConstructor>]
    abstract Create: pattern: string * ?options: URLPatternOptions -> URLPattern

    [<EmitConstructor>]
    abstract Create: pattern: string * baseUrl: string * ?options: URLPatternOptions -> URLPattern

    [<EmitConstructor>]
    abstract Create: input: URLPatternInit * ?options: URLPatternOptions -> URLPattern

[<AutoOpen>]
module URLPattern =
    /// Imports the `urlpattern-polyfill` polyfill if `URLPattern` is not natively supported.
    /// This is necessary for environments that do not support `URLPattern` natively.
    /// The polyfill should be included in the project dependencies.
    [<Emit("if (!globalThis.URLPattern) {  await import(\"urlpattern-polyfill\"); }")>]
    let inline importPolyfill () = nativeOnly

    [<Global>]
    let URLPattern: URLPatternType = nativeOnly
