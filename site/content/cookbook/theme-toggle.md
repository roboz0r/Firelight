---
title: Dark and light themes
tagline: a theme switch with CSS custom properties
description: "A Firelight theme picker: System, Light and Dark, with colours from CSS custom properties and light-dark(), the choice kept in localStorage, and a head script so a stored theme doesn't flash."
section: cookbook
order: 70
summary: "System, light and dark themes from custom properties, remembered across visits"
lead: "Build a theme picker with System, Light and Dark choices, that keeps the choice for the next visit."
links:
  - text: "MDN: light-dark()"
    href: https://developer.mozilla.org/en-US/docs/Web/CSS/color_value/light-dark
  - text: "MDN: color-scheme"
    href: https://developer.mozilla.org/en-US/docs/Web/CSS/color-scheme
---

Pick Dark, then reload the page: the box stays dark. System follows your device's setting.

::: example Snippets/ThemePicker.fs
<my-theme-picker></my-theme-picker>
:::

## How it works

The colours are CSS custom properties, each with a light and a dark value:
`--paper: light-dark(#fffdf9, #17151a)`. `light-dark()` picks one by the element's `color-scheme`.
`color-scheme: light dark` means "whichever the device prefers", and `light` or `dark` forces one.
So a theme is one property: the component's `render` adds the class `light` or `dark`, or neither,
and every colour follows. `color-scheme` also gives form controls and scroll bars the matching
look. The [Styling guide](/guides/styling/#theming-with-custom-properties) covers custom
properties across shadow DOM.

The choice is stored as a string. Anything other than `light` or `dark` reads as System, so a
stale or edited value can't break the page. [Remember state across visits](/cookbook/persist-state/)
covers storing more than a string.

## Theming a whole page

This demo themes a box, since the page around it has its own theme. For an app, put the palette on
`:root` in the page's stylesheet, and have the picker set `color-scheme` on the `<html>` element:

```fsharp
open Browser

let applyTheme (theme: string) =
    document.documentElement.style.setProperty ("color-scheme", if theme = "system" then "" else theme)

    try
        localStorage.setItem ("theme", theme)
    with _ ->
        ()
```

Custom properties inherit into every shadow root, so each component reads the palette with
`var(--paper)`.

A component's code runs after the page has painted, so a stored dark theme would first show light,
then switch. Set it before the first paint with a small script in the page's `<head>`, reading the
same key:

```html
<script>
  try {
    const theme = localStorage.getItem("theme");
    if (theme === "light" || theme === "dark") document.documentElement.style.colorScheme = theme;
  } catch {}
</script>
```

An inline script needs a hash or a nonce under a Content Security Policy that forbids inline
scripts. Prerendering can't help here: the build doesn't know which theme a visitor chose.

This site's theme button, in the header, works the same way, but in plain JavaScript in that head
script rather than as a component: a Firelight component in the header would load Lit on every
page, and pages without demos load no JavaScript but that script. It stores its choice under its
own key, so this demo's choice doesn't change the site's theme.

## Related

- [Styling](/guides/styling/), for custom properties, `:host` and parts.
- [Firelight.Context](/packages/context/), to pass a theme value to components that render
  differently by theme, not only colour.
