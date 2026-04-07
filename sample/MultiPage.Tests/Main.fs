module MultiPage.Tests.Main

open Fable.Pyxpecto

[<EntryPoint>]
let main argv = Pyxpecto.runTests [||] RouterTests.all
