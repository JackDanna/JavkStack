module Environment.Shared

type Environment = {
    COSMOS_CONNECTION_STRING: string
}

let e = Unchecked.defaultof<Environment>