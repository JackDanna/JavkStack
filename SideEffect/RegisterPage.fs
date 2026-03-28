module RegisterPage.SideEffect

open System
open RegisterPage.Shared
open User.Shared
open CosmosDB.SideEffect
open LoginPage.SideEffect

let register (register: Register) =
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
