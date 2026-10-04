# Changelog

All packages are versioned together. Firelight is pre-1.0, so a minor version may break the API;
each break is listed under Changed with how to migrate.

## 0.3.0

### Added

- **New packages**, published for the first time:
  - `Firelight.Signals`: bindings for `@lit-labs/signals`, with `LitSignals.defineElement` and
    signal-aware `html` and `svg` tags.
  - `Firelight.Task`: bindings for `@lit/task` (`LitTask`). It depends on Fable.Fetch (2.7.0 or
    later): the task function's `signal` is Fable.Fetch's `AbortSignal`, so it passes straight to
    Fable.Fetch's `fetch`: `fetch url [ Signal options.signal ]`.
  - `Firelight.Motion`: bindings for `@lit-labs/motion` (the `animate` directive, its keyframe
    presets, `AnimateController` and spring controllers).
  - `Firelight.Observers`: bindings for `@lit-labs/observers` (resize, intersection, mutation and
    performance controllers).
  - `Firelight.Virtualizer`: bindings for `@lit-labs/virtualizer`.
  - `Firelight.Templates`: `dotnet new firelight -n MyApp` creates a Vite app with one component.
- `LightDomElement`: a `LitElement` that renders into the host element instead of a shadow root.
- `ReactiveElement.dispatchEvent`, so a component can raise an event without a cast, and
  `this.dispatch`, the same without its `bool` result, so without `|> ignore`:

  ```fsharp
  // 0.2
  (box this :?> HTMLElement).dispatchEvent (Event.customEvent ("changed", value)) |> ignore
  // 0.3
  this.dispatch (Event.customEvent ("changed", value))
  ```

  Keep `dispatchEvent` for a cancelable event, whose result you need.
- `Event.customEvent` takes `cancelable` (default `false`), so an event a listener can cancel no
  longer needs `CustomEvent.Create` and `jsOptions`:
  `this.dispatchEvent (Event.customEvent ("note-closing", id, cancelable = true))`.
- `Ev.listen target name handler` adds an event listener and returns the function that removes it,
  for listeners on `window` or `document` added in `connectedCallback`. Removing one yourself with a
  method or a class `let` function removed nothing, as Fable passes a new function each time:

  ```fsharp
  // 0.2
  let onKey (e: Event) = this.key <- (e :?> KeyboardEvent).key
  window.addEventListener ("keydown", onKey)
  stopListening <- fun () -> window.removeEventListener ("keydown", onKey)
  // 0.3
  stopListening <- Ev.listen window "keydown" (Ev.keyboard (fun e -> this.key <- e.key))
  ```

- `Ev.valueAs<'T>`, for an element whose `value` property isn't a string, such as a slider
  component's number: it unboxes `currentTarget.value`, and parses nothing.
- `HTMLSlotElement`, which Fable.Browser.Dom doesn't have: `name`, `assignedNodes`,
  `assignedElements` (both with an optional `flatten`) and `assign`, which takes elements and text
  nodes, as the DOM does; plus `assignedSlot` on elements and text nodes. It's in the `Firelight` namespace, so it doesn't clash with
  `open Browser.Types`. `Ev.slot` passes a `@slotchange` handler the slot it's bound on, and
  `this.query<HTMLSlotElement>` finds one by selector. Slot code no longer needs a dynamic call or
  a type of its own:

  ```fsharp
  // 0.2
  @slotchange={fun (e: Event) -> this.count <- (e.target?assignedElements () : Element[]).Length}
  // 0.3
  @slotchange={Ev.slot (fun slot -> this.count <- slot.assignedElements().Length)}
  ```

  `slot.assignedElements (flatten = true)` compiles to `slot.assignedElements({ flatten: true })`.
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
- `AttributeConverter<'T>(fromAttribute = ..., toAttribute = ...)` for a property's `converter`
  option, for attribute text Lit's default conversion can't read. It replaces the empty
  `AttributeConverter` placeholder:

  ```fsharp
  // 0.2: no members, so
  converter = unbox (createObj [ "fromAttribute", box parse ])
  // 0.3
  converter = AttributeConverter<string list>(fromAttribute = parse, toAttribute = format)
  ```

  `fromAttribute` gets a `string option` (`None` once the attribute is removed); `toAttribute`
  returns one (`None` removes the attribute).
- `Globals.String`, `Globals.Object` and `Globals.Array`, beside `Boolean` and `Number`, for
  ``` ``type`` = jsConstructor<Globals.Array> ```. `open System` hides `Boolean`, `String`,
  `Object` and `Array` behind System's types, so qualify them with `Globals.`.
- `ReactiveControllerBase`, a base class for controllers whose four callbacks do nothing until
  overridden, so a controller overrides only what it needs. The `ReactiveController` interface
  stays, for classes that inherit something else:

  ```fsharp
  // 0.2
  type Clock(host: ReactiveControllerHost) as this =
      do host.addController this
      interface ReactiveController with
          member _.hostConnected() = start ()
          member _.hostDisconnected() = stop ()
          member _.hostUpdate() = ()
          member _.hostUpdated() = ()
  // 0.3
  type Clock(host: ReactiveControllerHost) as this =
      inherit ReactiveControllerBase()
      do host.addController this
      override _.hostConnected() = start ()
      override _.hostDisconnected() = stop ()
  ```

- In Debug builds, `css` throws when a hole holds anything but a `css` value or a number, naming
  the hole: `css: the hole after ":host { color: " holds the string "red"`. Lit throws for such a
  value in every build, but its message doesn't say which hole.
- `isServer` (Lit's), `true` under Lit SSR in Node and `false` in a browser.
- `this.element`, the component as the `HTMLElement` it is in the browser, for the host's own DOM
  members. `LitElement` can't inherit Fable's `HTMLElement` interface, so these needed a cast:

  ```fsharp
  // 0.2
  (unbox<HTMLElement> this).addEventListener ("click", onClick)
  // 0.3
  this.element.addEventListener ("click", onClick)
  ```

- `this.attachInternals ()`, returning a typed `ElementInternals` (`setFormValue`, `setValidity`
  with `ValidityStateFlags`, `validity`, `checkValidity`, `role` and the common ARIA properties),
  for form-associated components (`static member formAssociated = true`).
- Firelight.Observers: `IntersectionController.target ()`, like `ResizeController.target ()`:
  `<div {visible.target ()}>` observes the element a render puts there. `@lit-labs/observers` has
  no such directive for it; this one is Lit's `ref` with one callback per controller.
- `repeat (items, keyFn, template)` also takes functions of the item alone, so a method fits:
  `repeat (people, (fun p -> p.Id), this.PersonView)`.
- `ChildDirectiveResult`, a `DirectiveResult` that is also a `ChildRenderable`. The directives Lit
  accepts as an element's content return it: `repeat`, `keyed`, `cache`, `guard`, `until`,
  `asyncAppend`, `asyncReplace`, `unsafeHTML`, `unsafeSVG`, `templateContent` and Firelight.Signals'
  `watch`. So `render` can return one directly:

  ```fsharp
  // 0.2
  | Some p -> html $"{keyed (p.Id, profile p)}"
  // 0.3
  | Some p -> keyed (p.Id, profile p)
  ```

  `ref`, `classMap`, `styleMap`, `live` and Motion's `animate` still return a plain
  `DirectiveResult`, as does `virtualize`, which needs an element around it to virtualize.
- `this.query<'E> selector` and `this.queryAll<'E> selector`, as Lit's `@query` and `@queryAll`:
  the first match in the render root as an option, and every match as an array. `renderRoot` is a
  `U2`, so querying it needed a match or `shadowRoot`.
- `performUpdate` and `createRenderRoot` can be overridden (see Changed for `scheduleUpdate`).
- `Event.customEvent` also works after `open Browser`, whose `Event` value hid it ("The type
  'EventType' does not define the field, constructor or member 'customEvent'").
- `until (promise)` and `until (promise, placeholder)` overloads.
- `StaticHTML.mathml`.
- Firelight.Router: `RouterController.Navigate (url, ?replace)` goes to `url` from code as a
  click on a link to it does: a matching route pushes a history entry (or with `replace = true`
  replaces it) and renders, and an address no route matches is loaded by the browser.
- Firelight.Router: `RouterController(host, router, onRouteChange)` calls `onRouteChange` with
  the route after each change (a link, Back or Forward, `Navigate`) and when the host connects, so
  an Elmish component can keep the route in its model:
  `RouterController(this, router, fun route -> loop.dispatch (RouteChanged route))`.
- `loadPolyfill ()` in Firelight.Router: loads `urlpattern-polyfill` when `URLPattern` isn't
  native and returns a promise, for use inside a function, where `importPolyfill ()` can't go.

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
- **`PropertyDeclaration<'T>(...)` sets Lit's `type` from `'T`.** `Number` for `float`, `int`,
  `float32`, `int16`, `uint16`, `uint32`, `sbyte` and `byte`, and `Boolean` for `bool`, unless you
  pass ``` ``type`` ```. Before, Lit never saw `'T`, so `<my-quantity step="0.5">` set a `float`
  property to the string `"0.5"`, and `0 + step` gave `"00.5"`. An explicit ``` ``type`` ``` still
  wins, so existing declarations behave as before; drop the ones that only repeat `'T`:

  ```fsharp
  // 0.2
  "step", PropertyDeclaration<float>(``type`` = jsConstructor<Number>)
  "open", PropertyDeclaration<bool>(``type`` = jsConstructor<Boolean>, reflect = true)
  // 0.3
  "step", PropertyDeclaration<float>()
  "open", PropertyDeclaration<bool>(reflect = true)
  ```

  A numeric or `bool` property declared without `type` and set from an attribute now gets a number
  or a Boolean where it got the text. `int64`, `decimal` and other types still get the text.
- **`PropertyDeclaration<'T>(...)` is a method of `Lit`**, so it needs `open type Firelight.Lit`,
  which a component has for `html` (or write `Lit.PropertyDeclaration<'T>(...)`). A constructor
  can't see `'T` at compile time; an inline method can. Without the `open type`, the compiler says
  "Invalid use of a type name". The type `PropertyDeclaration<'T>`, what `properties` holds, is now
  an interface for reading options, with no constructor. A generic helper of your own that calls it
  must be `inline` too: `let prop<'T> () = PropertyDeclaration<'T>()` compiles in .NET, but Fable
  says "Cannot get type info of generic parameter T"; write `let inline prop<'T> () = ...`.
- **`attribute` takes a string or a Boolean**, as a `PropertyAttribute`, which F# converts either
  to. `!^` still compiles:

  ```fsharp
  // 0.2
  PropertyDeclaration<int>(attribute = !^"todo-id")
  PropertyDeclaration<Player list>(attribute = !^false)
  // 0.3
  PropertyDeclaration<int>(attribute = "todo-id")
  PropertyDeclaration<Player list>(attribute = false)
  ```

  A `U2<bool, string>` value no longer fits; unwrap it, or pass `!!value`.
- **`AttributeConverter` is `AttributeConverter<'T>`**, a typed options object (see Added). The
  old empty interface could only be created by `unbox`.
- **`repeat`'s template delegate is `RepeatTemplate<'T, 'R>`**, generic in what it returns, and
  `ItemTemplate<'T>` is now an abbreviation for `RepeatTemplate<'T, ChildRenderable>`. This lets
  the new one-argument overload sit beside it: with two overloads, F# types a lambda before
  choosing, and a lambda returning an `HTMLTemplateResult` no longer matched a delegate returning
  `ChildRenderable`. Calls with lambdas, and `ItemTemplate(fun item index -> ...)`, compile as
  before.
- **`scheduleUpdate` returns an `obj`**, Lit's `void | Promise<unknown>`, so an override can defer
  the update by returning a promise. It returned `unit`. An override returns
  `base.scheduleUpdate ()`, or a boxed promise:

  ```fsharp
  // 0.2
  override this.scheduleUpdate() = log "update"; base.scheduleUpdate ()
  // 0.3: the same compiles; to defer until the next frame:
  member this.ScheduleNow() = base.scheduleUpdate ()
  override this.scheduleUpdate() =
      let frame = JS.Constructors.Promise.Create(fun resolve _ -> window.requestAnimationFrame resolve |> ignore)
      // In then, so an error in the update rejects updateComplete rather than escaping the frame.
      box (frame.``then`` (fun _ -> this.ScheduleNow ()))
  ```

  An override that ends in something other than `base.scheduleUpdate ()` needs `box ()`.
- **`performUpdate` returns `unit`** and is overridable. It was typed as returning a
  `JS.Promise<obj> option`, which Lit 3 never returns.
- **`createRenderRoot` is an abstract member of `LitElement` returning `obj`**, so an override is
  `override this.createRenderRoot() = this`. It was a read-only property typed as a function, which
  a subclass could only shadow with `member`, as `LightDomElement` did; such a `member` now gets a
  "hides the abstract member" warning: make it `override`.
- **Firelight.Context: `ContextProvider` and `ContextConsumer` take any `ReactiveElement` host**,
  such as a `LitElement`, so both accept the F# `this` (with `as this`) as well as `jsThis`.
  `ContextProvider` required the `ReactiveElementHost` marker interface, which `LitElement` doesn't
  implement, and `ContextConsumer` a `LitElement`. The marker is removed: delete
  `interface ReactiveElementHost` from your components.
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

- **Firelight.Task: the signal is Fable.Fetch's `AbortSignal`.** The package is new in 0.3, but
  code built against the repository before this release saw its own `Firelight.Task.AbortSignal`,
  with only `aborted`, `reason` and `throwIfAborted`. That type is removed; `options.signal` is
  `Fetch.Types.AbortSignal`, the same JavaScript object, with the `abort` event too:

  ```fsharp
  // before
  open Firelight.Task
  [<Global>]
  let fetch (url: string, init: {| signal: AbortSignal |}) : JS.Promise<Response> = jsNative
  fetch (url, {| signal = options.signal |})
  // 0.3
  open Fetch
  fetch url [ Signal options.signal ]
  ```

  A binding of your own that typed its parameter `AbortSignal` with only `open Firelight.Task`
  says "The type 'AbortSignal' is not defined": add `open Fetch`, or write `Fetch.Types.AbortSignal`.
- **Firelight.Router: `EventHandlers.origin` is removed.** It held the page's origin, read from
  `window` when the module loaded. Read it where you need it: `window.location.origin`.
- **Firelight.Router: `importPolyfill ()` returns `unit`**, not a generic value. Code that used
  its result had a value that didn't exist; call it as a statement at the top level of a module.
  Inside a function, wait on `loadPolyfill ()` instead:

  ```fsharp
  // 0.2: compiled, then the module failed to parse
  let start () =
      importPolyfill ()
      render ()
  // 0.3
  let start () = loadPolyfill().``then`` (fun () -> render ())
  ```

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

- Firelight.Task: `initialState` was a generic `let` value, which Fable compiled to a function
  and called, so a task function that returned it threw and the task went to its error state. It
  is now inline and compiles to Lit's value.
- `PropertyDeclaration<float>()`, and other numeric or `bool` properties declared without
  ``` ``type`` ```, set from an attribute held the attribute's text: `0 + "0.5"` was `"00.5"`.
  See Changed.
- Firelight.Context: `ContextRoot` imported `ContextProvider` from `@lit/context`, so
  `ContextRoot()` threw. It now imports `ContextRoot`, and a provider defined after its consumers
  answers them (for consumers with `subscribe = true`).
- Firelight.Virtualizer: `Virtualizer.defineElement ()` registers with the global
  `customElements` rather than `window.customElements`, so a module that calls it also loads in
  Node.
- The `unsafeHTML` and `unsafeStatic` imports named `"unsafeHTML "` and `"unsafeStatic "`, with a
  trailing space. Fable trims import names, so they worked; the JavaScript is unchanged.
- XML docs: `noChange` had `nothing`'s summary; `ChildRenderable` and the template result types
  had none.
- Firelight.Router read `window.location` when its module loaded, so importing it failed where
  there is no `window`, such as Node. It now reads it when a link is clicked.
- `importPolyfill ()`'s docs now say to call it only at the top level of a module: it compiles to
  a top-level `await`, and inside a function the compiled module fails to parse.
