module Program

open Pulumi
open Pulumi.FSharp
open Pulumi.AzureNative.Resources
open Pulumi.AzureNative.CosmosDB
open Pulumi.AzureNative.App
open Pulumi.AzureNative.ContainerRegistry

open Environment.Shared

// Reminder: for non-interactive deploys (CI/CD), prefer a service principal.
// Pulumi Azure Native reads these environment variables:
// ARM_CLIENT_ID, ARM_CLIENT_SECRET, ARM_TENANT_ID, ARM_SUBSCRIPTION_ID

let infra () =
    let config = Config "azure-native"
    let location = config.Require "location"
    let acrName = config.Require "acrName"
    let appImageName = config.Get "appImageName" |> Option.ofObj |> Option.defaultValue "javk-stack-app"
    let appImageTag = config.Get "appImageTag" |> Option.ofObj |> Option.defaultValue "latest"

    // Create an Azure Resource Group
    let resourceGroup = ResourceGroup "resourceGroup"

    // Create an Azure Cosmos DB account
    let cosmosAccount =
        DatabaseAccount(
            "cosmos-account",
            DatabaseAccountArgs(
                ResourceGroupName = resourceGroup.Name,
                Location = input location,
                DatabaseAccountOfferType = input DatabaseAccountOfferType.Standard,
                EnableFreeTier = input true,
                Kind = inputUnion2Of2 DatabaseAccountKind.GlobalDocumentDB,
                Locations =
                    inputList [
                        input (Pulumi.AzureNative.CosmosDB.Inputs.LocationArgs(LocationName = input location))
                    ]
            )
        )

    let connectionStrings =
        ListDatabaseAccountConnectionStrings.Invoke(
            ListDatabaseAccountConnectionStringsInvokeArgs(
                ResourceGroupName = io resourceGroup.Name,
                AccountName = io cosmosAccount.Name
            )
        )

    let cosmosConnectionString =
        connectionStrings.Apply(fun c -> c.ConnectionStrings.[0].ConnectionString)

    let registry =
        Registry(
            "acr",
            RegistryArgs(
                RegistryName = input acrName,
                ResourceGroupName = io resourceGroup.Name,
                Location = input location,
                AdminUserEnabled = input true,
                Sku = input (Pulumi.AzureNative.ContainerRegistry.Inputs.SkuArgs(Name = SkuName.Basic))
            )
        )

    let registryCredentials =
        ListRegistryCredentials.Invoke(
            ListRegistryCredentialsInvokeArgs(
                ResourceGroupName = io resourceGroup.Name,
                RegistryName = io registry.Name
            )
        )

    // Container Apps Consumption plan includes a free monthly grant.
    // Keep replicas and resources minimal to stay within free usage.
    let containerEnv =
        ManagedEnvironment(
            "env",
            ManagedEnvironmentArgs(ResourceGroupName = io resourceGroup.Name, Location = input location)
        )

    let cosmosConnectionStringSecretName = "cosmos-connection-string"
    let acrPasswordSecretName = "acr-password"
    let containerApp =
        ContainerApp(
            "container-app",
            ContainerAppArgs(
                ResourceGroupName = io resourceGroup.Name,
                Location = input location,
                ManagedEnvironmentId = io containerEnv.Id,
                Configuration =
                    input (
                        Pulumi.AzureNative.App.Inputs.ConfigurationArgs(
                            Secrets =
                                inputList [
                                    input (
                                        Pulumi.AzureNative.App.Inputs.SecretArgs(
                                            Name = input acrPasswordSecretName,
                                            Value = io (registryCredentials.Apply(fun c -> c.Passwords.[0].Value))
                                        )
                                    )
                                    input (
                                        Pulumi.AzureNative.App.Inputs.SecretArgs(
                                            Name = input cosmosConnectionStringSecretName,
                                            Value = io cosmosConnectionString
                                        )
                                    )
                                ],
                            Registries =
                                inputList [
                                    input (
                                        Pulumi.AzureNative.App.Inputs.RegistryCredentialsArgs(
                                            Server = registry.LoginServer,
                                            Username = io (registryCredentials.Apply(fun c -> c.Username)),
                                            PasswordSecretRef = input acrPasswordSecretName
                                        )
                                    )
                                ],
                            Ingress =
                                input (
                                    Pulumi.AzureNative.App.Inputs.IngressArgs(
                                        External = input true,
                                        TargetPort = 
                                            (if appImageTag = "latest" then
                                                    input 80
                                                else
                                                    input 8080
                                            )
                                    )
                                )
                        )
                    ),
                Template =
                    input (
                        Pulumi.AzureNative.App.Inputs.TemplateArgs(
                            Containers =
                                inputList [
                                    input (
                                        Pulumi.AzureNative.App.Inputs.ContainerArgs(
                                            Name = input appImageName,
                                            Env =
                                                inputList [
                                                    input (
                                                        Pulumi.AzureNative.App.Inputs.EnvironmentVarArgs(
                                                            Name = input (nameof e.COSMOS_CONNECTION_STRING),
                                                            SecretRef = input cosmosConnectionStringSecretName
                                                        )
                                                    )
                                                ],
                                            Image =
                                                // Bootstrapping: appImageTag = "latest" means ACR is empty, use placeholder.
                                                // Once a real image has been pushed, the deploy command sets appImageTag
                                                // to the version string and re-runs pulumi up to switch to the real image.
                                                (if appImageTag = "latest" then
                                                    input "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest"
                                                else
                                                    io (
                                                        registry.LoginServer.Apply(fun server ->
                                                            $"{server}/{appImageName}:{appImageTag}")
                                                    )),
                                            Resources =
                                                input (
                                                    Pulumi.AzureNative.App.Inputs.ContainerResourcesArgs(
                                                        Cpu = input 0.25,
                                                        Memory = input "0.5Gi"
                                                    )
                                                )
                                        )
                                    )
                                ],
                            Scale =
                                input (
                                    Pulumi.AzureNative.App.Inputs.ScaleArgs(
                                        MinReplicas = input 0,
                                        MaxReplicas = input 1
                                    )
                                )
                        )
                    )
            )
        )

    // Export the cosmos account name
    dict [
        nameof e.COSMOS_CONNECTION_STRING, cosmosConnectionString :> obj
    ]

[<EntryPoint>]
let main _ = Deployment.run infra