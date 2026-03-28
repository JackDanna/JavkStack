module Environment.Shared

type Environment = {
    COSMOS_CONNECTION_STRING: string
    JWT_SECRET: string
}

// This reason we use this variable to reference the env variable names is so we can have the field name of the type and the env variable string always be the same
let e = Unchecked.defaultof<Environment>