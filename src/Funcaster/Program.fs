module Funcaster.Program

open System.Threading.Tasks
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Hosting
open Microsoft.Azure.Functions.Worker.Configuration

[<EntryPoint>]
HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .Build()
    .Run()
