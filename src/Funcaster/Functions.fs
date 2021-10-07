namespace Funcaster.Functions

open System.Collections.Generic
open System.Net
open System.Threading.Tasks
open Microsoft.Azure.Functions.Worker
open Microsoft.Azure.Functions.Worker.Http
open Microsoft.Extensions.Logging


type Functions() =

    [<Function("RSSFeed")>]
    member _.RSSFeed
        ([<HttpTrigger(AuthorizationLevel.Function, "get", Route = "myTest")>] req: HttpRequestData, executionContext: FunctionContext) =
        let res = req.CreateResponse(HttpStatusCode.OK)
        res.Headers.Add("Content-Type", "text/plain; charset=utf-8");

        res.WriteString("Welcome to Azure Functions!");
        res
