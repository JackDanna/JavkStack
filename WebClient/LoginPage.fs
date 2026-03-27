module LoginPage.WebClient

open LoginPage.Shared

open Feliz
open Feliz.DaisyUI

let renderLoginOutcome (loginAttempt: LoginAttempt) =
    match loginAttempt with
    | Resolved(Error(errorString: string)) ->
        Html.paragraph [
            prop.style [
                style.color.crimson
                style.padding 10
                style.maxWidth 300
                style.wordWrap.breakWord
                style.overflowWrap.breakWord
            ]
            prop.text errorString
        ]

    | Resolved(Ok _) ->
        Html.paragraph [
            prop.style [ style.color.green; style.padding 10 ]
            prop.text "User has successfully logged in"
        ]

    | _ -> Html.none

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
                                prop.type'.email
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
                                Daisy.button.button [
                                    prop.className "w-full"
                                    prop.onClick (fun _ -> LoginToServer |> dispatch)
                                    if model.LoginAttempt = InProgress then
                                        prop.children [ Html.span [ prop.className "loading loading-spinner" ] ]
                                    else
                                        prop.text "Login"
                                ]
                                |> List.singleton
                                |> prop.children
                            ]

                            renderLoginOutcome model.LoginAttempt
                        ]
                    ]
                ]
            ]
        ]
    ]
