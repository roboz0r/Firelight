namespace Lit
// lit-html.d.ts
open Fable.Core
open Browser.Types

module RenderOptions =
    type creationScope =
        abstract member importNode: node: Node * ?deep: bool -> Node

/// <summary>
/// Object specifying options for controlling lit-html rendering. Note that
/// while `render` may be called multiple times on the same `container` (and
/// `renderBefore` reference node) to efficiently update the rendered content,
/// only the options passed in during the first render are respected during
/// the lifetime of renders to that unique `container` + `renderBefore`
/// combination.
/// </summary>
/// <seealso href="https://lit.dev/docs/api/LitElement/#RenderOptions"/>
[<AllowNullLiteral>]
[<Global>]
type RenderOptions
    [<ParamObject; Emit("$0")>]
    (?host: obj, ?renderBefore: ChildNode, ?creationScope: RenderOptions.creationScope, ?isConnected: bool) =

    /// <summary>
    /// An object to use as the `this` value for event listeners. It's often
    /// useful to set this to the host component rendering a template.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/api/LitElement/#RenderOptions.host"/>
    member val host: obj option = nativeOnly with get, set

    /// <summary>
    /// A DOM node before which to render content in the container.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/api/LitElement/#RenderOptions.renderBefore"/>
    member val renderBefore: ChildNode option = nativeOnly with get, set

    /// <summary>
    /// Node used for cloning the template (`importNode` will be called on this
    /// node). This controls the `ownerDocument` of the rendered DOM, along with
    /// any inherited context. Defaults to the global `document`.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/api/LitElement/#RenderOptions.creationScope"/>
    member val creationScope: obj option = nativeOnly with get, set

    /// <summary>
    /// The initial connected state for the top-level part being rendered. If no
    /// `isConnected` option is set, `AsyncDirective`s will be connected by
    /// default. Set to `false` if the initial render occurs in a disconnected tree
    /// and `AsyncDirective`s should see `isConnected === false` for their initial
    /// render. The `part.setConnected()` method must be used subsequent to initial
    /// render to change the connected state of the part.
    /// </summary>
    /// <seealso href="https://lit.dev/docs/api/LitElement/#RenderOptions.isConnected"/>
    member val isConnected: bool option = nativeOnly with get, set

/// <summary>
/// Anything that Lit can render as the child of an HTML element
/// </summary>
/// <seealso href="https://lit.dev/docs/components/rendering/#renderable-values"/>
[<AllowNullLiteral>]
type Renderable = interface end

[<AllowNullLiteral>]
type ChildPartRenderable =
    inherit Renderable

/// The return type of the template tag functions.
[<AllowNullLiteral>]
type TemplateResult =
    inherit Renderable

[<AllowNullLiteral>]
type HTMLTemplateResult =
    inherit TemplateResult
    inherit ChildPartRenderable

[<AllowNullLiteral>]
type SVGTemplateResult =
    inherit TemplateResult

[<AllowNullLiteral>]
type MathMLTemplateResult =
    inherit TemplateResult

[<AllowNullLiteral>]
type Disconnectable = interface end

[<AllowNullLiteral>]
type ChildPart =
    inherit Disconnectable
    abstract ``type``: int
    abstract options: RenderOptions option
    /// <summary>
    /// The parent node into which the part renders its content.
    ///
    /// A ChildPart's content consists of a range of adjacent child nodes of
    /// <c>.parentNode</c>, possibly bordered by 'marker nodes' (<c>.startNode</c> and
    /// <c>.endNode</c>).
    ///
    /// - If both <c>.startNode</c> and <c>.endNode</c> are non-null, then the part's content
    /// consists of all siblings between <c>.startNode</c> and <c>.endNode</c>, exclusively.
    ///
    /// - If <c>.startNode</c> is non-null but <c>.endNode</c> is null, then the part's
    /// content consists of all siblings following <c>.startNode</c>, up to and
    /// including the last child of <c>.parentNode</c>. If <c>.endNode</c> is non-null, then
    /// <c>.startNode</c> will always be non-null.
    ///
    /// - If both <c>.endNode</c> and <c>.startNode</c> are null, then the part's content
    /// consists of all child nodes of <c>.parentNode</c>.
    /// </summary>
    abstract parentNode: Node
    /// <summary>
    /// The part's leading marker node, if any. See <c>.parentNode</c> for more
    /// information.
    /// </summary>
    abstract startNode: Node option
    /// <summary>
    /// The part's trailing marker node, if any. See <c>.parentNode</c> for more
    /// information.
    /// </summary>
    abstract endNode: Node option

/// <summary>
/// A top-level <c>ChildPart</c> returned from <c>render</c> that manages the connected
/// state of <c>AsyncDirective</c>s created throughout the tree below it.
/// </summary>
[<AllowNullLiteral>]
type RootPart =
    inherit ChildPart
    /// <summary>
    /// Sets the connection state for <c>AsyncDirective</c>s contained within this root
    /// ChildPart.
    ///
    /// lit-html does not automatically monitor the connectedness of DOM rendered;
    /// as such, it is the responsibility of the caller to <c>render</c> to ensure that
    /// <c>part.setConnected(false)</c> is called before the part object is potentially
    /// discarded, to ensure that <c>AsyncDirective</c>s have a chance to dispose of
    /// any resources being held. If a <c>RootPart</c> that was previously
    /// disconnected is subsequently re-connected (and its <c>AsyncDirective</c>s should
    /// re-connect), <c>setConnected(true)</c> should be called.
    /// </summary>
    /// <param name="isConnected">
    /// Whether directives within this tree should be connected
    /// or not
    /// </param>
    abstract setConnected: isConnected: bool -> unit
