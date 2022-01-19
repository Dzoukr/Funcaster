module Funcaster.Program

open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Funcaster.Storage

let configureServices (ctx:HostBuilderContext) (svcs:IServiceCollection) =
    let connString = ctx.Configuration.["PodcastStorage"]
    let episodes = EpisodesTable.createSafe connString
    let podcast = PodcastTable.createSafe connString
    let cdnSetup = CdnSetupTable.createSafe connString
    svcs.AddSingleton<EpisodesTable>(episodes) |> ignore
    svcs.AddSingleton<PodcastTable>(podcast) |> ignore
    svcs.AddSingleton<CdnSetupTable>(cdnSetup) |> ignore
    ()

[<EntryPoint>]
(HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices (configureServices))
    .Build()
    .Run()