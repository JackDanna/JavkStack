module Api.SideEffect

open Api.Shared
open LoginPage.SideEffect
open RegisterPage.SideEffect

// ---------------------------------------------------------------------------
// API implementations
// ---------------------------------------------------------------------------

let unauthenticatedApiImplementation ctx : UnauthenticatedApi = {
    login = login
    register = register
}

let apiImplementation ctx : AuthenticatedApi =
    { sayBanana = fun () -> async.Return "Banana!" }