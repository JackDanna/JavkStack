module Index.Shared

open LoginPage.Shared
open Elmish

// ---------- URL ---------------------------------------------------------

type Url =
    | IndexUrl
    | LoginPageUrl
    | NotFoundUrl

[<Literal>]
let LOGIN = "login"

[<Literal>]
let NOT_FOUND = "not-found"

let parseUrl segments =
    match segments with
    | [] -> IndexUrl
    | [ LOGIN ] -> LoginPageUrl
    | _ -> NotFoundUrl

let urlToSegments url =
    match url with
    | IndexUrl -> [||]
    | LoginPageUrl -> [| LOGIN |]
    | NotFoundUrl -> [| NOT_FOUND |]

// ---------- Page model --------------------------------------------------

type UnauthenticatedPage = LoginPage of LoginPage

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
    | LoadStoredToken
    | StoredTokenLoaded of Result<AuthenticatedSession, string>
    | LogoutRequested
    | TokenSaved of Result<unit, string>
    | TokenRemoved of Result<unit, string>
    | UrlChanged of Url

// ---------- Init --------------------------------------------------------

let init loadToken (currentUrlSegments: string list) =
    let loginModel, loginCmd = LoginPage.Shared.init ()

    {
        Url = parseUrl currentUrlSegments
        UnauthenticatedOrAuthenticated = loginModel |> LoginPage |> Unauthenticated
    },
    Cmd.batch [
        Cmd.map LoginPageMsg loginCmd
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

        | StoredTokenLoaded(Ok session), _ ->
            { model with UnauthenticatedOrAuthenticated = Authenticated session }, Cmd.none

        | StoredTokenLoaded(Error _), _ -> model, Cmd.none

        | LoginPageMsg(LoginResponse(Ok session)), _ ->
            { model with UnauthenticatedOrAuthenticated = Authenticated session },
            Cmd.OfAsync.perform saveToken session TokenSaved

        | LoginPageMsg loginMsg, LoginPage loginModel ->
            let updatedModel, cmd =
                LoginPage.Shared.update unauthenticatedApi.login unauthenticatedApi.register loginMsg loginModel

            {
                model with
                    UnauthenticatedOrAuthenticated = updatedModel |> LoginPage |> Unauthenticated
            },
            Cmd.map LoginPageMsg cmd

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
