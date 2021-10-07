module Funcaster.Program

open System.Threading.Tasks
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Hosting
open Microsoft.Azure.Functions.Worker.Configuration

let host =
    HostBuilder()
        .ConfigureFunctionsWorkerDefaults()
        .Build()

host.Run()
