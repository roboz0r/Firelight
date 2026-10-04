---
title: A custom form control
tagline: a component that a page's form submits
description: "Make a Firelight component a form-associated custom element: a switch that a page's <form> submits like a checkbox, labelled by <label for>, reset with the form, with ElementInternals bound from F#."
section: cookbook
order: 20
summary: "A switch component that a page's `<form>` submits like a checkbox"
lead: "Build an on/off switch that a `<form>` in the page submits like a checkbox, and that a `<label>` names, using the browser's form-associated custom elements."
links:
  - text: "MDN: ElementInternals"
    href: https://developer.mozilla.org/en-US/docs/Web/API/ElementInternals
  - text: "web.dev: More capable form controls"
    href: https://web.dev/articles/more-capable-form-controls
---

The form below is plain HTML in the page. Turn the switch on with a click or with Tab and Space,
then press Submit to see what the form would send. Reset puts the switch back.

::: example Snippets/FormSwitch.fs .stacked
<form id="switch-form" class="switch-form">
  <label for="switch-updates">Email me product updates</label>
  <my-form-switch id="switch-updates" name="updates"></my-form-switch>
  <button>Submit</button>
  <button type="reset">Reset</button>
</form>
<p id="switch-result" role="status"></p>
<style>
  .switch-form { display: flex; flex-wrap: wrap; align-items: center; gap: 0.75rem; }
  .switch-form button { font: inherit; padding: 0.3rem 0.9rem; }
</style>
:::

<script type="module">
  const form = document.getElementById("switch-form");
  const result = document.getElementById("switch-result");
  form.addEventListener("submit", (e) => {
    e.preventDefault();
    const sent = [...new FormData(form)].map(([name, value]) => `${name}=${value}`);
    result.textContent = sent.length ? `The form would send ${sent.join("&")}.` : "The form would send nothing.";
  });
  form.addEventListener("reset", () => (result.textContent = ""));
</script>

## How it works

- `static member formAssociated = true` tells the browser the element takes part in forms. With
  `[<AttachMembers>]` it becomes a static property of the JavaScript class, where the browser looks
  for it when the element is defined.
- `attachInternals` gives the element its `ElementInternals`, the object it reports to the form
  through. Neither is in [Fable](https://fable.io/)'s browser bindings, so the module declares the three members it
  uses and an `Emit` for the call, as [Using JavaScript libraries](/guides/js-libraries/#typed-bindings)
  describes.
- `setFormValue` sets what the form submits under the element's `name`, and `null` leaves it out,
  as for an unticked checkbox. `Report` calls it, and `Toggle` calls `Report` before it raises
  `change`, so a listener that reads the form sees the new value. `updated` calls it after every
  other change too: a reset, or code setting `checked`.
- The element itself is the control, with no `<button>` inside. `internals.role` makes it a
  switch to assistive technology and `ariaChecked` reports its state. Form-associated elements are
  labelable, so `<label for>` names it, as it would an `<input>`, and clicking the label clicks it.
- A `tabindex`, added in `connectedCallback`, makes it focusable. It listens for its own `click`
  and `keydown`, added in the constructor: a template's `@click` only reaches elements inside the
  template, not the host.
- The browser calls `formResetCallback` when the form is reset. It goes back to the `checked`
  attribute, which stays the default, as on a native checkbox.

The awkward part is the host. To F#, `LitElement` isn't an `HTMLElement`, so calling the host's own
DOM methods, such as `attachInternals`, `addEventListener` or `tabIndex`, needs
`unbox<HTMLElement> this` first.

The role is set in the constructor, so the [prerendered](/guides/prerendering/) HTML has it too:
Lit SSR copies the ARIA properties set on `ElementInternals` there into attributes.

## What this leaves out

A complete control also handles `disabled` (`formDisabledCallback`), reports invalid values with
`setValidity`, and restores its state when the browser restores the form
(`formStateRestoreCallback`). MDN's [ElementInternals](https://developer.mozilla.org/en-US/docs/Web/API/ElementInternals)
reference covers each.

## Related

- [Form validation](/cookbook/form-validation/), for a form and its fields in one component.
- [Events: Raise an event](/guides/events/#raise-an-event). The switch raises a plain `change`
  event, as a checkbox does.
