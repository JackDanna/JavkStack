module CloudInfrastructure.Program

open Pulumi.FSharp

[<EntryPoint>]
let main _ =
    Deployment.run (fun () -> Stack.resources ())
