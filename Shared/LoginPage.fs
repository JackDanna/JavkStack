module LoginPage.Shared

open Elmish

type JWT = string

type AuthResponse = {
    Token: string
    RefreshToken: string
    Id: int
}

type AuthenticatedSession = {
    Token: JWT
    RefreshToken: string
    UserId: int
}

type Login = { Username: string; Password: string }

type LoginAttempt =
    | NotStarted
    | InProgress
    | Resolved of Result<AuthenticatedSession, string>

type LoginPage = {
    Login: Login
    LoginAttempt: LoginAttempt
}

type Msg =
    | UsernameChanged of string
    | PasswordChanged of string
    | LoginToServer
    | LoginResponse of Result<AuthenticatedSession, string>

let init () =
    {
        Login = { Username = ""; Password = "" }
        LoginAttempt = NotStarted
    },
    Cmd.none

let update authenticate msg model =
    match msg with
    | UsernameChanged newUsername ->
        {
            model with
                Login = { model.Login with Username = newUsername }
        },
        Cmd.none

    | PasswordChanged newPassword ->
        {
            model with
                Login = { model.Login with Password = newPassword }
        },
        Cmd.none

    | LoginToServer ->
        { model with LoginAttempt = InProgress },
        Cmd.OfAsync.perform authenticate model.Login (fun result ->
            LoginResponse(
                result
                |> Result.map (fun (authResponse: AuthResponse) -> {
                    Token = authResponse.Token
                    RefreshToken = authResponse.RefreshToken
                    UserId = authResponse.Id
                })
            ))

    | LoginResponse loginResult ->
        { model with LoginAttempt = Resolved loginResult }, Cmd.none
