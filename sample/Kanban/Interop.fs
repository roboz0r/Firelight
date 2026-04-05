namespace Kanban

open Fable.Core
open Fable.Core.JsInterop

/// Minimal helpers for the HTML Drag and Drop API.
/// Fable.Browser.Event 1.7.0 does not include DragEvent or DataTransfer.
module DragInterop =

    /// Get the dataTransfer object from a drag event.
    let inline dataTransfer (e: Browser.Types.Event) : obj = e?dataTransfer

    /// Set dataTransfer.effectAllowed on a drag event.
    let inline setEffectAllowed (effect: string) (e: Browser.Types.Event) = e?dataTransfer?effectAllowed <- effect

    /// Set dataTransfer.dropEffect on a drag event.
    let inline setDropEffect (effect: string) (e: Browser.Types.Event) = e?dataTransfer?dropEffect <- effect

    /// Get clientY from a drag/mouse event.
    let inline clientY (e: Browser.Types.Event) : float = e?clientY
