[<AutoOpen>]
module Browser

open Fable.Core
open Browser.Types

[<AllowNullLiteral>]
type ElementDefinitionOptions =
    /// String specifying the name of a built-in element to extend. Used to create a customized built-in element.
    abstract member extends: string option with get, set

[<AllowNullLiteral>]
type CustomElementRegistry =
    /// <summary>
    /// Adds a definition for a custom element to the custom element registry, mapping its name to the constructor which will be used to create it.
    /// </summary>
    /// <remarks>
    /// Use `Fable.Core.JsInterop.jsConstructor` to convert an F# type to a JavaScript constructor.
    /// </remarks>
    abstract member define: name: string * constructor: 'TConstructor * ?options: ElementDefinitionOptions -> unit
    /// Returns the constructor for a previously-defined custom element.
    abstract member get: name: string -> 'TConstructor option
    /// <summary>
    /// Returns the name for a previously-defined custom element.
    /// </summary>
    /// <remarks>
    /// Use `Fable.Core.JsInterop.jsConstructor` to convert an F# type to a JavaScript constructor.
    /// </remarks>
    abstract member getName: constructor: 'TConstructor -> string option
    /// Upgrades all shadow-containing custom elements in a Node subtree, even before they are connected to the main document.
    abstract member upgrade: root: Node -> unit
    /// Returns a Promise that fulfills with the custom element's constructor when the named element is defined.
    abstract member whenDefined: name: string -> JS.Promise<'TConstructor>

type Window with
    /// Returns the CustomElementRegistry for the current document.
    [<Emit("$0.customElements")>]
    member _.customElements: CustomElementRegistry = nativeOnly

[<AllowNullLiteral>]
type HTMLTemplateElement =
    inherit HTMLElement
    abstract member content: DocumentFragment with get
    abstract member shadowRootMode: string option with get, set
    abstract member shadowRootDelegatesFocus: bool option with get, set
    abstract member shadowRootClonable: bool option with get, set
    abstract member shadowRootSerializable: bool option with get, set

type HTMLAnchorElement with
    /// Returns a string containing the Unicode serialization of the origin of the &lt;a&gt; element's href.
    [<Emit("$0.origin")>]
    member _.origin: string = nativeOnly
