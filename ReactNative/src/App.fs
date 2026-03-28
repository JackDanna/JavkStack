module App

open Fable.ReactNative.Navigation

module R = Fable.ReactNative.Helpers
module P = Fable.ReactNative.Props
open Fable.ReactNative.Props

let private stack = Stack.CreateStackNavigator()

let homePage (nav: Types.INavigation<_>) =
    R.view [
        P.ViewProperties.Style [
            P.FlexStyle.Flex 1.
            P.FlexStyle.JustifyContent JustifyContent.Center
            P.FlexStyle.AlignItems ItemAlignment.Center
        ]
    ] [
        R.text [] "This is the home screen"

        R.touchableOpacity [
            OnPress(fun _ -> nav.navigation.push "counter")
        ] [
            R.text [
                P.TextProperties.Style [
                    P.FlexStyle.MarginTop (R.pct 5.)
                ]
            ] "Open counter screen"
        ]
    ]

let render () =
    navigationContainer [] [
        stack.Navigator.navigator [
            Stack.NavigatorProps.InitialRouteName "home"
        ] [
            stack.Screen.screen "home" homePage [] []
            stack.Screen.screen "counter" Counter.counter [
                Stack.ScreenProps.InitialParams ({ Initial = None }: Counter.CounterProps)
            ] []
        ]
    ]

Helpers.registerApp "ReactNative" (render ())
