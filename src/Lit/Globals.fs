[<AutoOpen>]
module Globals

open System
open Fable.Core
open Fable.Core.JsInterop

/// Placeholder for the primitive `Boolean` type. Used with `jsConstructor` to specify the type of a property.
[<Global>]
type Boolean = interface end

/// Placeholder for the primitive `Number` type. Used with `jsConstructor` to specify the type of a property.
[<Global>]
type Number = interface end

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
