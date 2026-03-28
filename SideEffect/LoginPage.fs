module LoginPage.SideEffect

open System
open System.Security.Claims
open System.IdentityModel.Tokens.Jwt
open Microsoft.IdentityModel.Tokens
open LoginPage.Shared
open User.Shared
open CosmosDB.SideEffect
open Environment.SideEffect

// ---------------------------------------------------------------------------
// JWT generation
// ---------------------------------------------------------------------------

let generateAccessToken (user: User) =
    JwtSecurityToken(
        issuer = issuerString,
        audience = audienceString,
        claims = [|
            Claim(JwtRegisteredClaimNames.Sub, user.id)
            Claim(JwtRegisteredClaimNames.UniqueName, user.username)
        |],
        expires = DateTime.UtcNow.AddHours 1.0,
        signingCredentials = SigningCredentials(SymmetricSecurityKey(Text.Encoding.UTF8.GetBytes environment.JWT_SECRET), SecurityAlgorithms.HmacSha256)
    )
    |> JwtSecurityTokenHandler().WriteToken

// ---------------------------------------------------------------------------
// Password hashing (PBKDF2 / SHA-256)
// ---------------------------------------------------------------------------

let hashPassword (password: string) =
    let saltBytes = Array.zeroCreate 16
    use rng = Security.Cryptography.RandomNumberGenerator.Create()
    rng.GetBytes saltBytes

    Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
        password,
        saltBytes,
        100_000,
        Security.Cryptography.HashAlgorithmName.SHA256,
        32
    )
    |> Convert.ToBase64String
    |> sprintf "%s:%s" (Convert.ToBase64String saltBytes)

let verifyPassword (password: string) (storedHash: string) =
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

let login (login: Login) =
    async {
        match! Users.getUserByUsername login.Username with
        | Error _ -> return Error "Invalid username or password"
        | Ok user ->
            if verifyPassword login.Password user.passwordHash then
                return
                    Ok {
                        Token = generateAccessToken user
                        RefreshToken = Guid.NewGuid().ToString()
                        UserId = user.id
                    }
            else
                return Error "Invalid username or password"
    }

