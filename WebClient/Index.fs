module Index.WebClient

open System
open Index.Shared

open Elmish
open Feliz
open Feliz.DaisyUI
open Feliz.Router

let view (model: Index) dispatch =
    Html.text "Hello World"