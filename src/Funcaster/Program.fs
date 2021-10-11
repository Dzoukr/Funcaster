module Funcaster.Program

open System
open System.Threading.Tasks
open Azure.Storage.Blobs
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Hosting
open Microsoft.Azure.Functions.Worker.Configuration
open Microsoft.Extensions.DependencyInjection

let configureServices (ctx:HostBuilderContext) (svcs:IServiceCollection) =
    let connString = ctx.Configuration.["PodcastStorage"]
    let blobClient = BlobServiceClient(connString)
    svcs.AddSingleton<BlobServiceClient>(blobClient) |> ignore
    ()

[<EntryPoint>]
(HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices (configureServices))
    .Build()
    .Run()