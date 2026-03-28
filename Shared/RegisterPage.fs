module RegisterPage.Shared

open Elmish
open Deferred.Shared

type Register = {
    Username: string
    Password: string
    Email: string
}

type RegisterPage = {
    Register: Register
    RegisterAttempt: Deferred<Result<unit, string>>
}

type Msg =
    | UsernameChanged of string
    | PasswordChanged of string
    | EmailChanged of string
    | RegisterToServer
    | RegisterResponse of Result<unit, string>

let init () =
    {
        Register = { Username = ""; Password = ""; Email = "" }
        RegisterAttempt = NotStarted
    },
    Cmd.none

let update register msg model =
    match msg with
    | UsernameChanged u ->
        { model with Register = { model.Register with Username = u } }, Cmd.none
    | PasswordChanged p ->
        { model with Register = { model.Register with Password = p } }, Cmd.none
    | EmailChanged e ->
        { model with Register = { model.Register with Email = e } }, Cmd.none
    | RegisterToServer ->
        { model with RegisterAttempt = InProgress },
        Cmd.OfAsync.perform register model.Register RegisterResponse
    | RegisterResponse result ->
        { model with RegisterAttempt = Resolved result }, Cmd.none
