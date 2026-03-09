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

open Api.Shared

let api: Api = 
    Remoting.createApi () |> Remoting.withRouteBuilder Api.Shared.routingBuilder |> Remoting.buildProxy<Api>

Program.mkProgram
    (fun () -> Index.Shared.init api.sayBanana (Router.currentUrl ()))
    (Index.Shared.update api)
    Index.WebClient.view
#if DEBUG
|> Program.withConsoleTrace
#endif
|> Program.withReactSynchronous "elmish-app"

|> Program.run