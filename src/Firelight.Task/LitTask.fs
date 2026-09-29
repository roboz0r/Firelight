namespace Firelight.Task

open Fable.Core
open Firelight

[<AllowNullLiteral>]
[<Interface>]
type AbortSignal =
    abstract aborted: bool with get
    abstract reason: obj with get
    abstract throwIfAborted: unit -> unit

/// States for task status.
type TaskStatus =
    | INITIAL = 0
    | PENDING = 1
    | COMPLETE = 2
    | ERROR = 3

[<AllowNullLiteral>]
[<Interface>]
type TaskFunctionOptions =
    abstract signal: AbortSignal with get

type TaskResult<'Result> = U2<'Result, JS.Promise<'Result>>

/// A task function receives the current arguments and a signal for cancelling stale work.
type TaskFunction<'Args, 'Result> = delegate of args: 'Args * options: TaskFunctionOptions -> TaskResult<'Result>

/// Compares the previous and current task arguments.
type TaskArgsEqual<'Args> = delegate of oldArgs: 'Args * newArgs: 'Args -> bool

/// Callbacks used by LitTask.render for each status.
[<AllowNullLiteral>]
[<Global>]
type StatusRenderer<'Result, 'Rendered>
    [<ParamObject; Emit("$0")>]
    (?initial: unit -> 'Rendered, ?pending: unit -> 'Rendered, ?complete: 'Result -> 'Rendered, ?error: obj -> 'Rendered)
    =
    member val initial: (unit -> 'Rendered) option = nativeOnly with get, set
    member val pending: (unit -> 'Rendered) option = nativeOnly with get, set
    member val complete: ('Result -> 'Rendered) option = nativeOnly with get, set
    member val error: (obj -> 'Rendered) option = nativeOnly with get, set

/// Configuration for an automatic or manually run Lit task.
[<AllowNullLiteral>]
[<Global>]
type TaskConfig<'Args, 'Result>
    [<ParamObject; Emit("$0")>]
    (
        task: TaskFunction<'Args, 'Result>,
        ?args: unit -> 'Args,
        ?autoRun: U2<bool, string>,
        ?argsEqual: TaskArgsEqual<'Args>,
        ?initialValue: 'Result,
        ?onComplete: 'Result -> unit,
        ?onError: obj -> unit
    ) =
    member val task: TaskFunction<'Args, 'Result> = nativeOnly with get, set
    member val args: (unit -> 'Args) option = nativeOnly with get, set
    /// Determines whether the task runs automatically when its arguments change after a host update.
    /// Defaults to true. With true, arguments are checked after willUpdate() and before update(),
    /// so changes must be made in willUpdate() or earlier to affect the current update.
    /// With 'afterUpdate', arguments are checked after the host update, including changes made
    /// during update(); a second host update is needed to render resulting task status changes.
    /// 'afterUpdate' is unlikely to be compatible with server-side rendering in the future.
    /// With false, call run explicitly to start the task.
    member val autoRun: U2<bool, string> option = nativeOnly with get, set
    /// Compares current and previous argument arrays. Returning true prevents an automatic run.
    /// The default is shallowArrayEquals; deepArrayEquals is also available.
    member val argsEqual: TaskArgsEqual<'Args> option = nativeOnly with get, set
    /// Initializes the task in COMPLETE status with this value. Initial arguments should match
    /// the value, since they are assumed to have produced it. When autoRun is true, the task
    /// does not run again until its arguments change.
    member val initialValue: 'Result option = nativeOnly with get, set
    member val onComplete: ('Result -> unit) option = nativeOnly with get, set
    member val onError: (obj -> unit) option = nativeOnly with get, set

/// A controller that runs an asynchronous task when its host element updates.
/// It requests host updates when the task starts and finishes so the host can render its
/// status, value, or error. The supplied arguments function is checked on each host update.
/// The task normally runs when its arguments change; set autoRun to false and call run to
/// control when it starts. The render method selects a callback for the current status.
/// Changes made in the host's willUpdate() are visible to the task in that update pass;
/// changes made in update() or updated() are visible in the next pass.
[<AllowNullLiteral>]
[<Import("Task", "@lit/task")>]
type LitTask<'Args, 'Result>(host: ReactiveControllerHost, config: TaskConfig<'Args, 'Result>) =
    member _.status: TaskStatus = nativeOnly
    /// Determines whether the task runs automatically after host updates. Defaults to true.
    /// See TaskConfig.autoRun for the behavior of true, false, and 'afterUpdate'.
    member val autoRun: U2<bool, string> = nativeOnly with get, set
    /// The result of the previous task run, if it resolved. Undefined before the first run
    /// and after a run that errored.
    member _.value: 'Result option = nativeOnly
    /// The error from the previous task run, if it rejected. Undefined before the first run
    /// and after a run that completed successfully.
    member _.error: obj option = nativeOnly
    /// Resolves when the current task run completes. If another run starts while one is
    /// pending, this promise is retained and resolves when the new run completes.
    member _.taskComplete: JS.Promise<'Result> = nativeOnly
    /// Runs the task manually, for example in response to an event. If args is omitted,
    /// the configured arguments function supplies the arguments for this run.
    member _.run(?args: 'Args) : JS.Promise<unit> = nativeOnly
    /// Aborts a pending run by aborting the signal passed to the task function. This has no
    /// effect unless the task is pending, and does not itself cancel the task function.
    /// The function must forward the signal to an API such as fetch() or handle cancellation
    /// through signal.throwIfAborted() or the signal's abort event. The optional reason is
    /// passed to AbortController.abort().
    member _.abort(?reason: obj) : unit = nativeOnly
    member _.render(renderer: StatusRenderer<'Result, 'Rendered>) : 'Rendered option = nativeOnly

    interface ReactiveController with
        member _.hostConnected() : unit = nativeOnly
        member _.hostDisconnected() : unit = nativeOnly
        member _.hostUpdate() : unit = nativeOnly
        member _.hostUpdated() : unit = nativeOnly


[<AutoOpen>]
module TaskHelpers =
    /// A special value that resets a task to INITIAL status when returned by its task function.
    [<Import("initialState", "@lit/task")>]
    let initialState<'Result> : TaskResult<'Result> = nativeOnly

    /// Compare task argument arrays by reference or primitive value.
    [<Import("shallowArrayEquals", "@lit/task")>]
    let shallowArrayEquals<'T> (oldArgs: 'T[]) (newArgs: 'T[]) : bool = nativeOnly

    /// Compare task argument arrays recursively.
    [<Import("deepArrayEquals", "@lit/task/deep-equals.js")>]
    let deepArrayEquals<'T> (oldArgs: 'T[]) (newArgs: 'T[]) : bool = nativeOnly
