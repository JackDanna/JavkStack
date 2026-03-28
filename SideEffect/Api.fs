module Api.SideEffect

open Api.Shared
open LoginPage.SideEffect
open RegisterPage.SideEffect

let unauthenticatedApiImplementation ctx : UnauthenticatedApi = {
    login = login
    register = register
}

let authenticatedApiImplementation ctx : AuthenticatedApi =
    { sayBanana = fun () -> async.Return "Banana!" }