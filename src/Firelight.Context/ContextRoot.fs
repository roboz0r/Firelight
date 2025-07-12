namespace Firelight.Context

open Fable.Core
open Browser.Types
open Firelight

/// A ContextRoot can be used to gather unsatisfied context requests and
/// re-dispatch them when new providers which satisfy matching context keys are
/// available.
///
/// This allows providers to be added to a DOM tree, or upgraded, after the
/// consumers.
[<AllowNullLiteral>]
[<Import("ContextProvider", "@lit/context")>]
type ContextRoot() =
    /// <summary>
    /// Attach the ContextRoot to a given element to intercept <c>context-request</c> and
    /// <c>context-provider</c> events.
    /// </summary>
    /// <param name="element">an element to add event listeners to</param>
    member _.attach(element: HTMLElement) : unit = nativeOnly

    /// <summary>Removes the ContextRoot event listeners from a given element.</summary>
    /// <param name="element">an element from which to remove event listeners</param>
    member _.detach(element: HTMLElement) : unit = nativeOnly

    // TODO: LitElement actually inherits from HTMLElement, but HTMLElement is not a class in Fable
    // So we'd have to reimplement 100s of HTMLElement properties and methods on ReactiveElement

    /// <summary>
    /// Attach the ContextRoot to a given element to intercept <c>context-request</c> and
    /// <c>context-provider</c> events.
    /// </summary>
    /// <param name="element">an element to add event listeners to</param>
    member _.attach(element: LitElement) : unit = nativeOnly

    /// <summary>Removes the ContextRoot event listeners from a given element.</summary>
    /// <param name="element">an element from which to remove event listeners</param>
    member _.detach(element: LitElement) : unit = nativeOnly
