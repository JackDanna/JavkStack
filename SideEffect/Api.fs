module Api.SideEffect

open Api.Shared
open LoginPage.SideEffect

// ---------------------------------------------------------------------------
// API implementations
// ---------------------------------------------------------------------------

let unauthenticatedApiImplementation ctx : UnauthenticatedApi = {
    login = login
    register = register
}

let apiImplementation ctx : Api =
    { sayBanana = fun () -> async.Return "Banana!" }