module Snippets.SlotCard

open Fable.Core
open Browser.Types
open Firelight
open type Firelight.Lit

/// The part of <slot> this module uses. Fable's browser bindings don't have it.
type HTMLSlotElement =
    inherit HTMLElement
    abstract assignedElements: unit -> Element[]

/// A card with a heading, a body and an optional footer, all supplied by the page.
[<AttachMembers>]
type SlotCard() =
    inherit LitElement()

    let footer = createRef<HTMLSlotElement> ()

    static member properties =
        PropertyDeclarations.create [ "hasFooter", PropertyDeclaration<bool>(state = true) ]

    static member styles =
        css
            $$"""
        :host { display: block; align-self: start; width: 16rem; border: 1px solid var(--border); border-radius: 0.5rem;
                background: var(--bg); }
        header, .body, footer { padding: 0.75rem 1rem; }
        header { border-bottom: 1px solid var(--border); font-weight: 600; }
        ::slotted([slot="heading"]) { margin: 0; font-size: 1.1rem; }
        footer { display: flex; gap: 0.5rem; justify-content: end; border-top: 1px solid var(--border); }
        footer[hidden] { display: none; }
        ::slotted(button) { font: inherit; padding: 0.3rem 0.9rem; }
        """

    member val hasFooter = false with get, set

    member this.CheckFooter() =
        footer.value
        |> Option.iter (fun slot -> this.hasFooter <- slot.assignedElements().Length > 0)

    // slotchange reports changes. Check once the first update is done, too: in a prerendered
    // card, the page's elements were in their slots before this code ran.
    override this.firstUpdated _ =
        this.updateComplete.``then`` (fun _ -> this.CheckFooter()) |> ignore

    override this.render() =
        html
            $"""
        <header><slot name="heading">Untitled</slot></header>
        <div class="body"><slot></slot></div>
        <footer ?hidden={not this.hasFooter}>
            <slot name="footer" {ref footer} @slotchange={fun _ -> this.CheckFooter()}></slot>
        </footer>"""

defineElement<SlotCard> "my-slot-card"
