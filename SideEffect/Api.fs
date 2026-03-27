module Api.SideEffect

open Api.Shared
open LoginPage.Shared

let unauthenticatedApiImplementation ctx : UnauthenticatedApi = {
    login =
        fun (login: Login) ->
            async {
                // TODO: implement real authentication
                return Error "Authentication not yet implemented"
            }
}

let apiImplementation ctx : Api =
    { sayBanana = fun () -> async { return "Banana!" } }