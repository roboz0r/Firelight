---
title: Remember state across visits
tagline: save F# values in localStorage
description: "Keep a Firelight component's state across page loads: encode F# records as plain JSON in localStorage, decode them defensively, version the key, and restore after the first render so prerendering still matches."
section: cookbook
order: 60
summary: "Save F# values to `localStorage` and restore them on the next visit"
lead: "Build a packing list that keeps its items in `localStorage`, so they're still there when the page is reloaded."
links:
  - text: "MDN: Window.localStorage"
    href: https://developer.mozilla.org/en-US/docs/Web/API/Window/localStorage
---

Add an item and tick another, then reload the page. Start again puts the list back as it was.

::: example Snippets/PackingList.fs
<my-packing-list></my-packing-list>
:::

## How it works

`localStorage` holds strings, so the list goes in as JSON. `save` and `load` are plain functions,
outside the component.

- **Encode on purpose.** `save` maps each `Item` to an anonymous record, which [Fable](https://fable.io/)
  compiles to a plain object, and `JS.JSON.stringify` writes that. Stringifying the F# list would
  work too, but the stored names would be the record's field names, and renaming a field would
  quietly lose everyone's data.
- **Decode into F# values.** `JS.JSON.parse` returns arrays and plain objects, not F# lists and
  records. `unbox<Item list>` on its result compiles, then fails wherever list functions or record
  equality meet it, so `load` builds each `Item` itself.
- **Check what you read.** The stored text may be from an older version of the page, edited by
  hand, or written by another script on the same origin. `load` checks each field's type with `:?`,
  which Fable compiles to a `typeof` test. It skips an item that doesn't fit, and returns `None` if
  the text isn't a JSON array at all.
- **Version the key.** The key ends in `-v1`. When `Item` changes shape, change the key, and old
  data is ignored rather than misread.
- **Save on change.** Every change goes through `Change`, which assigns the new list and saves it.
- **Expect storage to fail.** `setItem` throws when storage is full or turned off, and then the list
  works for this visit only.
- **Restore after the first render.** The [prerendered](/guides/prerendering/) HTML can't know
  what's stored in your browser, so it shows the default list, and the first render in the browser
  must match it. `firstUpdated` waits for that update to finish, then loads. Setting a property
  during `firstUpdated` itself also works, but Lit's development build warns that an update was
  scheduled during one.

For a larger model, a decoding library such as [Thoth.Json](https://thoth-org.github.io/Thoth.Json/)
writes the encoders and decoders with you; the
[Kanban sample](https://github.com/roboz0r/Firelight/blob/main/sample/Kanban/Persistence.fs) stores
its whole board that way.

`localStorage` belongs to the origin, so every page on the site shares it, and other tabs see each
change as a `storage` event on `window`. Listen for it, in `connectedCallback`, if two tabs might
edit the list at once. Don't keep anything secret in it: any script on the origin can read it.

## Related

- [Dark and light themes](/cookbook/theme-toggle/), which stores a single string the same way.
- [Lifecycle](/guides/lifecycle/#reach-the-dom-after-it-renders), for `firstUpdated` and
  `updateComplete`.
