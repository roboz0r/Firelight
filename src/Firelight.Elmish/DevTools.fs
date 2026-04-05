namespace Firelight.Elmish

open Elmish
open Browser.Dom

/// Development-time helpers for Firelight.Elmish programs.
/// These are intended for use during development only and should not be
/// included in production builds.
module DevTools =

    /// Default debounce delay in milliseconds used by <see cref="withLocalStorage"/>.
    [<Literal>]
    let DefaultDelay = 500

    /// <summary>
    /// Program transformer that persists and restores the Elmish model via
    /// <c>localStorage</c>, enabling state to survive hot-module replacement
    /// (HMR) page reloads.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// On init, the transformer attempts to deserialise a previously saved
    /// model from <c>localStorage[key]</c>. If deserialisation succeeds the
    /// saved model is used as the initial state; the original init <c>Cmd</c>
    /// still runs so subscriptions and any required side-effects are set up
    /// correctly. If nothing is stored, or if <c>decode</c> returns
    /// <c>None</c>, the program falls back to the real <c>init</c> result.
    /// </para>
    /// <para>
    /// On every state transition the transformer schedules a debounced write
    /// to <c>localStorage</c>. A monotonic nonce is incremented on each
    /// update; the scheduled write only proceeds if the nonce has not changed
    /// by the time the <c>delay</c> elapses, i.e. the model has been idle for
    /// at least <c>delay</c> milliseconds. This avoids a localStorage write on
    /// every keystroke or rapid dispatch.
    /// </para>
    /// <para>
    /// The write is implemented as a fire-and-forget <c>Cmd</c> that never
    /// calls <c>dispatch</c>. The message type is therefore unchanged and the
    /// transformer composes freely with other Program transformers.
    /// </para>
    ///
    /// <b>Key versioning</b>
    /// <para>
    /// When the shape of <c>'Model</c> changes, the stored JSON will no longer
    /// deserialise correctly. Encode a version into the key (e.g.
    /// <c>"my-component-v2"</c>) so that stale values are silently ignored via
    /// <c>decode</c> returning <c>None</c>.
    /// </para>
    ///
    /// <b>Serialisation</b>
    /// <para>
    /// The model must be fully serialisable. Non-serialisable values — API
    /// clients, DOM references, functions — must not appear in the model.
    /// Provide these as Lit context consumers instead (see
    /// components-and-templates.md). Encode and decode functions are
    /// user-supplied so any JSON library (e.g. Thoth.Json) can be used.
    /// </para>
    /// </remarks>
    ///
    /// <param name="delay">
    /// Debounce delay in milliseconds. The model is written to localStorage
    /// only after this many milliseconds of inactivity.
    /// Use <see cref="DefaultDelay"/> (500 ms) as a starting point.
    /// </param>
    /// <param name="key">localStorage key. Include a version suffix.</param>
    /// <param name="encode">Serialise the model to a string.</param>
    /// <param name="decode">
    /// Deserialise a string to the model. Return <c>None</c> on failure;
    /// the program will fall back to <c>init</c>.
    /// </param>
    /// <param name="program">The Elmish program to transform.</param>
    let withLocalStorage
        (delay: int)
        (key: string)
        (encode: 'Model -> string)
        (decode: string -> 'Model option)
        (program: Program<'Arg, 'Model, 'Msg, 'View>)
        : Program<'Arg, 'Model, 'Msg, 'View> =

        let mutable nonce = 0

        let restore () =
            window.localStorage.getItem key |> Option.ofObj |> Option.bind decode

        let debouncedSave (model: 'Model) : Cmd<'Msg> =
            nonce <- nonce + 1
            let savedNonce = nonce

            [
                fun (_: 'Msg -> unit) ->
                    window.setTimeout (
                        (fun () ->
                            if nonce = savedNonce then
                                window.localStorage.setItem (key, encode model)
                        ),
                        delay
                    )
                    |> ignore
            ]

        program
        |> Program.map
            (fun init ->
                fun arg ->
                    let model, cmd = init arg

                    match restore () with
                    | Some saved -> saved, cmd
                    | None -> model, cmd
            )
            (fun update ->
                fun msg model ->
                    let newModel, cmd = update msg model
                    newModel, Cmd.batch [ cmd; debouncedSave newModel ]
            )
            id
            id
            id
            id
