module Server

open Saturn
open Giraffe
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.DataProtection
open Microsoft.AspNetCore.Builder
open System
open System.IO
open System.Net.Http
open System.Threading
open System.Threading.Tasks

open Api.SideEffect

let configureDataProtection (services: IServiceCollection) =
    // Configure DataProtection for container environments
    let dataProtectionBuilder = services.AddDataProtection()

    // Try to use environment variable, fallback to /tmp/dpkeys
    let keysPath =
        match Environment.GetEnvironmentVariable "DataProtection__PersistKeysToFileSystem" with
        | null
        | "" -> "/tmp/dpkeys"
        | path -> path

    // Ensure directory exists
    if not (Directory.Exists keysPath) then
        Directory.CreateDirectory keysPath |> ignore

    dataProtectionBuilder.PersistKeysToFileSystem(DirectoryInfo(keysPath)) |> ignore
    dataProtectionBuilder.SetDefaultKeyLifetime(TimeSpan.FromDays 90) |> ignore
    services

let configureStaticFileOptions (services: IServiceCollection) =
    services.Configure<StaticFileOptions>(fun (options: StaticFileOptions) ->
        options.OnPrepareResponse <-
            fun ctx ->
                let requestPath = ctx.Context.Request.Path.Value.ToLower()
                let fileName = ctx.File.Name.ToLower()
                let response = ctx.Context.Response

                if fileName.EndsWith ".html" || fileName.EndsWith "index.html" then
                    // HTML files: Disable caching completely to ensure users always get latest version
                    // Problem: ETags rely on file modification time, which may not update in Docker deployments
                    // Solution: no-cache forces revalidation, no-store prevents any caching
                    response.Headers.["Cache-Control"] <-
                        Microsoft.Extensions.Primitives.StringValues("no-cache, no-store, must-revalidate")

                    response.Headers.["Pragma"] <- Microsoft.Extensions.Primitives.StringValues("no-cache")
                    response.Headers.["Expires"] <- Microsoft.Extensions.Primitives.StringValues("0")
                elif
                    requestPath.Contains "/assets/"
                    && (fileName.EndsWith ".js"
                        || fileName.EndsWith ".css"
                        || fileName.EndsWith ".png"
                        || fileName.EndsWith ".jpg"
                        || fileName.EndsWith ".svg")
                then
                    // Hashed assets: Long-term cache with immutable flag
                    // Since filename contains hash, content never changes - no revalidation needed
                    response.Headers.["Cache-Control"] <-
                        Microsoft.Extensions.Primitives.StringValues("public, max-age=31536000, immutable")
                else
                    // Other static files: Moderate cache with revalidation
                    response.Headers.["Cache-Control"] <-
                        Microsoft.Extensions.Primitives.StringValues("public, max-age=3600, must-revalidate"))
    |> ignore

    services

/// Combined router with Fable.Remoting API
let webApp =
    let mainHandler =
        Remoting.createApi ()
        |> Remoting.withRouteBuilder Api.Shared.routingBuilder
        |> Remoting.fromContext apiImplementation
        |> Remoting.buildHttpHandler

    choose [ mainHandler ]

let app = application {
    use_router webApp
    memory_cache
    use_static "public"
    use_gzip
    service_config configureDataProtection
    service_config configureStaticFileOptions
}

[<EntryPoint>]
let main _ =
    run app
    0