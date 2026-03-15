module CloudInfrastructure.Stack

open Pulumi
open Pulumi.FSharp
open Pulumi.AzureNative.Resources

let resources () =
    let config = Config "azure-native"

    let provider =
        Pulumi.AzureNative.Provider(
            "azure-provider",
            Pulumi.AzureNative.ProviderArgs(SubscriptionId = input (config.Require "subscriptionId"))
        )

    let resourceGroup =
        ResourceGroup(
            "javkstack-rg",
            ResourceGroupArgs(Location = input "eastus"),
            CustomResourceOptions(Provider = provider)
        )

    dict [ 
        "resourceGroupName", 
        resourceGroup.Name 
        :> obj 
    ]
