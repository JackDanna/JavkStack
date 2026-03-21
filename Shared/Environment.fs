module Environment.Shared

type Environment = {
    COSMOS_CONNECTION_STRING: string
}

let e = Unchecked.defaultof<Environment>

let string_COSMOS_CONNECTION_STRING = nameof e.COSMOS_CONNECTION_STRING