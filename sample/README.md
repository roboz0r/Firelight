# Sample Projects

These samples demonstrate Firelight patterns from first principles to production-scale applications. Each is a self-contained Fable + Vite project.

---

## GettingStarted

**`sample/GettingStarted/`**

A guided tour of every core Firelight concept, one module at a time. Start here if you're new to the library.

| Module | Demonstrates |
|---|---|
| `Defining.fs` | Registering a custom element with `defineElement` |
| `Rendering.fs` | Implementing `render()` to return an `html` template |
| `ReactiveProperties.fs` | Declaring reactive properties that trigger re-renders |
| `Styles.fs` | Scoped Shadow DOM CSS via the `css` tagged template |
| `Event.fs` | Handling DOM events with `@event-name` bindings |
| `Controllers.fs` | Composing behaviour with reactive controllers |
| `Context.fs` | Providing and consuming context values across the component tree |
| `Components.fs` | Building reusable, styled component templates |
| `Elmish.fs` | Wiring an Elmish MVU loop to a component with `ElmishController` |
| `App.fs` | Composing all demos into a single root element |

## Todo

**`sample/Todo/`**

A classic Todo app that demonstrates context-based state sharing in a real application.

**What it covers:**

- Modelling domain state as discriminated unions (`TodoItem`, `TodoState`, `TodoMsg`)
- Hosting an Elmish loop at the root component and broadcasting `dispatch` via context
- Child components that consume context to read state and dispatch messages without receiving props
- Clean separation between domain logic (`TodoModel.fs`) and UI components (`App.fs`)

This is the recommended reference for applications with a single shared state tree.

## Kanban

**`sample/Kanban/`**

A fully-featured Kanban board with drag-and-drop, inline editing, and persistent state. The most complete example of Firelight in a real-world scenario.

**What it covers:**

- Rich domain modelling: cards, columns, priorities, and multi-phase drag state
- Template functions as pure views — no component state, just `Model -> HTMLTemplateResult`
- Elmish `DevTools.withLocalStorage` for state persistence across hot-module reloads
- Drag-and-drop via native HTML5 drag events with JS interop for event details
- JSON serialization/deserialization with Thoth.Json (`Persistence.fs`)
- Tailwind CSS integration via Vite for utility-first styling

**Key files:**

| File | Role |
|---|---|
| `KanbanModel.fs` | Domain types: `Card`, `Column`, `DragState`, `Priority` |
| `KanbanMsg.fs` | Pure `update` function handling all user interactions |
| `Templates.fs` | All rendering logic as plain functions |
| `KanbanApp.fs` | Root element wiring Elmish + DevTools to the component |
| `Persistence.fs` | Thoth.Json encode/decode for localStorage |

## Kanban.Tests

**`sample/Kanban.Tests/`**

Unit tests for the Kanban domain logic using [Expecto](https://github.com/haf/expecto).

**What it covers:**

- Testing pure `update` functions without any DOM dependency
- Structuring Elmish applications so business logic is fully testable in isolation
- Running Fable-compiled F# tests with [Fable.Pyxpecto](https://github.com/Freymaurer/Fable.Pyxpecto)

This demonstrates how the template-centric architecture pays off — because all state transitions live in a pure `update` function, the entire domain can be tested without a browser.

## Kanban.E2E

**`sample/Kanban.E2E/`**

End-to-end browser tests for the Kanban app using [Playwright](https://playwright.dev/).

**What it covers:**

- Spinning up a local dev server (`Server.fs`) and running the compiled application
- Writing browser-level test scenarios against real Web Components in a Chromium context
- Testing drag-and-drop interactions and UI state changes end-to-end

## Running the Samples

Each sample uses Vite as the dev server. From a sample directory (e.g. `sample/GettingStarted`):

```sh
# Install JS dependencies
npm install

# Start the Fable compiler in watch mode and Vite dev server
npm run dev
```

Then open the URL printed by Vite (typically `http://localhost:5173`).
