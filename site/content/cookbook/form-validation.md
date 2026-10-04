---
title: Form validation
tagline: check a form in F# and show its errors
description: "A sign-up form in Firelight that checks its fields in plain F# on submit, shows each error beside its field with aria-describedby, and moves focus to the first one."
section: cookbook
order: 10
summary: "Check fields in F# on submit and show each error beside its field"
lead: "Build a sign-up form that checks its fields when it's submitted, shows each error beside its field, and moves focus to the first field that needs fixing."
links:
  - text: "MDN: Client-side form validation"
    href: https://developer.mozilla.org/en-US/docs/Learn_web_development/Extensions/Forms/Form_validation
---

Press Sign up with both fields empty. Then type a name: its error clears as you type.

::: example Snippets/SignupForm.fs
<my-signup-form></my-signup-form>
:::

## How it works

- `validate` is plain F#: a record in, a list of field ids and messages out. It doesn't touch the
  DOM, so a unit test can call it, and an F# server can run the same checks.
- `Ev.submit` types the handler's event, and `e.preventDefault ()` stops the browser's own
  submission, which would reload the page.
- `novalidate` turns off the browser's validation bubbles, so every message comes from `validate`.
  `type="email"` and `autocomplete` still do their other jobs: the right keyboard on a phone, and
  autofill.
- Errors appear only after the first submit, so nobody is told off for a field they haven't
  reached yet. From then on, `render` checks the form again on every keystroke, so each error
  clears as soon as it's fixed.
- `aria-describedby` points each field at its message, so a screen reader reads the message with
  the field, and `aria-invalid` marks the field as wrong. `ifDefined` leaves both attributes out
  while the field is fine.
- After a failed submit, the handler waits for `updateComplete`, then focuses the first field with
  an error. Before that render, the error messages aren't on the page yet.
- The thanks message goes in a `role="status"` paragraph that is always there, so screen readers
  announce it when its text changes. Its hole holds `nothing` until then, which keeps the
  [prerendered](/guides/prerendering/) HTML and the first render in the browser the same.

## Keep the form and its fields together

A `<form>` only submits the controls in its own DOM tree. A form in the page doesn't see inputs
inside a component's shadow root, and a form inside a component doesn't see inputs in the page.
Keep the form and its fields in one template, as this component does. To build a control of your
own that a page's form submits, see [A custom form control](/cookbook/form-control/).

## Related

- [Templates: Typed event handlers](/guides/templates/#typed-event-handlers) for `Ev.submit` and
  `Ev.value`, and [Directives](/guides/templates/#directives) for `ifDefined`.
- [Lifecycle: Reach the DOM after it renders](/guides/lifecycle/#reach-the-dom-after-it-renders)
  for `updateComplete`.
