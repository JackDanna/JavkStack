module App

open Elmish
open Elmish.React

open Feliz.Router
open Fable.Remoting.Client

open Fable.Core.JsInterop

importSideEffects "./index.css"

#if DEBUG
open Elmish.HMR
#endif

open Fable.SimpleJson


open System


Program.mkProgram
    (fun () -> Index.Shared.init (Router.currentUrl ()))
    Index.Shared.update
    Index.WebClient.view
#if DEBUG
|> Program.withConsoleTrace
#endif
|> Program.withReactSynchronous "elmish-app"

|> Program.run