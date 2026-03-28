module LoginPage.Shared

open Elmish

type JWT = string

type AuthResponse = {
    Token: string
    RefreshToken: string
    Id: string
}

type AuthenticatedSession = {
    Token: JWT
    RefreshToken: string
    UserId: string
}

type Login = { Username: string; Password: string }

type Register = {
    Username: string
    Password: string
    Email: string
}

type LoginAttempt =
    | NotStarted
    | InProgress
    | Resolved of Result<AuthenticatedSession, string>

type RegisterAttempt =
    | RegisterNotStarted
    | RegisterInProgress
    | RegisterResolved of Result<unit, string>

type LoginPage = {
    Login: Login
    LoginAttempt: LoginAttempt
    Register: Register
    RegisterAttempt: RegisterAttempt
    IsRegistering: bool
}

type Msg =
    | UsernameChanged of string
    | PasswordChanged of string
    | LoginToServer
    | LoginResponse of Result<AuthenticatedSession, string>
    | SwitchToRegister
    | SwitchToLogin
    | RegisterUsernameChanged of string
    | RegisterPasswordChanged of string
    | RegisterEmailChanged of string
    | RegisterToServer
    | RegisterResponse of Result<unit, string>

let init () =
    {
        Login = { Username = ""; Password = "" }
        LoginAttempt = NotStarted
        Register = { Username = ""; Password = ""; Email = "" }
        RegisterAttempt = RegisterNotStarted
        IsRegistering = false
    },
    Cmd.none

let update authenticate register msg model =
    match msg with
    | UsernameChanged newUsername ->
        { model with Login = { model.Login with Username = newUsername } }, Cmd.none

    | PasswordChanged newPassword ->
        { model with Login = { model.Login with Password = newPassword } }, Cmd.none

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

    | LoginResponse loginResult -> { model with LoginAttempt = Resolved loginResult }, Cmd.none

    | SwitchToRegister -> { model with IsRegistering = true; RegisterAttempt = RegisterNotStarted }, Cmd.none

    | SwitchToLogin -> { model with IsRegistering = false; LoginAttempt = NotStarted }, Cmd.none

    | RegisterUsernameChanged u ->
        { model with Register = { model.Register with Username = u } }, Cmd.none

    | RegisterPasswordChanged p ->
        { model with Register = { model.Register with Password = p } }, Cmd.none

    | RegisterEmailChanged e ->
        { model with Register = { model.Register with Email = e } }, Cmd.none

    | RegisterToServer ->
        { model with RegisterAttempt = RegisterInProgress },
        Cmd.OfAsync.perform register model.Register RegisterResponse

    | RegisterResponse(Ok()) ->
        { model with
            RegisterAttempt = RegisterResolved(Ok())
            IsRegistering = false },
        Cmd.none

    | RegisterResponse(Error e) ->
        { model with RegisterAttempt = RegisterResolved(Error e) }, Cmd.none

let issuerString = "JavkStack"
let audienceString = "JavkStack"
