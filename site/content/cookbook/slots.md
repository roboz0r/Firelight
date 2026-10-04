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

::: example Snippets/SlotCard.fs .spaced
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
- **Styling slotted elements.** `::slotted(...)` styles the page's elements from inside the
  component. The slotted heading takes `font: inherit`, so it looks the same as the "Untitled"
  fallback, which the header styles; `.body ::slotted(*)` removes the body elements' margins,
  and the body's grid gap spaces them instead; and `::slotted(button)` gives the footer's button
  a border and a background. These rules reach only the top-level children, and the page's own
  rules win.
- **The page's colours.** Custom properties such as `--border` and `--surface` inherit into the
  shadow root, so the card and its button follow the page's light and dark themes.
- **An empty footer.** A slot can't hide the element around it, so the component checks the slot
  with `assignedElements` and sets `hasFooter`. `?hidden` hides the footer, border and all,
  while it's empty.

`Ev.slot` hands `CheckFooter` the `<slot>` that `@slotchange` is bound on, as an
`HTMLSlotElement`, so `assignedElements` needs no cast. `slotchange` reports changes, but in a
prerendered card the page's elements were in their slots before the component's code ran, and no
event reports them. So `firstUpdated` checks once as well, finding the same slot with
`this.query<HTMLSlotElement> "slot[name=footer]"`. It waits for `updateComplete` in a
`promise { }` first: setting `hasFooter` during the first update would schedule a second one
straight away, which Lit warns about.

The cards are [prerendered](/guides/prerendering/), and the page's elements are already in the
page, so the first card shows its heading and body before any JavaScript runs. Its footer appears
only once `firstUpdated` has looked, because `render` at build time can't see which slots the page
fills, so the card grows and moves what's below it. CSS can't make that check yet: the
`:has-slotted` pseudo-class would hide an empty footer without script, but only Firefox supports
it. If most of your cards have a footer, start `hasFooter` as `true`: the usual card then doesn't
move, and a card without a footer shrinks instead.

## Related

- [Styling: Component styles](/guides/styling/#component-styles), for `::slotted` and `:host`.
- [From Lit: @queryAssignedElements](/from-lit/decorators/#queryassignedelements), for counting
  slotted elements.
