module Index.WebClient

open Index.Shared

open Feliz
open Feliz.DaisyUI
open Feliz.Router

let navLink (label: string) (targetUrl: Url) (currentUrl: Url) =
    Html.li [
        Html.a [
            if currentUrl = targetUrl then
                prop.className "active"
            prop.onClick (fun _ -> urlToSegments targetUrl |> Router.navigate)
            prop.text label
        ]
    ]

let view (model: Index) dispatch =
    match model.UnauthenticatedOrAuthenticated with
    | Unauthenticated unauthenticatedPage ->
        match unauthenticatedPage with
        | LoginPage loginModel ->
            LoginPage.WebClient.view loginModel (fun msg -> dispatch (LoginPageMsg msg))

    | Authenticated _session ->
        Html.div [
            prop.className "min-h-screen bg-base-100"
            prop.children [
                Daisy.navbar [
                    prop.className "bg-neutral text-neutral-content shadow-lg"
                    prop.children [
                        Daisy.navbarStart [
                            Daisy.menu [
                                menu.horizontal
                                prop.children [
                                    navLink "Home" HomeUrl model.Url
                                    navLink "Rules" RulesUrl model.Url
                                ]
                            ]
                        ]
                        Daisy.navbarEnd [
                            Daisy.button.button [
                                button.ghost
                                prop.onClick (fun _ -> dispatch LogoutRequested)
                                prop.text "Logout"
                            ]
                        ]
                    ]
                ]

                Html.main [
                    prop.className "p-8"
                    prop.children [
                        match model.Url with
                        | HomeUrl | IndexUrl ->
                            Html.div [
                                Html.h1 [ prop.className "text-2xl font-bold"; prop.text "Home" ]
                                Html.p [ prop.className "mt-2 text-base-content/60"; prop.text "Welcome to JavkStack." ]
                            ]
                        | RulesUrl ->
                            Html.div [
                                Html.h1 [ prop.className "text-2xl font-bold"; prop.text "Rules" ]
                                Html.p [ prop.className "mt-2 text-base-content/60"; prop.text "No rules yet." ]
                            ]
                        | _ ->
                            Html.p [ prop.text "Page not found." ]
                    ]
                ]
            ]
        ]
    |> fun activePage ->
        React.router [
            router.onUrlChanged (parseUrl >> UrlChanged >> dispatch)
            router.children [ activePage ]
        ]
