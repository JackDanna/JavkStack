module LoginPage.WebClient

open LoginPage.Shared
open Deferred.Shared

open Feliz
open Feliz.DaisyUI
open Feliz.Router

let view (model: LoginPage) dispatch =
    Html.div [
        prop.className "flex items-center justify-center min-h-screen"
        prop.children [
            Daisy.card [
                color.bgBase100
                prop.children [
                    Daisy.cardBody [
                        prop.className "items-center text-center space-y-5"
                        prop.children [
                            Daisy.cardTitle "Login"

                            Daisy.input [
                                prop.placeholder "Username"
                                prop.valueOrDefault model.Login.Username
                                prop.onChange (UsernameChanged >> dispatch)
                                prop.onKeyDown (fun e ->
                                    if e.key = "Enter" then
                                        LoginToServer |> dispatch)
                            ]

                            Daisy.input [
                                prop.placeholder "Password"
                                prop.type'.password
                                prop.valueOrDefault model.Login.Password
                                prop.onChange (PasswordChanged >> dispatch)
                                prop.onKeyDown (fun e ->
                                    if e.key = "Enter" then
                                        LoginToServer |> dispatch)
                            ]

                            Daisy.cardActions [
                                prop.className "w-full"
                                prop.children [
                                    Daisy.button.button [
                                        prop.className "w-full"
                                        prop.onClick (fun _ -> LoginToServer |> dispatch)
                                        if model.LoginAttempt = InProgress then
                                            prop.children [ Html.span [ prop.className "loading loading-spinner" ] ]
                                        else
                                            prop.text "Login"
                                    ]
                                ]
                            ]

                            match model.LoginAttempt with
                            | Resolved(Error msg) ->
                                Html.paragraph [
                                    prop.style [
                                        style.color.crimson
                                        style.padding 10
                                        style.maxWidth 300
                                        style.wordWrap.breakWord
                                        style.overflowWrap.breakWord
                                    ]
                                    prop.text msg
                                ]
                            | _ -> Html.none

                            Html.p [
                                prop.className "text-sm"
                                prop.children [
                                    Html.span [ prop.text "Don't have an account? " ]
                                    Html.a [
                                        prop.className "link link-primary"
                                        prop.onClick (fun _ -> Router.navigate [| "register" |])
                                        prop.text "Register"
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]
