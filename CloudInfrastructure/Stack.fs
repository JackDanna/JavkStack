module CloudInfrastructure.Stack

open Pulumi.FSharp
open Pulumi.AzureNative.Resources

let resources () =
    let resourceGroup =
        ResourceGroup(
            "javkstack-rg",
            ResourceGroupArgs(Location = input "eastus")
        )

    dict [ 
        "resourceGroupName", 
        resourceGroup.Name 
        :> obj 
    ]
