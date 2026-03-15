module CloudInfrastructure.Stack

open Pulumi
open Pulumi.FSharp
open Pulumi.AzureNative.Resources
open Pulumi.AzureNative.DocumentDB
open Pulumi.AzureNative.OperationalInsights
open Pulumi.AzureNative.App

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

    // Log Analytics Workspace required by Container App Environment
    let logAnalytics =
        Workspace(
            "javkstack-logs",
            WorkspaceArgs(
                ResourceGroupName = io resourceGroup.Name,
                Location = input eastus,
                Sku = input (Pulumi.AzureNative.OperationalInsights.Inputs.WorkspaceSkuArgs(Name = inputUnion2Of2 WorkspaceSkuNameEnum.PerGB2018)),
                RetentionInDays = input 30
            ),
            CustomResourceOptions(Provider = provider)
        )

    let logAnalyticsKeys =
        GetSharedKeys.Invoke(
            GetSharedKeysInvokeArgs(
                ResourceGroupName = io resourceGroup.Name,
                WorkspaceName = io logAnalytics.Name
            )
        )

    // Consumption plan - includes free monthly allowance
    let containerEnv =
        ManagedEnvironment(
            "javkstack-env",
            ManagedEnvironmentArgs(
                ResourceGroupName = io resourceGroup.Name,
                Location = input eastus,
                AppLogsConfiguration = input (
                    Pulumi.AzureNative.App.Inputs.AppLogsConfigurationArgs(
                        Destination = input "log-analytics",
                        LogAnalyticsConfiguration = input (
                            Pulumi.AzureNative.App.Inputs.LogAnalyticsConfigurationArgs(
                                CustomerId = io logAnalytics.CustomerId,
                                SharedKey = io (logAnalyticsKeys.Apply(fun k -> k.PrimarySharedKey))
                            )
                        )
                    )
                )
            ),
            CustomResourceOptions(Provider = provider)
        )

    let containerApp =
        ContainerApp(
            "javkstack-app",
            ContainerAppArgs(
                ResourceGroupName = io resourceGroup.Name,
                Location = input eastus,
                ManagedEnvironmentId = io containerEnv.Id,
                Configuration = input (
                    Pulumi.AzureNative.App.Inputs.ConfigurationArgs(
                        Ingress = input (
                            Pulumi.AzureNative.App.Inputs.IngressArgs(
                                External = input true,
                                TargetPort = input 80
                            )
                        )
                    )
                ),
                Template = input (
                    Pulumi.AzureNative.App.Inputs.TemplateArgs(
                        Containers = inputList [
                            input (
                                Pulumi.AzureNative.App.Inputs.ContainerArgs(
                                    Name = input "javkstack-server",
                                    Image = input "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest",
                                    Resources = input (
                                        Pulumi.AzureNative.App.Inputs.ContainerResourcesArgs(
                                            Cpu = input 0.25,
                                            Memory = input "0.5Gi"
                                        )
                                    )
                                )
                            )
                        ],
                        Scale = input (
                            Pulumi.AzureNative.App.Inputs.ScaleArgs(
                                MinReplicas = input 0,
                                MaxReplicas = input 1
                            )
                        )
                    )
                )
            ),
            CustomResourceOptions(Provider = provider)
        )

    dict [
        "RESOURCE_GROUP_NAME", resourceGroup.Name :> obj
        "COSMOS_ACCOUNT_NAME", cosmosAccount.Name :> obj
        "COSMOS_CONNECTION_STRING", connectionStrings.Apply(fun c -> c.ConnectionStrings.[0].ConnectionString) :> obj
        "CONTAINER_APP_URL", containerApp.LatestRevisionFqdn :> obj
    ]
