module Index.Shared

open LoginPage.Shared
open RegisterPage.Shared
open Elmish

// ---------- URL ---------------------------------------------------------

type Url =
    | IndexUrl
    | LoginPageUrl
    | RegisterPageUrl
    | NotFoundUrl
    | HomeUrl
    | RulesUrl

[<Literal>]
let LOGIN = "login"

[<Literal>]
let REGISTER = "register"

[<Literal>]
let NOT_FOUND = "not-found"

[<Literal>]
let HOME = "home"

[<Literal>]
let RULES = "rules"

let parseUrl segments =
    match segments with
    | [] -> HomeUrl
    | [ LOGIN ] -> LoginPageUrl
    | [ REGISTER ] -> RegisterPageUrl
    | [ HOME ] -> HomeUrl
    | [ RULES ] -> RulesUrl
    | _ -> NotFoundUrl

let urlToSegments url =
    match url with
    | IndexUrl -> [||]
    | LoginPageUrl -> [| LOGIN |]
    | RegisterPageUrl -> [| REGISTER |]
    | NotFoundUrl -> [| NOT_FOUND |]
    | HomeUrl -> [| HOME |]
    | RulesUrl -> [| RULES |]

// ---------- Page model --------------------------------------------------

type UnauthenticatedPage =
    | LoginPage of LoginPage
    | RegisterPage of RegisterPage

type UnauthenticatedOrAuthenticated =
    | Unauthenticated of UnauthenticatedPage
    | Authenticated of AuthenticatedSession

type Index = {
    Url: Url
    UnauthenticatedOrAuthenticated: UnauthenticatedOrAuthenticated
}

// ---------- Msg ---------------------------------------------------------

type Msg =
    | LoginPageMsg of LoginPage.Shared.Msg
    | RegisterPageMsg of RegisterPage.Shared.Msg
    | LoadStoredToken
    | StoredTokenLoaded of Result<AuthenticatedSession, string>
    | LogoutRequested
    | TokenSaved of Result<unit, string>
    | TokenRemoved of Result<unit, string>
    | UrlChanged of Url

// ---------- Init --------------------------------------------------------

let init loadToken (currentUrlSegments: string list) =
    let url = parseUrl currentUrlSegments

    let initialPage, pageCmd =
        match url with
        | RegisterPageUrl ->
            let regModel, regCmd = RegisterPage.Shared.init ()
            regModel |> RegisterPage |> Unauthenticated, Cmd.map RegisterPageMsg regCmd
        | _ ->
            let loginModel, loginCmd = LoginPage.Shared.init ()
            loginModel |> LoginPage |> Unauthenticated, Cmd.map LoginPageMsg loginCmd

    {
        Url = url
        UnauthenticatedOrAuthenticated = initialPage
    },
    Cmd.batch [
        pageCmd
        Cmd.OfAsync.perform loadToken () StoredTokenLoaded
    ]

// ---------- Update ------------------------------------------------------

let update
    (unauthenticatedApi: Api.Shared.UnauthenticatedApi)
    saveToken
    removeToken
    msg
    model
    =
    match model.UnauthenticatedOrAuthenticated with
    | Unauthenticated unauthenticatedPage ->
        match msg, unauthenticatedPage with
        | StoredTokenLoaded tokenResult, _ ->
            match tokenResult with
            | Ok session ->
                { model with UnauthenticatedOrAuthenticated = Authenticated session }
            | Error _ ->
                model
            , Cmd.none

        | LoginPageMsg(LoginResponse(Ok session)), _ ->
            { model with UnauthenticatedOrAuthenticated = Authenticated session },
            Cmd.OfAsync.perform saveToken session TokenSaved

        | LoginPageMsg loginMsg, LoginPage loginModel ->
            let updatedModel, cmd =
                LoginPage.Shared.update unauthenticatedApi.login loginMsg loginModel

            {
                model with
                    UnauthenticatedOrAuthenticated = updatedModel |> LoginPage |> Unauthenticated
            },
            Cmd.map LoginPageMsg cmd

        | RegisterPageMsg registerMsg, RegisterPage registerModel ->
            let updatedModel, cmd =
                RegisterPage.Shared.update unauthenticatedApi.register registerMsg registerModel

            {
                model with
                    UnauthenticatedOrAuthenticated = updatedModel |> RegisterPage |> Unauthenticated
            },
            Cmd.map RegisterPageMsg cmd

        | UrlChanged url, _ ->
            let updatedModel = { model with Url = url }

            match url with
            | LoginPageUrl ->
                let loginModel, loginCmd = LoginPage.Shared.init ()

                { updatedModel with UnauthenticatedOrAuthenticated = loginModel |> LoginPage |> Unauthenticated },
                Cmd.map LoginPageMsg loginCmd
            | RegisterPageUrl ->
                let regModel, regCmd = RegisterPage.Shared.init ()

                { updatedModel with UnauthenticatedOrAuthenticated = regModel |> RegisterPage |> Unauthenticated },
                Cmd.map RegisterPageMsg regCmd
            | _ -> updatedModel, Cmd.none

        | _ -> model, Cmd.none

    | Authenticated _session ->
        match msg with
        | LogoutRequested ->
            let loginModel, loginCmd = LoginPage.Shared.init ()

            {
                model with
                    UnauthenticatedOrAuthenticated = loginModel |> LoginPage |> Unauthenticated
            },
            Cmd.batch [
                Cmd.map LoginPageMsg loginCmd
                Cmd.OfAsync.perform removeToken () TokenRemoved
            ]

        | TokenSaved(Ok _) -> model, Cmd.none
        | TokenSaved(Error _) -> model, Cmd.none
        | TokenRemoved(Ok _) -> model, Cmd.none
        | TokenRemoved(Error _) -> model, Cmd.none

        | UrlChanged url -> { model with Url = url }, Cmd.none

        | _ -> model, Cmd.none
