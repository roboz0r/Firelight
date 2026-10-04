---
title: A card with slots
tagline: components that take content from the page
description: "A Firelight card component that takes its heading, body and footer from the page through named slots, with fallback content, ::slotted styles, and a footer that hides itself when it's empty."
section: cookbook
order: 80
summary: "A card that takes its heading, body and footer from the page"
lead: "Build a card that takes its heading, body and footer from the page through named slots, and hides its footer when there's nothing in it."
links:
  - text: "Lit docs: Slots"
    href: https://lit.dev/docs/components/shadow-dom/#slots
---

The first card fills all three slots. The second gives only a body: its heading falls back to
"Untitled", and its footer is hidden.

::: example Snippets/SlotCard.fs
<my-slot-card>
  <h3 slot="heading">Weekly report</h3>
  <p>Sales are up 4% on last week.</p>
  <button slot="footer">Open</button>
</my-slot-card>
<my-slot-card>
  <p>No heading and no footer.</p>
</my-slot-card>
:::

## How it works

A `<slot>` in a component's template shows the elements the page put inside the component's tag.
A child with `slot="heading"` goes to `<slot name="heading">`, and the rest go to the slot with no
name. The children stay in the page's DOM: the page's styles and listeners apply to them, and the
page picks the elements, such as which heading level fits where the card sits.

- **Fallback content.** What's inside a `<slot>` shows when nothing is assigned to it, as
  "Untitled" does.
- **Styling slotted elements.** `::slotted([slot="heading"])` and `::slotted(button)` style the
  page's elements from inside the component. They reach only the top-level children, and the
  page's own rules win.
- **An empty footer.** A slot can't hide the element around it, so the component checks the slot
  with `assignedElements` and sets `hasFooter`. `slotchange` reports changes, but in a
  prerendered card the page's elements were in their slots before the component's code ran, and
  no event reports them. So `firstUpdated` also checks once, after the first update. `?hidden`
  hides the footer, border and all, while it's empty.

`CheckFooter` takes the slot as an `HTMLSlotElement`, which Firelight binds because
[Fable](https://fable.io/)'s browser bindings don't. `Ev.slot` hands it the `<slot>` that
`@slotchange` is bound on, and `this.query<HTMLSlotElement> "slot[name=footer]"` finds the same
slot in `firstUpdated`.

The cards are [prerendered](/guides/prerendering/), and the page's elements are already in the
page, so the first card shows its heading and body before any JavaScript runs. Its footer appears
once `firstUpdated` has looked, because `render` at build time can't see which slots the page
fills.

## Related

- [Styling: Component styles](/guides/styling/#component-styles), for `::slotted` and `:host`.
- [From Lit: @queryAssignedElements](/from-lit/decorators/#queryassignedelements), for counting
  slotted elements.
