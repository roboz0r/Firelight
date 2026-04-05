[<AutoOpen>]
module Utils

open Firelight.Context

type LitContext<'State> =
    inherit Context<'State>
    inherit symbol
