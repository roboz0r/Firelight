# Elmish DevTools

`Firelight.Elmish.DevTools` provides development-time Program transformers that improve the experience of building Firelight components.

---

## `DevTools.withLocalStorage`

Persists the Elmish model to `localStorage` so that state survives hot-module replacement (HMR) page reloads. Lit web components cannot participate in standard HMR state-preservation because HMR tooling cannot reach inside shadow DOM. This transformer is the practical workaround.

### How it works

**On init** — the transformer attempts to deserialise a model from `localStorage[key]`. If it succeeds, that model is used as the starting state instead of the value returned by `init`. The original init `Cmd` still runs, so subscriptions are set up correctly and any required side-effects still fire.

**On every update** — a debounced write is scheduled. A monotonic nonce is incremented with each state transition. After `delay` milliseconds, the write only proceeds if the nonce has not changed in the meantime — i.e. the model has been idle long enough. Rapid dispatches (typing, dragging, animations) each reset the timer, so only a single write occurs once the burst settles.

The write is implemented as a fire-and-forget `Cmd` that never calls `dispatch`. The message type is unchanged and the transformer composes freely with other Program transformers such as `Program.withConsoleTrace`.

### Signature

```fsharp
DevTools.withLocalStorage
    (delay: int)
    (key: string)
    (encode: 'Model -> string)
    (decode: string -> 'Model option)
    : Program<'Arg, 'Model, 'Msg, 'View> -> Program<'Arg, 'Model, 'Msg, 'View>
```

| Parameter | Description |
| ----------- | ------------- |
| `delay` | Debounce delay in milliseconds. Use `DevTools.DefaultDelay` (500 ms) as a starting point. |
| `key` | `localStorage` key. Include a version suffix (see below). |
| `encode` | Serialise `'Model` to a string. |
| `decode` | Deserialise a string to `'Model option`. Return `None` on any failure; the program falls back to `init`. |

### Usage

```fsharp
open Elmish
open Firelight.Elmish

let encode (model: MyModel) : string = ... // e.g. Thoth.Json.Encode.Auto.toString model
let decode (json: string) : MyModel option = ... // e.g. Thoth.Json.Decode.Auto.fromString

let elmish =
    Program.mkSimple MyModule.init MyModule.update (fun _ _ -> ())
    |> DevTools.withLocalStorage DevTools.DefaultDelay "my-component-v1" encode decode
    |> fun p -> ElmishController(this, p)
```

It composes with other transformers in the usual way:

```fsharp
Program.mkSimple init update (fun _ _ -> ())
|> Program.withConsoleTrace
|> DevTools.withLocalStorage 500 "my-component-v1" encode decode
|> fun p -> ElmishController(this, p)
```

### Key versioning

When the shape of `'Model` changes, JSON stored under the old key will not deserialise correctly. Because `decode` returns `None` on failure, the program silently falls back to `init` — no crash. To make this explicit and avoid confusion from leftover stale values, encode a version into the key:

```text
"my-component-v1"   →   "my-component-v2"
```

Old entries remain in `localStorage` but are never read. Clear them manually via the browser DevTools console if needed.

### Serialisation requirements

The model must be fully serialisable. Non-serialisable values — API clients, DOM references, functions, F# closures — must not appear in the model. This is the right constraint anyway: such values have no meaningful serialised form and belong in Lit context consumers, not in model state (see [components-and-templates.md](components-and-templates.md)).

For F# records and discriminated unions over primitive types, `Thoth.Json` is the recommended serialisation library. For simple records, Fable's native JSON interop works:

```fsharp
open Fable.Core.JS

let encode (model: MyModel) : string = JSON.stringify model

let decode (json: string) : MyModel option =
    try JSON.parse json :?> MyModel |> Some
    with _ -> None
```

### Development only

`withLocalStorage` is a development aid. In production, removing it means `init` always produces the starting state — correct behaviour with no other changes required. A common pattern is to apply it conditionally:

```fsharp
let addDevTools program =
#if DEBUG
    program |> DevTools.withLocalStorage 500 "my-component-v1" encode decode
#else
    program
#endif

let elmish =
    Program.mkSimple init update (fun _ _ -> ())
    |> addDevTools
    |> fun p -> ElmishController(this, p)
```

### Behaviour on disconnect and reconnect

If a component is temporarily removed from the DOM while a debounce timer is pending, the timer fires normally and writes to `localStorage` if the model has been idle. On reconnect, `ElmishController.hostConnected` triggers a re-render from the current in-memory model — the `localStorage` value is only read during `init`, i.e. on first construction. Reconnecting an element does not re-run `init`.
