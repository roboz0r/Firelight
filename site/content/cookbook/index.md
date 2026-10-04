---
title: Cookbook
tagline: short recipes for common tasks
description: "Short Firelight recipes, each with a live demo and its code: forms, data loading, dialogs, tabs, theming, drag and drop, animation and testing."
section: cookbook
order: 0
lead: "Short recipes for common tasks, each with a live demo and its code. A recipe solves one problem and links to the guides for the ideas behind it."
---

## Forms and input

- [Form validation](/cookbook/form-validation/): check fields in F# on submit and show each error
  beside its field.
- [A custom form control](/cookbook/form-control/): a switch component that a page's `<form>`
  submits like a checkbox.
- [Search as you type](/cookbook/debounced-search/): search after a pause in typing, and drop
  requests for old text.

## Data

- [Fetch JSON](/cookbook/fetch-json/): load JSON with `fetch`, with loading, error and retry
  states.
- [Infinite scroll](/cookbook/load-more/): load the next page when the end of a list scrolls into
  view.
- [Remember state across visits](/cookbook/persist-state/): save F# values to `localStorage` and
  restore them on the next visit.

## Layout and components

- [Dark and light themes](/cookbook/theme-toggle/): system, light and dark themes from custom
  properties, remembered across visits.
- [A card with slots](/cookbook/slots/): a card that takes its heading, body and footer from the
  page.
- [A modal dialog](/cookbook/dialog/): ask a question with `<dialog>`, and read the answer when it
  closes.
- [Tabs](/cookbook/tabs/): tabs with the ARIA roles, arrow keys, and panels that keep their state.

## Behaviour

- [Toast notifications](/cookbook/toasts/): short messages that any code can raise, shown in one
  place.
- [Keyboard shortcuts](/cookbook/keyboard-shortcuts/): page-wide keys that skip fields, in a
  controller any component can use.
