---
title: A modal dialog
tagline: confirm with the native dialog element
description: "A Firelight confirmation dialog built on the native <dialog> element: showModal through a ref, a form with method=dialog for the answer, Escape to cancel, and focus handled by the browser."
section: cookbook
order: 90
summary: "Ask a question with `<dialog>`, and read the answer when it closes"
lead: "Build a confirmation dialog on the browser's `<dialog>` element, and read which button closed it."
links:
  - text: "MDN: The dialog element"
    href: https://developer.mozilla.org/en-US/docs/Web/HTML/Element/dialog
---

Press Delete report.pdf…, then answer with a button, or press Escape to cancel. Focus goes to
Keep it when the dialog opens, and back to the button that opened it when it closes.

::: example Snippets/ConfirmDialog.fs
<my-confirm-delete></my-confirm-delete>
:::

## How it works

The browser does most of the work. `showModal ()` opens the dialog above everything else, makes the
rest of the page inert, so Tab stays inside the dialog, and closes it on Escape. When it closes, the
focus returns to the element that had it before.

- **Opening.** A template can't call a method, so a [`ref`](/guides/templates/#directives) reaches
  the `<dialog>`, and `Ask` calls `showModal` on it. [Fable](https://fable.io/)'s browser bindings type it as
  `HTMLDialogElement`, with `showModal`, `close` and `returnValue`.
- **The answer.** A `<form method="dialog">` doesn't submit anywhere: pressing one of its buttons
  closes the dialog and sets its `returnValue` to that button's `value`. The `@close` handler reads
  it.
- **Cancelling.** Escape closes the dialog without touching `returnValue`, so it would still hold
  the last answer. `Ask` clears it before opening, so Escape reads as `""`.
- **First focus.** `autofocus` marks Keep it, the safe choice, as the element to focus when the
  dialog opens. Without it, the browser focuses the first focusable element, which changes when
  the content does.
- **A name.** `aria-labelledby` points at the dialog's heading, which screen readers announce as
  the dialog opens.

The dialog lives in the component's shadow root, so the component's styles reach it, including
`::backdrop`. A closed `<dialog>` isn't displayed, so the [prerendered](/guides/prerendering/) HTML
shows only the button.

To close the dialog when the backdrop is clicked too, add `closedby="any"` to it. Browsers that don't
support the attribute ignore it, and Escape and the buttons still work.

## Related

- [Events](/guides/events/), for `@close` and the events a component raises.
- [Using web component libraries](/guides/component-libraries/), whose dialogs, such as
  `<wa-dialog>`, add animation and styling on top of the same idea.
