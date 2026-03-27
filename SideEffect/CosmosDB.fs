module CosmosDB.SideEffect

open System
open System.IO
open Microsoft.Azure.Cosmos

open System.Text.Json
open FSharp.SystemTextJson
open System.Text.Json.Serialization

open User.Shared

// ---------------------------------------------------------------------------
// Serializer
// ---------------------------------------------------------------------------

let jsonOptions: JsonSerializerOptions =
    JsonFSharpOptions.Default().ToJsonSerializerOptions()

type SystemTextJsonCosmosSerializer(opts: JsonSerializerOptions) =
    inherit CosmosSerializer()

    override _.FromStream<'T>(stream: Stream) =
        use reader = new StreamReader(stream)
        let json = reader.ReadToEnd()
        JsonSerializer.Deserialize<'T>(json, opts)

    override _.ToStream<'T>(input: 'T) =
        let json = JsonSerializer.Serialize(input, opts)
        let stream = new MemoryStream()
        use writer = new StreamWriter(stream, leaveOpen = true)
        writer.Write(json)
        writer.Flush()
        stream.Position <- 0L
        stream

// ---------------------------------------------------------------------------
// Client / database bootstrap
// ---------------------------------------------------------------------------

let cosmosClientResult =
    try
        let opts = CosmosClientOptions()
        opts.ConnectionMode <- ConnectionMode.Gateway
        opts.LimitToEndpoint <- true
        opts.Serializer <- SystemTextJsonCosmosSerializer jsonOptions

        opts.HttpClientFactory <-
            fun () ->
                let handler = new System.Net.Http.HttpClientHandler()
                handler.ServerCertificateCustomValidationCallback <- fun _ _ _ _ -> true
                handler.ClientCertificateOptions <- System.Net.Http.ClientCertificateOption.Manual
                new System.Net.Http.HttpClient(handler)

        new CosmosClient(Environment.SideEffect.environment.COSMOS_CONNECTION_STRING, opts)
        |> Ok
    with ex ->
        Error $"Failed to create Cosmos client: {ex.Message}"

let databaseResult =
    async {
        match cosmosClientResult with
        | Error err -> return Error err
        | Ok client ->
            try
                let! response = "javkstack-database" |> client.CreateDatabaseIfNotExistsAsync |> Async.AwaitTask
                return Ok response.Database
            with ex ->
                return Error $"Failed to create database: {ex.Message}"
    }
    |> Async.RunSynchronously

// ---------------------------------------------------------------------------
// Users container
// ---------------------------------------------------------------------------

module Users =

    let containerResult =
        async {
            match databaseResult with
            | Error err -> return Error err
            | Ok database ->
                try
                    let! response =
                        database.CreateContainerIfNotExistsAsync(
                            typeof<User>.Name,
                            $"/{nameof Unchecked.defaultof<User>.id}"
                        )
                        |> Async.AwaitTask

                    return Ok response.Container
                with ex ->
                    return Error $"Failed to get users container: {ex.Message}"
        }
        |> Async.RunSynchronously

    /// Add a new user. Fails if a user with the same `id` already exists.
    let addUser (user: User) =
        async {
            match containerResult with
            | Error err -> return Error err
            | Ok container ->
                try
                    let! response = container.CreateItemAsync(user, PartitionKey user.id) |> Async.AwaitTask
                    return Ok response.Resource
                with ex ->
                    return Error $"Failed to add user: {ex.Message}"
        }

    /// Get a user by their `id`.
    let getUserById (id: string) =
        async {
            match containerResult with
            | Error err -> return Error err
            | Ok container ->
                try
                    let! response = container.ReadItemAsync<User>(id, PartitionKey id) |> Async.AwaitTask

                    return Ok response.Resource
                with ex ->
                    match ex with
                    | :? CosmosException as cosmosEx when cosmosEx.StatusCode = System.Net.HttpStatusCode.NotFound ->
                        return Error $"User '{id}' not found"
                    | _ -> return Error $"Failed to get user: {ex.Message}"
        }

    /// Get a user by their username (case-sensitive query).
    let getUserByUsername (username: string) =
        async {
            match containerResult with
            | Error err -> return Error err
            | Ok container ->
                try
                    let query =
                        QueryDefinition "SELECT * FROM c WHERE c.username = @username"
                        |> fun q -> q.WithParameter("@username", username)

                    let iterator = container.GetItemQueryIterator<User> query

                    let! response = iterator.ReadNextAsync() |> Async.AwaitTask

                    match response |> Seq.tryHead with
                    | Some user -> return Ok user
                    | None -> return Error $"User '{username}' not found"
                with ex ->
                    return Error $"Failed to get user by username: {ex.Message}"
        }

    /// Return all users in the container.
    let getAllUsers () =
        async {
            match containerResult with
            | Error err -> return Error err
            | Ok container ->
                try
                    let iterator =
                        container.GetItemQueryIterator<User>(QueryDefinition "SELECT * FROM c")

                    let rec readAll acc =
                        async {
                            if iterator.HasMoreResults then
                                let! response = iterator.ReadNextAsync() |> Async.AwaitTask
                                return! readAll (acc @ (response |> Seq.toList))
                            else
                                return acc
                        }

                    let! users = readAll []
                    return users |> List.toArray |> Ok
                with ex ->
                    return Error $"Failed to get all users: {ex.Message}"
        }

    /// Replace a user document entirely. The `id` must match an existing record.
    let updateUser (user: User) =
        async {
            match containerResult with
            | Error err -> return Error err
            | Ok container ->
                try
                    let! response =
                        container.ReplaceItemAsync(user, user.id, PartitionKey user.id)
                        |> Async.AwaitTask

                    return Ok response.Resource
                with ex ->
                    return Error $"Failed to update user: {ex.Message}"
        }

    /// Delete a user by their `id`.
    let deleteUser (id: string) =
        async {
            match containerResult with
            | Error err -> return Error err
            | Ok container ->
                try
                    let! _ = container.DeleteItemAsync<User>(id, PartitionKey id) |> Async.AwaitTask

                    return Ok()
                with ex ->
                    return Error $"Failed to delete user: {ex.Message}"
        }
