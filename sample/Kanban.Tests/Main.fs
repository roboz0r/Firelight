module Kanban.Tests.Main

open Fable.Pyxpecto

[<EntryPoint>]
let main argv = Pyxpecto.runTests [||] UpdateTests.all
