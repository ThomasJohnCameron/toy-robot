module Program

let rec readLines () =
    seq {
        let line = System.Console.ReadLine()
        if line <> "" && line <> null then
            yield line
            yield! readLines ()
    }


[<EntryPoint>]
let main argv =
    let lines = readLines () |> Seq.toList
    match Robot.Parse.fromList lines with
    | Error e -> printfn "%s" e
    | Ok cmds ->
        Robot.Process.commands 5 Robot.Environment.initial cmds
        |> Robot.Environment.log
    0