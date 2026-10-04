# Changelog

All packages are versioned together. Firelight is pre-1.0, so a minor version may break the API;
each break is listed under Changed with how to migrate.

## 0.3.0

### Added

- **New packages**, published for the first time:
  - `Firelight.Signals`: bindings for `@lit-labs/signals`, with `LitSignals.defineElement` and
    signal-aware `html` and `svg` tags.
  - `Firelight.Task`: bindings for `@lit/task` (`LitTask`).
  - `Firelight.Motion`: bindings for `@lit-labs/motion` (the `animate` directive, its keyframe
    presets, `AnimateController` and spring controllers).
  - `Firelight.Observers`: bindings for `@lit-labs/observers` (resize, intersection, mutation and
    performance controllers).
  - `Firelight.Virtualizer`: bindings for `@lit-labs/virtualizer`.
  - `Firelight.Templates`: `dotnet new firelight -n MyApp` creates a Vite app with one component.
- `LightDomElement`: a `LitElement` that renders into the host element instead of a shadow root.
- `ReactiveElement.dispatchEvent`, so a component can raise an event without a cast:
  `this.dispatchEvent (Event.customEvent ("changed", value))`.
- Lit's `when`, `choose`, `map` and `range` directives. `when` is an F# keyword, so it's `when'`:

  ```fsharp
  when' (loggedIn, (fun () -> html $"<p>Welcome</p>"), fun () -> html $"<p>Please log in</p>")
  choose (tab, [ "home", (fun () -> home ()); "about", (fun () -> about ()) ], fun () -> notFound ())
  html $"""<ul>{map (range 3, fun i -> html $"<li>{i}</li>")}</ul>"""
  ```

- The `Ev` module, for typed `@event` handlers. A hole is `obj`, so `fun e -> ...` needed its
  type annotated; `Ev.keyboard`, `Ev.mouse`, `Ev.pointer`, `Ev.focus`, `Ev.input`, `Ev.wheel`,
  `Ev.drag`, `Ev.touch`, `Ev.submit`, `Ev.event` and `Ev.custom<'T>` supply it and compile to the
  lambda alone. `Ev.value` and `Ev.checked'` pass your handler the field's `value` or `checked`:

  ```fsharp
  // 0.2
  html $"""<input @input={fun (e: Event) -> setName (e.target :?> HTMLInputElement).value}
      @keydown={fun (e: KeyboardEvent) -> if e.key = "Enter" then save ()}>"""
  // 0.3
  html $"""<input @input={Ev.value setName} @keydown={Ev.keyboard (fun e -> if e.key = "Enter" then save ())}>"""
  ```

  Nothing checks the function against the event's name.
- `until (promise)` and `until (promise, placeholder)` overloads.
- `StaticHTML.mathml`.

### Changed

Breaking changes, with how to migrate:

- **The packages need Fable.Core 5.0.0 or later** (0.2.0 needed 4.5.0).
- **In Debug builds, a format specifier in a hole throws.** A hole such as `{price:N2}` or
  `{name,10}` compiled, but Lit gets only the value, so the format was dropped without a word.
  Now `html`, `svg`, `mathml` and `css`, and the `StaticHTML` and `LitSignals` tags, throw in a
  Debug build (`dotnet fable watch`, as `npm run dev` runs it, or `-c Debug`), naming the hole.
  Release builds emit the same JavaScript as before. Format the value in F#:

  ```fsharp
  // 0.2: renders 3.14159
  html $"""<p>{price:N2}</p>"""
  // 0.3
  html $"""<p>{price.ToString "N2"}</p>"""
  ```

  In `css`, which takes only `css` values and numbers, pass the number, or wrap trusted text in
  `unsafeCSS`.
- **`live`, `keyed`, `guard`, `cache` and `join` take plain values.** They took `obj option`
  (or `TemplateResult option`, or `seq<'T> option`), so every call needed `Some(box ...)`.
  Now `live (value: 'T)`, `keyed (key: 'K, value: 'V)`, `guard (dependencies: obj[], valueFn: unit -> 'T)`,
  `cache (value: ChildRenderable)` and `join (items: seq<'T>, joiner: 'U)`. Remove the
  `Some(box ...)` and `Some(... :> TemplateResult)`:

  ```fsharp
  // 0.2
  html $"""<input .value={live (Some(box text))}>"""
  keyed (Some(box user.Id), Some(box (profile user)))
  guard ([| Some(box rows) |], fun () -> Some(box (table rows)))
  cache (Some(tab :> TemplateResult))
  join (Some items, html $"<br>")
  // 0.3
  html $"""<input .value={live text}>"""
  keyed (user.Id, profile user)
  guard ([| rows |], fun () -> table rows)
  cache tab
  join (items, html $"<br>")
  ```

  For non-null values Lit receives the same arguments. A `null` value used to reach Lit wrapped
  in a Fable `Some` object; Lit now gets the `null`.
- **`until` takes promises and values directly.** The `ParamArray` of
  `U2<Promise<ChildRenderable>, ChildRenderable>` is now `until (promise)`,
  `until (promise, placeholder)`, or `until (values: obj[])` for more than one promise:

  ```fsharp
  // 0.2
  until (U2.Case1 profile, U2.Case2(html $"<p>Loading…</p>" :> ChildRenderable))
  // 0.3
  until (profile, html $"<p>Loading…</p>")
  ```

- **`StaticHTML.html` returns `HTMLTemplateResult`** and `StaticHTML.svg` returns
  `SVGTemplateResult`, rather than `TemplateResult`. Lit's static tags call its core `html` and
  `svg`, so these are the same kind of value as `Lit.html`'s and `Lit.svg`'s. `StaticHTML.html`
  now fits helpers typed `HTMLTemplateResult` and the `renderable` builder. Code that annotated
  the result as `TemplateResult` still compiles; a function whose type must stay `TemplateResult`
  may need `:> TemplateResult`.

Other changes:

- **The type of `nothing` is a subtype of `HTMLTemplateResult`, `SVGTemplateResult` and
  `MathMLTemplateResult`.** A template-or-nothing needs no annotation when the template comes first:

  ```fsharp
  // 0.2
  let button: ChildRenderable = if canEmpty then html $"<button>Empty</button>" else nothing
  // 0.3
  let button = if canEmpty then html $"<button>Empty</button>" else nothing
  ```

  F# takes an `if` or `match` type from its first branch, so with `nothing` first, keep the
  `ChildRenderable` annotation or put the template first. Existing code compiles unchanged.
- `defineElement` (and `LitSignals.defineElement`) registers with the global `customElements`
  rather than `window.customElements`: the same object in a browser, and modules that define
  elements now also load under Lit SSR in Node, where there is no `window`.
- `Firelight.Router` only intercepts clicks on links that match one of its routes. Links it has
  no route for, such as other pages on the same site, navigate normally.

### Fixed

- The `unsafeHTML` and `unsafeStatic` imports named `"unsafeHTML "` and `"unsafeStatic "`, with a
  trailing space. Fable trims import names, so they worked; the JavaScript is unchanged.
- XML docs: `noChange` had `nothing`'s summary; `ChildRenderable` and the template result types
  had none.
