[<AutoOpen>]
module Utils

open Lit.Context

type LitContext<'State> =
    inherit Context<'State>
    inherit symbol