module Environment.SideEffect

open Environment.Shared

let environment = {
    COSMOS_CONNECTION_STRING = System.Environment.GetEnvironmentVariable "COSMOS_CONNECTION_STRING"
}