[<AutoOpen>]
module Globals

open System
open Fable.Core
open Fable.Core.JsInterop

// JavaScript's global constructors, for `jsConstructor`, as Lit's `type` property option:
// ``type`` = jsConstructor<Globals.Array>. `open System` hides Boolean, String, Object and Array behind System's types,
// for which Fable says "Only declared types define a function constructor in JS"; write
// `Globals.Number` and so on to be sure of these.

/// JavaScript's <c>Boolean</c>, for <c>jsConstructor&lt;Globals.Boolean&gt;</c>.
[<Global>]
type Boolean = interface end

/// JavaScript's <c>Number</c>, for <c>jsConstructor&lt;Globals.Number&gt;</c>.
[<Global>]
type Number = interface end

/// JavaScript's <c>String</c>, for <c>jsConstructor&lt;Globals.String&gt;</c>.
[<Global>]
type String = interface end

/// JavaScript's <c>Object</c>, for <c>jsConstructor&lt;Globals.Object&gt;</c>: Lit parses the attribute as JSON.
[<Global>]
type Object = interface end

/// JavaScript's <c>Array</c>, for <c>jsConstructor&lt;Globals.Array&gt;</c>: Lit parses the attribute as JSON.
[<Global>]
type Array = interface end

[<Global>]
type TemplateStringsArray = interface end

/// <summary>
/// A unique identifier.
/// </summary>
/// <seealso href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Symbol" />
[<Global>]
type symbol =
    /// Expose the [[Description]] internal slot of a symbol directly.
    abstract description: string option

[<Erase>]
type JS =
    /// Returns a new, unique Symbol value.
    [<Global>]
    static member inline Symbol() : symbol = nativeOnly

    /// Returns a new, unique Symbol value.
    [<Global>]
    static member inline Symbol(description: string) : symbol = nativeOnly

    /// Returns a new, unique Symbol value.
    [<Global>]
    static member inline Symbol(description: float) : symbol = nativeOnly

[<Erase>]
module JS =
    [<Global>]
    type Symbol =
        /// <summary>
        /// Returns a Symbol object from the global symbol registry matching the given key if found.
        /// Otherwise, returns a new symbol with this key.
        /// </summary>
        /// <param name="key">key to search for.</param>
        [<CompiledName("for")>]
        static member inline for'(description: string) : symbol = nativeOnly

        /// <summary>
        /// Returns a key from the global symbol registry matching the given Symbol if found.
        /// Otherwise, returns a undefined.
        /// </summary>
        /// <param name="sym">Symbol to find the key for.</param>
        static member inline keyFor(sym: symbol) : string option = nativeOnly
