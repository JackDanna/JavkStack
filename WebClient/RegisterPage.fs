module RegisterPage.WebClient

open RegisterPage.Shared
open Deferred.Shared

open Feliz
open Feliz.DaisyUI
open Feliz.Router

let view (model: RegisterPage) dispatch =
    Html.div [
        prop.className "flex items-center justify-center min-h-screen"
        prop.children [
            Daisy.card [
                color.bgBase100
                prop.children [
                    Daisy.cardBody [
                        prop.className "items-center text-center space-y-5"
                        prop.children [
                            match model.RegisterAttempt with
                            | Resolved(Ok()) ->
                                Daisy.cardTitle "Account Created"
                                Html.p [ prop.className "text-success"; prop.text "Registration successful!" ]
                                Daisy.button.button [
                                    prop.className "w-full"
                                    prop.onClick (fun _ -> Router.navigate [| "login" |])
                                    prop.text "Go to Login"
                                ]
                            | _ ->
                                Daisy.cardTitle "Create Account"

                                Daisy.input [
                                    prop.placeholder "Username"
                                    prop.valueOrDefault model.Register.Username
                                    prop.onChange (UsernameChanged >> dispatch)
                                    prop.onKeyDown (fun e ->
                                        if e.key = "Enter" then
                                            RegisterToServer |> dispatch)
                                ]

                                Daisy.input [
                                    prop.placeholder "Email"
                                    prop.type'.email
                                    prop.valueOrDefault model.Register.Email
                                    prop.onChange (EmailChanged >> dispatch)
                                    prop.onKeyDown (fun e ->
                                        if e.key = "Enter" then
                                            RegisterToServer |> dispatch)
                                ]

                                Daisy.input [
                                    prop.placeholder "Password"
                                    prop.type'.password
                                    prop.valueOrDefault model.Register.Password
                                    prop.onChange (PasswordChanged >> dispatch)
                                    prop.onKeyDown (fun e ->
                                        if e.key = "Enter" then
                                            RegisterToServer |> dispatch)
                                ]

                                Daisy.cardActions [
                                    prop.className "w-full"
                                    prop.children [
                                        Daisy.button.button [
                                            prop.className "w-full"
                                            prop.onClick (fun _ -> RegisterToServer |> dispatch)
                                            if model.RegisterAttempt = InProgress then
                                                prop.children [ Html.span [ prop.className "loading loading-spinner" ] ]
                                            else
                                                prop.text "Create Account"
                                        ]
                                    ]
                                ]

                                match model.RegisterAttempt with
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
                                        Html.span [ prop.text "Already have an account? " ]
                                        Html.a [
                                            prop.className "link link-primary"
                                            prop.onClick (fun _ -> Router.navigate [| "login" |])
                                            prop.text "Login"
                                        ]
                                    ]
                                ]
                        ]
                    ]
                ]
            ]
        ]
    ]
