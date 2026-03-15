module CloudInfrastructure.Stack

open Pulumi
open Pulumi.FSharp
open Pulumi.AzureNative.Resources
open Pulumi.AzureNative.DocumentDB

let eastus = "eastus"

let resources () =
    // The config is Pulumi's way of reading values from the stack's config file, Pulumi.prod.yaml
    let config = Config "azure-native"

    let provider =
        Pulumi.AzureNative.Provider(
            "azure-provider",
            Pulumi.AzureNative.ProviderArgs(SubscriptionId = input (config.Require "subscriptionId"))
        )

    let resourceGroup =
        ResourceGroup(
            "javkstack-rg",
            ResourceGroupArgs(
                Location = input eastus
            ),
            CustomResourceOptions(Provider = provider)
        )

    let cosmosAccount =
        DatabaseAccount(
            "javkstack-cosmos",
            DatabaseAccountArgs(
                ResourceGroupName = io resourceGroup.Name,
                Location = input eastus,
                DatabaseAccountOfferType = input DatabaseAccountOfferType.Standard,
                EnableFreeTier = input true,
                Locations = inputList [
                    input (Pulumi.AzureNative.DocumentDB.Inputs.LocationArgs(LocationName = input eastus))
                ],
                Kind = inputUnion2Of2 DatabaseAccountKind.GlobalDocumentDB
            ),
            CustomResourceOptions(Provider = provider)
        )

    let connectionStrings =
        ListDatabaseAccountConnectionStrings.Invoke(
            ListDatabaseAccountConnectionStringsInvokeArgs(
                ResourceGroupName = io resourceGroup.Name,
                AccountName = io cosmosAccount.Name
            )
        )

    dict [
        "resourceGroupName", resourceGroup.Name :> obj
        "cosmosAccountName", cosmosAccount.Name :> obj
        "cosmosConnectionString", connectionStrings.Apply(fun c -> c.ConnectionStrings.[0].ConnectionString) :> obj
    ]
