module Deferred.Shared

type Deferred<'t> =
    | NotStarted
    | InProgress
    | Resolved of 't
