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
