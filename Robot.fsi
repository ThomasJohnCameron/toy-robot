module Robot

type Direction =
    | North
    | South
    | East
    | West

type Robot =
    {
      x: int
      y: int
      direction: Direction
    }

type Environment =
    {
      robot: Option<Robot>
      reports: List<string>
    }

type Command =
    | Place of Robot
    | Move
    | Left
    | Right
    | Report
    | Reset

module Environment =
    
    val initial: Environment
    
    val log: env: Environment -> unit

module Parse =
    
    val fromString: str: string -> Option<Command>
    
    val fromList: strs: List<string> -> Result<List<Command>,string>

module Process =
    
    val command: size: int -> env: Environment -> cmd: Command -> Environment
    
    val commands:
      size: int -> env: Environment -> cmds: List<Command> -> Environment
