module Index.Shared

open Elmish

type Index = { title: string }

type Msg = 
    | NoOp
    | StoredTokenLoaded of string

let init getData url =
    { title = "Hello World" }, Cmd.OfAsync.perform getData () StoredTokenLoaded

let update api msg model =
    match msg with
    | NoOp -> model, Cmd.none
    | StoredTokenLoaded token ->
        { model with title = token }, Cmd.none
