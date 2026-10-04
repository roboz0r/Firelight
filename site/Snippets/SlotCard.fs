module Snippets.SlotCard

open Fable.Core
open Firelight
open type Firelight.Lit

/// A card with a heading, a body and an optional footer, all supplied by the page.
[<AttachMembers>]
type SlotCard() =
    inherit LitElement()

    static member properties =
        PropertyDeclarations.create [ "hasFooter", PropertyDeclaration<bool>(state = true) ]

    static member styles =
        css
            $$"""
        :host { display: block; align-self: start; width: 16rem; border: 1px solid var(--border);
                border-radius: var(--radius); background: var(--bg); }
        header, .body, footer { padding: 0.75rem 1rem; }
        /* The heading looks the same from the slot as from the fallback: the slotted one inherits. */
        header { border-bottom: 1px solid var(--border); font-size: 1.1rem; font-weight: 600; }
        ::slotted([slot="heading"]) { margin: 0; font: inherit; }
        /* The body's elements lose their own margins; the grid's gap spaces them instead. */
        .body { display: grid; gap: 0.5rem; }
        .body ::slotted(*) { margin: 0; }
        footer { display: flex; gap: 0.5rem; justify-content: end; border-top: 1px solid var(--border); }
        footer[hidden] { display: none; }
        ::slotted(button) { font: inherit; color: var(--fg); background: var(--surface); padding: 0.3rem 0.9rem;
                            border: 1px solid var(--border); border-radius: var(--radius); cursor: pointer; }
        ::slotted(button:hover) { border-color: var(--muted); }
        ::slotted(button:focus-visible) { outline: 2px solid var(--accent); outline-offset: 2px; }
        """

    member val hasFooter = false with get, set

    member this.CheckFooter(slot: HTMLSlotElement) =
        this.hasFooter <- slot.assignedElements().Length > 0

    // slotchange reports changes, but a prerendered card's elements were in their slots before
    // this code ran, so check once as well. After updateComplete, not in firstUpdated itself:
    // setting hasFooter during an update schedules another, which Lit warns about.
    override this.firstUpdated _ =
        promise {
            let! _ = this.updateComplete
            this.query<HTMLSlotElement> "slot[name=footer]" |> Option.iter this.CheckFooter
        }
        |> Promise.start

    override this.render() =
        html
            $"""
        <header><slot name="heading">Untitled</slot></header>
        <div class="body"><slot></slot></div>
        <footer ?hidden={not this.hasFooter}>
            <slot name="footer" @slotchange={Ev.slot this.CheckFooter}></slot>
        </footer>"""

defineElement<SlotCard> "my-slot-card"
