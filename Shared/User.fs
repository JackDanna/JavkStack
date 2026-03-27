module User.Shared

open System

/// A user stored in Cosmos DB.
/// `id` is the partition key and acts as the primary identifier (e.g. a GUID string).
type User = {
    id: string
    username: string
    passwordHash: string
    email: string
    createdAt: DateTimeOffset
}
