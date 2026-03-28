module Api.Shared

open LoginPage.Shared
open RegisterPage.Shared

let routingBuilder = sprintf "/api/%s/%s"

type UnauthenticatedApi = {
    login: Login -> Async<Result<AuthenticatedSession, string>>
    register: Register -> Async<Result<unit, string>>
}

type AuthenticatedApi = {
    sayBanana: unit -> Async<string>
}