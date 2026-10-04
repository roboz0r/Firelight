---
title: Keyboard shortcuts
tagline: page-wide keys in a reusable controller
description: "Page-wide keyboard shortcuts in Firelight: a reactive controller that listens on the document, ignores keys typed into fields, including fields inside shadow roots, and removes its listener when the component leaves."
section: cookbook
order: 120
summary: "Page-wide keys that skip fields, in a controller any component can use"
lead: "Build a keyboard shortcut controller: one line in a component adds a key that works anywhere on the page, except while the user is typing."
links:
  - text: "MDN: aria-keyshortcuts"
    href: https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA/Reference/Attributes/aria-keyshortcuts
---

Click anywhere outside the box on this page, then press <kbd>/</kbd> to jump to the search box,
or <kbd>?</kbd> to show the shortcuts. Type `?` into the search box: it's typed, not taken as a
shortcut. Untick Single-key shortcuts to turn them off.

::: example Snippets/Shortcuts.fs
<my-shortcut-demo></my-shortcut-demo>
:::

## How it works

`KeyboardShortcut` is a [controller](/guides/controllers/). A component creates one per key in its
constructor, with the function to run, and the controller does the rest: it adds a `keydown`
listener to `document` when the component joins the page, and removes it when the component leaves.
The listener is a local function, so `removeEventListener` gets the same function
`addEventListener` did.

- **Skip typing.** A shortcut mustn't fire while the user types into a field. The field may be
  inside a shadow root, and a listener on `document` sees `e.target` retargeted to the outermost
  host, such as `<my-shortcut-demo>`, not the `<input>`. `e.composedPath ()` starts with the
  element where the key was actually pressed, so the controller checks that one.
- **Plain keys only.** It ignores keys pressed with Ctrl, ⌘ or Alt, which belong to the browser
  and the operating system, and the repeats a held key sends, so holding <kbd>?</kbd> doesn't
  flicker the help.
- **Say so.** `aria-keyshortcuts="/"` tells screen readers the search box has a shortcut, and the
  page shows the keys too. Shortcuts nobody can discover don't help anyone.

Single-character shortcuts get in the way of speech input and of keyboard users who press keys by
accident, so WCAG asks for a way to turn them off or remap them, unless they only work while their
component has the focus. The controller's `Enabled` is that switch, and the demo's checkbox sets
it on both shortcuts. In an app, keep the setting with the user's other preferences.

## Related

- [Events: Listen outside the component](/guides/events/#listen-outside-the-component), for
  listeners on `window` and `document` and their cleanup.
- [Events: How far an event goes](/guides/events/#how-far-an-event-goes), for retargeting and
  `composedPath`.
