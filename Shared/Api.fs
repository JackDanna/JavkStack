module Api.Shared

open System

type RefreshTokenRequest = { UserId: int; RefreshToken: string }

let routingBuilder = sprintf "/api/%s/%s"

type Api = {
    sayBanana: unit -> Async<string>
}