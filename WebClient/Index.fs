module Index.WebClient

open Index.Shared

open Feliz
open Feliz.DaisyUI

let view (model: Index) dispatch =
    match model.UnauthenticatedOrAuthenticated with
    | Unauthenticated unauthenticatedPage ->
        match unauthenticatedPage with
        | LoginPage loginModel ->
            LoginPage.WebClient.view loginModel (fun msg -> dispatch (LoginPageMsg msg))

    | Authenticated _session ->
        Html.div [
            prop.className "flex items-center justify-center min-h-screen"
            prop.children [
                Html.div [
                    prop.className "text-center space-y-4"
                    prop.children [
                        Html.h1 [ prop.className "text-2xl font-bold"; prop.text "Welcome" ]
                        Daisy.button.button [
                            prop.onClick (fun _ -> dispatch LogoutRequested)
                            prop.text "Logout"
                        ]
                    ]
                ]
            ]
        ]
