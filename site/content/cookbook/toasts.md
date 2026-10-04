---
title: Toast notifications
tagline: short messages raised from anywhere
description: "Firelight toast notifications that any code can raise with an event on the document, shown by one toaster component in a live region, with timers cleared when it leaves the page."
section: cookbook
order: 110
summary: "Short messages that any code can raise, shown in one place"
lead: "Build toast notifications that any code on the page can raise, from F# or JavaScript, and that one component shows."
links:
  - text: "MDN: ARIA status role"
    href: https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA/Reference/Roles/status_role
---

Press Save, a separate component, or the page's own button, which runs plain JavaScript. Each
toast goes after five seconds, or when you dismiss it.

::: example Snippets/Toasts.fs .stacked
<div class="toast-sources">
  <my-save-button></my-save-button>
  <button id="page-toast">Raise one from JavaScript</button>
</div>
<my-toaster></my-toaster>
<style>
  .toast-sources { display: flex; gap: 0.75rem; }
  #page-toast { font: inherit; padding: 0.3rem 0.9rem; }
</style>
:::

<script type="module">
  document.getElementById("page-toast").addEventListener("click", () =>
    document.dispatchEvent(new CustomEvent("toast", { detail: "Hello from plain JavaScript." })));
</script>

## How it works

The toaster and the code that raises a toast don't know about each other. `showToast` dispatches a
`toast` event on `document`, with the message as its `detail`, and the toaster listens there. Any
F# code can call `showToast`, and any script can dispatch the same event, as the page does.

- **Listening.** The toaster adds its listener in `connectedCallback` and removes it in
  `disconnectedCallback`, keeping the very function it added, as
  [Events: Listen outside the component](/guides/events/#listen-outside-the-component) explains.
- **Timers.** Each toast has a `setTimeout` to remove it, kept in a map by the toast's id.
  Dismissing a toast clears its timer, and leaving the page clears them all, so nothing runs on a
  removed component. While the focus is on a toast, a timer that runs out starts again instead, so
  a toast never disappears from under the keyboard.
- **The focus.** Removing the element that has the focus sends the focus to the page's start. So
  Dismiss moves it to the next toast's button, or, after the last one, back to where it was before
  it moved onto a toast, which `@focusin` noted from the event's `relatedTarget`.
- **Announcing.** The toasts render inside one `role="status"` element, which is on the page from
  the start. Screen readers announce what's added to it without moving the focus; a live region
  added along with its message is often not announced at all. A status region announces all of
  its content by default, so `aria-atomic="false"` limits that to the new toast.
- **Keyed.** [`repeat`](/guides/templates/#keyed-lists-with-repeat) keys each toast by its id, so
  dismissing one doesn't move the others' messages between elements.

In this demo the toaster sits in the flow. In an app, put one `<my-toaster>` at the end of
`<body>`, and fix it to a corner from the page's CSS: `my-toaster { position: fixed; inset: auto
1rem 1rem auto; }`.

Five seconds suits a confirmation such as "Saved". A message that someone must read or act on,
such as an error, shouldn't disappear on a timer: show it where it happened, or keep it until it's
dismissed.

An event suits messages anyone can raise. When the toasts are state that several components read,
such as a count in the header, keep them in a [signal](/packages/signals/) instead.

## Related

- [Component communication](/guides/communication/), for events and signals between components.
