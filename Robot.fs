module Robot

type Direction =
    | North
    | South
    | East
    | West

type Robot =
    { x: int
      y: int
      direction: Direction }

type Environment =
    { robot: Option<Robot>
      reports: List<string> }

type Command =
    | Place of Robot
    | Move
    | Left
    | Right
    | Report
    | Reset

module Environment =
    let initial = { robot = None; reports = [] }

    let log (env: Environment) : unit =
        env.reports
        |> List.rev
        |> List.iter (fun s -> printfn "%s" s)

module Parse =
    open System.Text.RegularExpressions

    let (|PlaceMatch|_|) input : Option<Robot> =
        let m = Regex.Match(input, "PLACE (\d+),(\d+),(NORTH|SOUTH|EAST|WEST)$")

        if (m.Success) then
            Some(
                { x = int m.Groups.[1].Value
                  y = int m.Groups.[2].Value
                  direction =
                    match m.Groups.[3].Value with
                    | "NORTH" -> North
                    | "SOUTH" -> South
                    | "EAST" -> East
                    | "WEST" -> West
                    | _ -> failwith "unreachable" }
            )
        else
            None

    let fromString (str: string) : Option<Command> =
        match str with
        | "MOVE" -> Some Move
        | "LEFT" -> Some Left
        | "RIGHT" -> Some Right
        | "REPORT" -> Some Report
        | "RESET" -> Some Reset
        | PlaceMatch robot -> Some(Place robot)
        | _ -> None

    let fromList (strs: List<string>) : Result<List<Command>, string> =
        strs
        |> List.fold
            (fun ac el ->
                ac
                |> Result.bind (fun cmds ->
                    match fromString el with
                    | Some cmd -> Ok(List.append cmds [ cmd ])
                    | None -> Error $"Unable to parse: \"{el}\""))
            (Ok [])

    module Tests =
        open NUnit.Framework

        [<Test>]
        let ``Parse "MOVE" string`` () =
            let result = fromString "MOVE"
            Assert.AreEqual(Some Move, result)

        [<Test>]
        let ``Parse "LEFT" string`` () =
            let result = fromString "LEFT"
            Assert.AreEqual(Some Left, result)

        [<Test>]
        let ``Parse "RIGHT" string`` () =
            let result = fromString "RIGHT"
            Assert.AreEqual(Some Right, result)

        [<Test>]
        let ``Parse "REPORT" string`` () =
            let result = fromString "REPORT"
            Assert.AreEqual(Some Report, result)

        [<Test>]
        let ``Parse "PLACE" string`` () =
            let result = fromString "PLACE 0,0,NORTH"
            Assert.AreEqual(Some(Place { x = 0; y = 0; direction = North }), result)

        [<Test>]
        let ``Parse empty string`` () =
            let result = fromString ""
            Assert.AreEqual(None, result)

        [<Test>]
        let ``Parse arbitrary string`` () =
            let result = fromString "shall i compare thee to a summer's day"
            Assert.AreEqual(None, result)

        [<Test>]
        let ``Parse arbitrary string with newlines`` () =
            let result = fromString "shall\ni\ncompare\nthee\nto\na\nsummer's\nday"
            Assert.AreEqual(None, result)

        [<Test>]
        let ``Parse and ignore lowercase invalid "move" string`` () =
            let result = fromString "move"
            Assert.AreEqual(None, result)

        [<Test>]
        let ``Parse and ignore lowercase invalid "left" string`` () =
            let result = fromString "left"
            Assert.AreEqual(None, result)

        [<Test>]
        let ``Parse and ignore lowercase invalid "right" string`` () =
            let result = fromString "right"
            Assert.AreEqual(None, result)

        [<Test>]
        let ``Test fromString to make sure it's returning the first error only`` () =
            let result =
                fromList [ "foo"
                           "PLACE 1,2,NORTH"
                           "bar" ]

            Assert.AreEqual((Error "Unable to parse: \"foo\"": Result<List<Command>, string>), result)

module Process =

    let left (robot: Robot) : Robot =
        match robot.direction with
        | North -> { robot with direction = West }
        | South -> { robot with direction = East }
        | East -> { robot with direction = North }
        | West -> { robot with direction = South }

    let right (robot: Robot) : Robot =
        match robot.direction with
        | North -> { robot with direction = East }
        | South -> { robot with direction = West }
        | East -> { robot with direction = South }
        | West -> { robot with direction = North }

    let boundMoveCoordinates (size: int) (i: int) : int =
        match i with
        | i when i >= size -> size - 1
        | i when i < 0 -> 0
        | _ -> i

    let move (size: int) (robot: Robot) : Robot =
        let { x = x; y = y; direction = direction } = robot

        match direction with
        | North -> { robot with y = (y + 1) |> (boundMoveCoordinates size) }
        | South -> { robot with y = (y - 1) |> (boundMoveCoordinates size) }
        | East -> { robot with x = (x + 1) |> (boundMoveCoordinates size) }
        | West -> { robot with x = (x - 1) |> (boundMoveCoordinates size) }

    let command (size: int) (env: Environment) (cmd: Command) : Environment =
        match cmd with
        | Place (robot: Robot) ->
            if robot.x >= 0
               && robot.y >= 0
               && robot.x < size
               && robot.y < size then
                { env with robot = Some robot }
            else
                env
        | Move -> { env with robot = env.robot |> Option.map (move size) }
        | Left -> { env with robot = env.robot |> Option.map left }
        | Right -> { env with robot = env.robot |> Option.map right }
        | Report ->
            match env.robot with
            | Some { x = x; y = y; direction = direction } ->
                { env with reports = List.append env.reports [ ($"{x},{y},{direction}".ToUpper()) ] }
            | None -> env
        | Reset -> { robot = None; reports = [] }

    let commands (size: int) (env: Environment) (cmds: List<Command>) : Environment = List.fold (command size) env cmds


    module Tests =
        open NUnit.Framework

        [<Test>]
        let ``report without robot is empty`` () =
            let result = command 5 Environment.initial Report
            CollectionAssert.AreEqual(([]: List<string>), result.reports)

        [<Test>]
        let ``place robot work with empty environment`` () =
            let result =
                command 5 Environment.initial (Place { x = 2; y = 2; direction = East })

            Assert.AreEqual(Some { x = 2; y = 2; direction = East }, result.robot)

        [<Test>]
        let ``place robot work with populated environment`` () =
            let result =
                command
                    5
                    { robot = Some { x = 2; y = 2; direction = East }
                      reports = [] }
                    (Place { x = 3; y = 3; direction = North })

            Assert.AreEqual(Some { x = 3; y = 3; direction = North }, result.robot)

        [<Test>]
        let ``cannot move south of 0`` () =
            let result =
                command
                    5
                    { robot = Some { x = 0; y = 0; direction = South }
                      reports = [] }
                    Move

            Assert.AreEqual(Some { x = 0; y = 0; direction = South }, result.robot)

        [<Test>]
        let ``cannot move west of 0`` () =
            let result =
                command
                    5
                    { robot = Some { x = 0; y = 0; direction = West }
                      reports = [] }
                    Move

            Assert.AreEqual(Some { x = 0; y = 0; direction = West }, result.robot)

        [<Test>]
        let ``cannot move north of upper size`` () =
            let result =
                command
                    5
                    { robot = Some { x = 0; y = 4; direction = North }
                      reports = [] }
                    Move

            Assert.AreEqual(Some { x = 0; y = 4; direction = North }, result.robot)

        [<Test>]
        let ``cannot move east of upper size`` () =
            let result =
                command
                    5
                    { robot = Some { x = 4; y = 0; direction = East }
                      reports = [] }
                    Move

            Assert.AreEqual(Some { x = 4; y = 0; direction = East }, result.robot)

        [<Test>]
        let ``four lefts gives the same robot`` () =
            let robot = Some { x = 0; y = 0; direction = North }
            let result = commands 5 { robot = robot; reports = [] } [ Left; Left; Left; Left ]
            Assert.AreEqual(robot, result.robot)

        [<Test>]
        let ``four rights gives the same robot`` () =
            let robot = Some { x = 0; y = 0; direction = North }

            let result =
                commands 5 { robot = robot; reports = [] } [ Right; Right; Right; Right ]

            Assert.AreEqual(robot, result.robot)

        [<Test>]
        let ``no reports when running commands if there was no place command`` () =
            let (robot: Option<Robot>) = None

            let result =
                commands 5 { robot = robot; reports = [] } [ Right; Left; Move; Report ]

            CollectionAssert.AreEqual(([]: List<string>), result.reports)

        [<Test>]
        let ``test for simple valid turn`` () =
            let result =
                commands
                    5
                    { robot = Some { x = 4; y = 0; direction = East }
                      reports = [] }
                    [ Left ]

            Assert.AreEqual(Some { x = 4; y = 0; direction = North }, result.robot)

        [<Test>]
        let ``test for simple valid move`` () =
            let result =
                commands
                    5
                    { robot = Some { x = 2; y = 0; direction = East }
                      reports = [] }
                    [ Move ]

            Assert.AreEqual(Some { x = 3; y = 0; direction = East }, result.robot)

        [<Test>]
        let ``test for an invalid placement`` () =
            let result =
                commands 5 { robot = None; reports = [] } [ Place { x = 2; y = 100; direction = East } ]

            Assert.AreEqual((None: Option<Robot>), result.robot)

        [<Test>]
        let ``report is ignored before placement`` () =
            let result =
                commands
                    5
                    { robot = None; reports = [] }
                    [ Report
                      Place { x = 1; y = 1; direction = North }
                      Report ]

            CollectionAssert.AreEqual([ "1,1,NORTH" ], result.reports)

        [<Test>]
        let ``multiple reports occur in order`` () =
            let result =
                commands
                    5
                    { robot = None; reports = [] }
                    [ Place { x = 0; y = 0; direction = North }
                      Report
                      Move
                      Report
                      Report
                      Move
                      Report
                      Report
                      Report
                      Move
                      Report
                      Report
                      Report
                      Report
                      Move
                      Report
                      Report
                      Report
                      Report
                      Report
                      Move
                      Report
                      Move
                      Report
                      Move ]

            CollectionAssert.AreEqual(
                [ "0,0,NORTH"
                  "0,1,NORTH"
                  "0,1,NORTH"
                  "0,2,NORTH"
                  "0,2,NORTH"
                  "0,2,NORTH"
                  "0,3,NORTH"
                  "0,3,NORTH"
                  "0,3,NORTH"
                  "0,3,NORTH"
                  "0,4,NORTH"
                  "0,4,NORTH"
                  "0,4,NORTH"
                  "0,4,NORTH"
                  "0,4,NORTH"
                  "0,4,NORTH"
                  "0,4,NORTH" ],
                result.reports
            )
