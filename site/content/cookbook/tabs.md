---
title: Tabs
tagline: accessible tabs with arrow keys and cache
description: "Firelight tabs with the ARIA tab roles, arrow-key navigation with a roving tabindex, and Lit's cache directive to keep each panel's DOM while it's hidden."
section: cookbook
order: 100
summary: "Tabs with the ARIA roles, arrow keys, and panels that keep their state"
lead: "Build tabs that screen readers announce as tabs, that the arrow keys move between, and whose panels keep what was typed into them."
links:
  - text: "ARIA Authoring Practices: Tabs"
    href: https://www.w3.org/WAI/ARIA/apg/patterns/tabs/
---

Type a note in the Notes tab, switch to About, then back: the note is still there. With the focus on
a tab, the arrow keys, Home and End move between tabs.

::: example Snippets/SettingsTabs.fs
<my-settings-tabs></my-settings-tabs>
:::

## How it works

The roles do the announcing. `role="tablist"` holds `role="tab"` buttons, each with
`aria-selected`, and `role="tabpanel"` holds the selected tab's content, named by that tab through
`aria-labelledby`. A screen reader then says "General, tab, selected, 1 of 3".

- **One Tab stop.** Only the selected tab has `tabindex="0"`; the others have `-1`. Tab moves
  into the strip and on to the panel, and the arrow keys move along the strip. This is the
  keyboard behaviour the ARIA pattern describes.
- **Arrow keys.** One `@keydown` on the tablist handles them all. `OnKey` picks the next tab,
  selects it, and focuses it once the update has rendered, by awaiting `updateComplete`. It calls
  `preventDefault` only for keys it handles, so the page doesn't scroll on Home and End.
- **Keeping the panels.** `match` picks a different template for each tab. Lit would normally
  throw away the old panel's DOM and build the new one, losing what was typed. `cache` keeps the
  DOM of each template it has shown, and puts it back when that template returns. Each panel must
  be its own template for that, as `general`, `notes` and `about` are.

`cache` keeps the DOM, not your data. Anything that matters after the user leaves the page belongs
in state, as in [Remember state across visits](/cookbook/persist-state/).

Selecting a tab on arrow keys, rather than moving the focus and waiting for Enter, suits panels that
render at once. For panels that load data, the ARIA pattern suggests manual activation instead.

## Related

- [Templates: Directives](/guides/templates/#directives), for `cache` and the others.
- [Lifecycle: Reach the DOM after it renders](/guides/lifecycle/#reach-the-dom-after-it-renders),
  for focusing after an update.
