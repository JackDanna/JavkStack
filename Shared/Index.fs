module Index.Shared

open Elmish

type Index = {
    title: string
}

type Msg =
    | NoOp

let init url =
    {
        title = "Hello World"
    }, Cmd.none

let update msg model =
    match msg with
    | NoOp -> model, Cmd.none