module Funcaster.Program

open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Funcaster.Storage

let configureServices (ctx:HostBuilderContext) (svcs:IServiceCollection) =
    let connString = ctx.Configuration.["PodcastStorage"]
    let episodes = EpisodesTable.create connString
    let podcast = PodcastTable.create connString
    svcs.AddSingleton<EpisodesTable>(episodes) |> ignore
    svcs.AddSingleton<PodcastTable>(podcast) |> ignore
    ()

[<EntryPoint>]
(HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices (configureServices))
    .Build()
    .Run()