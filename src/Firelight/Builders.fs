[<AutoOpen>]
module Firelight.Builders

open Browser.Types
open Fable.Core
open Fable.Core.JsInterop

[<Erase>]
type RenderableBuilder() =
    member inline _.Zero() = ignore

    member inline _.Yield(x: Renderable) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(x)

    member inline _.Yield(x: string) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(RenderableString x)

    member inline _.Yield(x: float) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(RenderableFloat x)

    // Fable small number types are not supported by the JS runtime, so we convert them to float
    // https://fable.io/docs/javascript/compatibility.html#numeric-types
    member inline _.Yield(x: float32) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(RenderableFloat(float x))

    member inline _.Yield(x: int) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(RenderableFloat(float x))

    member inline _.Yield(x: uint32) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(RenderableFloat(float x))

    member inline _.Yield(x: int16) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(RenderableFloat(float x))

    member inline _.Yield(x: uint16) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(RenderableFloat(float x))

    member inline _.Yield(x: int8) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(RenderableFloat(float x))

    member inline _.Yield(x: uint8) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(RenderableFloat(float x))

    member inline _.Yield(x: bool) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(RenderableBoolean x)

    member inline _.Yield(x: bigint) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(RenderableBigInt x)

    // 64-bit integers are not supported by the JS runtime, so we convert them to bigint
    member inline _.Yield(x: int64) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(RenderableBigInt(bigint x))

    member inline _.Yield(x: uint64) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(RenderableBigInt(bigint x))

    member inline _.Yield(x: decimal) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(RenderableBigInt(bigint x))

    member inline _.Yield(x: HTMLElement) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(RenderableHTMLElement x)

    member inline _.Yield(x: HTMLTemplateResult) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(RenderableHTMLTemplate x)

    member inline _.Yield(x: nothing) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(RenderableNothing x)

    member inline _.Yield(x: noChange) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(RenderableNoChange x)

    member inline _.Yield(x: ResizeArray<Renderable>) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(RenderableResizeArray x)

    member inline _.Yield(x: Renderable[]) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(RenderableArray x)

    member inline _.Yield(x: Renderable seq) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(RenderableSeq x)

    member inline _.YieldFrom(x: string seq) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(x |> Seq.map RenderableString |> RenderableSeq)

    member inline _.YieldFrom(x: float seq) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(x |> Seq.map RenderableFloat |> RenderableSeq)

    // Fable small number types are not supported by the JS runtime, so we convert them to float
    // https://fable.io/docs/javascript/compatibility.html#numeric-types
    member inline _.YieldFrom(x: float32 seq) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(x |> Seq.map (float >> RenderableFloat) |> RenderableSeq)

    member inline _.YieldFrom(x: int seq) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(x |> Seq.map (float >> RenderableFloat) |> RenderableSeq)

    member inline _.YieldFrom(x: uint32 seq) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(x |> Seq.map (float >> RenderableFloat) |> RenderableSeq)

    member inline _.YieldFrom(x: int16 seq) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(x |> Seq.map (float >> RenderableFloat) |> RenderableSeq)

    member inline _.YieldFrom(x: uint16 seq) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(x |> Seq.map (float >> RenderableFloat) |> RenderableSeq)

    member inline _.YieldFrom(x: int8 seq) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(x |> Seq.map (float >> RenderableFloat) |> RenderableSeq)

    member inline _.YieldFrom(x: uint8 seq) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(x |> Seq.map (float >> RenderableFloat) |> RenderableSeq)

    member inline _.YieldFrom(x: bool seq) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(x |> Seq.map RenderableBoolean |> RenderableSeq)

    member inline _.YieldFrom(x: bigint seq) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(x |> Seq.map RenderableBigInt |> RenderableSeq)

    // 64-bit integers are not supported by the JS runtime, so we convert them to bigint
    member inline _.YieldFrom(x: int64 seq) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(x |> Seq.map (bigint >> RenderableBigInt) |> RenderableSeq)

    member inline _.YieldFrom(x: uint64 seq) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(x |> Seq.map (bigint >> RenderableBigInt) |> RenderableSeq)

    member inline _.YieldFrom(x: decimal seq) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(x |> Seq.map (bigint >> RenderableBigInt) |> RenderableSeq)

    member inline _.YieldFrom(x: HTMLElement seq) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(x |> Seq.map RenderableHTMLElement |> RenderableSeq)

    member inline _.YieldFrom(x: HTMLTemplateResult seq) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(x |> Seq.map RenderableHTMLTemplate |> RenderableSeq)

    member inline _.YieldFrom(x: Renderable seq) =
        fun (xs: ResizeArray<Renderable>) -> xs.Add(RenderableSeq x)

    member inline _.Run([<InlineIfLambda>] f: ResizeArray<Renderable> -> unit) =
        let xs = ResizeArray<Renderable>()
        f xs

        if xs.Count = 1 then
            xs.[0] // If there's only one item, return it directly
        else
            RenderableResizeArray xs

    member inline _.Delay([<InlineIfLambda>] f: unit -> ResizeArray<Renderable> -> unit) =
        fun (xs: ResizeArray<Renderable>) -> f () xs

    member inline _.Combine
        ([<InlineIfLambda>] f1: ResizeArray<Renderable> -> unit, [<InlineIfLambda>] f2: ResizeArray<Renderable> -> unit)
        =
        fun (xs: ResizeArray<Renderable>) ->
            f1 xs
            f2 xs

    member inline _.For(xs: seq<Renderable>, [<InlineIfLambda>] f: Renderable -> ResizeArray<Renderable> -> unit) =
        fun (ys: ResizeArray<Renderable>) ->
            for x in xs do
                f x ys

    member inline _.While
        ([<InlineIfLambda>] guard: unit -> bool, [<InlineIfLambda>] f: ResizeArray<Renderable> -> unit)
        =
        fun (xs: ResizeArray<Renderable>) ->
            while guard () do
                f xs

[<Erase>]
type CSSResultGroupBuilder() =
    member inline _.Zero() = ignore

    member inline _.Yield(x: CSSResultOrNative) =
        fun (xs: ResizeArray<CSSResultGroup>) -> xs.Add(CSSResultSingle x)

    member inline _.Yield(x: CSSResult) =
        fun (xs: ResizeArray<CSSResultGroup>) -> xs.Add(CSSResultSingle !^x)

    member inline _.Yield(x: CSSStyleSheet) =
        fun (xs: ResizeArray<CSSResultGroup>) -> xs.Add(CSSResultSingle !^x)

    member inline _.Yield(x: CSSResultArray) =
        fun (xs: ResizeArray<CSSResultGroup>) -> xs.Add(CSSResultGroup x)

    member inline _.Yield(x: CSSResultGroup) =
        fun (xs: ResizeArray<CSSResultGroup>) -> xs.Add(CSSResultGroup [| x |])

    member inline _.Yield(x: ResizeArray<CSSResultGroup>) =
        fun (xs: ResizeArray<CSSResultGroup>) -> xs.Add(CSSResultGroupResize x)

    member inline _.YieldFrom(x: CSSResultOrNative seq) =
        fun (xs: ResizeArray<CSSResultGroup>) ->
            xs.Add(x |> Seq.map CSSResultSingle |> ResizeArray |> CSSResultGroupResize)

    member inline _.YieldFrom(x: CSSResult seq) =
        fun (xs: ResizeArray<CSSResultGroup>) ->
            xs.Add(
                x
                |> Seq.map (U2.Case1 >> CSSResultSingle)
                |> ResizeArray
                |> CSSResultGroupResize
            )

    member inline _.YieldFrom(x: CSSStyleSheet seq) =
        fun (xs: ResizeArray<CSSResultGroup>) ->
            xs.Add(
                x
                |> Seq.map (U2.Case2 >> CSSResultSingle)
                |> ResizeArray
                |> CSSResultGroupResize
            )

    member inline _.YieldFrom(x: CSSResultGroup seq) =
        fun (xs: ResizeArray<CSSResultGroup>) -> xs.Add(x |> ResizeArray |> CSSResultGroupResize)

    member inline _.Run([<InlineIfLambda>] f: ResizeArray<CSSResultGroup> -> unit) =
        let xs = ResizeArray<CSSResultGroup>()
        f xs

        if xs.Count = 1 then
            xs.[0] // If there's only one item, return it directly
        else
            CSSResultGroupResize xs

    member inline _.Delay([<InlineIfLambda>] f: unit -> ResizeArray<CSSResultGroup> -> unit) =
        fun (xs: ResizeArray<CSSResultGroup>) -> f () xs

    member inline _.Combine
        (
            [<InlineIfLambda>] f1: ResizeArray<CSSResultGroup> -> unit,
            [<InlineIfLambda>] f2: ResizeArray<CSSResultGroup> -> unit
        ) =
        fun (xs: ResizeArray<CSSResultGroup>) ->
            f1 xs
            f2 xs

    member inline _.For
        (xs: seq<CSSResultGroup>, [<InlineIfLambda>] f: CSSResultGroup -> ResizeArray<CSSResultGroup> -> unit)
        =
        fun (ys: ResizeArray<CSSResultGroup>) ->
            for x in xs do
                f x ys

    member inline _.While
        ([<InlineIfLambda>] guard: unit -> bool, [<InlineIfLambda>] f: ResizeArray<CSSResultGroup> -> unit)
        =
        fun (xs: ResizeArray<CSSResultGroup>) ->
            while guard () do
                f xs

[<Erase>]
let renderable = RenderableBuilder()

[<Erase>]
let cssResultGroup = CSSResultGroupBuilder()
