module Api.SideEffect

open Api.Shared

let apiImplementation ctx : Api =
    { sayBanana = fun () -> async { return "Banana!" } }