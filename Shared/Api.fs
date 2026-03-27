module Api.Shared

open LoginPage.Shared

let routingBuilder = sprintf "/api/%s/%s"

type UnauthenticatedApi = {
    login: Login -> Async<Result<AuthResponse, string>>
    register: Register -> Async<Result<unit, string>>
}

type Api = {
    sayBanana: unit -> Async<string>
}