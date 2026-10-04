module Snippets.PropertyTypes

open Fable.Core
open Fable.Core.JsInterop
open Firelight
open type Firelight.Lit

let private styles =
    css
        $$"""
    :host { display: flex; align-items: center; gap: 0.75rem; }
    .caption { min-width: 7rem; }
    button { font: inherit; padding: 0.2rem 0.6rem; }
    """

let private quantity (caption: string) (step: float) (total: float) (add: unit -> unit) =
    html
        $"""
    <span class="caption">{caption}</span>
    <button @click={fun _ -> add ()}>Add {step}</button>
    <span>Total: <output>{total}</output></span>"""

/// <my-quantity step="0.5"></my-quantity>
[<AttachMembers>]
type Quantity() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [
            "step", PropertyDeclaration<float>(``type`` = jsConstructor<Number>)
            "total", PropertyDeclaration<float>(state = true)
        ]

    static member styles = styles

    member val step = 1.0 with get, set
    member val total = 0.0 with get, set

    override this.render() =
        quantity "With type" this.step this.total (fun () -> this.total <- this.total + this.step)

/// The same component without `type`, so `step` stays the attribute's text.
[<AttachMembers>]
type UntypedQuantity() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [
            "step", PropertyDeclaration<float>()
            "total", PropertyDeclaration<float>(state = true)
        ]

    static member styles = styles

    member val step = 1.0 with get, set
    member val total = 0.0 with get, set

    override this.render() =
        quantity "Without type" this.step this.total (fun () -> this.total <- this.total + this.step)

defineElement<Quantity> "my-quantity"
defineElement<UntypedQuantity> "my-untyped-quantity"
