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

open LoginPage.Shared
open Fable.SimpleJson
open Api.Shared

let sessionKey = "authenticated_session"

let saveSessionToLocalStorage (session: AuthenticatedSession) : Async<Result<unit, string>> = async {
    try
        let json = Json.serialize session
        Browser.WebStorage.localStorage.setItem (sessionKey, json)
        return Ok()
    with ex ->
        return Error $"Failed to save session to localStorage: {ex.Message}"
}

let loadSessionFromLocalStorage () : Async<Result<AuthenticatedSession, string>> = async {
    try
        let json = Browser.WebStorage.localStorage.getItem sessionKey

        if isNull json || json = "" then
            return Error "No session found in localStorage"
        else
            try
                let session = Json.parseAs<AuthenticatedSession> json
                return Ok session
            with ex ->
                return Error $"Failed to deserialize session: {ex.Message}"
    with ex ->
        return Error $"Failed to load session from localStorage: {ex.Message}"
}

let removeSessionFromLocalStorage () : Async<Result<unit, string>> = async {
    try
        Browser.WebStorage.localStorage.removeItem sessionKey
        return Ok()
    with ex ->
        return Error $"Failed to remove session from localStorage: {ex.Message}"
}

let unauthenticatedApi: UnauthenticatedApi =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Api.Shared.routingBuilder
    |> Remoting.buildProxy<UnauthenticatedApi>

Program.mkProgram
    (fun () -> Index.Shared.init loadSessionFromLocalStorage (Router.currentUrl ()))
    (Index.Shared.update unauthenticatedApi saveSessionToLocalStorage removeSessionFromLocalStorage)
    Index.WebClient.view
#if DEBUG
|> Program.withConsoleTrace
#endif
|> Program.withReactSynchronous "elmish-app"
|> Program.run