module App

open Browser
open Fable.Core
open Firelight
open Firelight.Observers
open Firelight.Virtualizer
open type Firelight.Lit

/// ResizeController observes its host and asks Lit to render when its size changes.
[<AttachMembers>]
type ResizeDemo() as this =
    inherit LitElement()

    let size =
        ResizeController<float>(
            this,
            ResizeControllerConfig(
                callback =
                    ResizeValueCallback(fun entries _ ->
                        if entries.Length = 0 then
                            0.0
                        else
                            entries.[0].contentRect.width
                    )
            )
        )

    override _.render() =
        html $"<p>Resize this panel: {size.value |> Option.defaultValue 0.0}px wide</p>"

let items = [| for index in 1..1000 -> $"Item {index}" |]

let renderItem =
    RenderItem<string>(fun item _ -> html $"<div class='item'>{item}</div>")

[<AttachMembers>]
type VirtualListDemo() =
    inherit LitElement()

    let listRef = createRef<Browser.Types.HTMLElement>()

    override _.render() =
        let list =
            Virtualizer.virtualize (
                VirtualizeConfig(
                    items,
                    RenderItem(fun item _ -> html $"<li>{item}</li>"),
                    keyFunction = KeyFunction(fun _ index -> box index),
                    scroller = true
                )
            )

        html
            $"""
            <p>The directive virtualizes a regular list:</p>
            <button @click={fun _ ->
                                listRef.value
                                |> Option.bind (fun element -> Virtualizer.get (element))
                                |> Option.bind (fun handle -> handle.element (499))
                                |> Option.iter (fun child -> child.scrollIntoView ())}>Scroll to item 500</button>
            <ul {ref listRef} style="height: 14rem; overflow: auto;">{list}</ul>
            <p>The custom element offers the same behavior:</p>
            <lit-virtualizer scroller style="height: 14rem;" .items={items} .renderItem={renderItem}></lit-virtualizer>
            """

defineElement<ResizeDemo> "resize-demo"
defineElement<VirtualListDemo> "virtual-list-demo"
Virtualizer.defineElement ()

let root = document.getElementById "app"

if root <> null then
    render (
        html
            $"""
            <style>
              body {{ font: 16px system-ui, sans-serif; margin: 2rem auto; max-width: 48rem; }}
              resize-demo {{ display: block; resize: horizontal; overflow: auto; min-width: 15rem; border: 1px solid #aaa; padding: 1rem; }}
              .item {{ padding: .35rem; border-bottom: 1px solid #ddd; }}
              li {{ padding: .35rem; }}
            </style>
            <h1>Observers and Virtualizer</h1>
            <resize-demo></resize-demo>
            <virtual-list-demo></virtual-list-demo>
            """,
        root
    )
    |> ignore
