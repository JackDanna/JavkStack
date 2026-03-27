module Api.SideEffect

open System
open Api.Shared
open LoginPage.Shared
open User.Shared
open CosmosDB.SideEffect

// ---------------------------------------------------------------------------
// Password hashing (PBKDF2 / SHA-256)
// ---------------------------------------------------------------------------

let private hashPassword (password: string) =
    let saltBytes = Array.zeroCreate 16
    use rng = Security.Cryptography.RandomNumberGenerator.Create()
    rng.GetBytes(saltBytes)
    let salt = Convert.ToBase64String(saltBytes)

    let hash =
        Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            100_000,
            Security.Cryptography.HashAlgorithmName.SHA256,
            32
        )
        |> Convert.ToBase64String

    $"{salt}:{hash}"

let private verifyPassword (password: string) (storedHash: string) =
    let parts = storedHash.Split(':')

    if parts.Length <> 2 then
        false
    else
        let saltBytes = Convert.FromBase64String(parts.[0])
        let expectedHash = parts.[1]

        let hash =
            Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
                password,
                saltBytes,
                100_000,
                Security.Cryptography.HashAlgorithmName.SHA256,
                32
            )
            |> Convert.ToBase64String

        hash = expectedHash

// ---------------------------------------------------------------------------
// API implementations
// ---------------------------------------------------------------------------

let unauthenticatedApiImplementation ctx : UnauthenticatedApi = {
    login =
        fun (login: Login) ->
            async {
                match! Users.getUserByUsername login.Username with
                | Error _ -> return Error "Invalid username or password"
                | Ok user ->
                    if verifyPassword login.Password user.passwordHash then
                        return
                            Ok {
                                Token = Guid.NewGuid().ToString()
                                RefreshToken = Guid.NewGuid().ToString()
                                Id = user.id
                            }
                    else
                        return Error "Invalid username or password"
            }

    register =
        fun (register: Register) ->
            async {
                match! Users.getUserByUsername register.Username with
                | Ok _ -> return Error $"Username '{register.Username}' is already taken"
                | Error _ ->
                    let newUser: User = {
                        id = Guid.NewGuid().ToString()
                        username = register.Username
                        passwordHash = hashPassword register.Password
                        email = register.Email
                        createdAt = DateTimeOffset.UtcNow
                    }

                    match! Users.addUser newUser with
                    | Ok _ -> return Ok()
                    | Error err -> return Error err
            }
}

let apiImplementation ctx : Api =
    { sayBanana = fun () -> async { return "Banana!" } }