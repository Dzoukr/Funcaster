namespace Funcaster.Functions

open System
open System.Collections.Generic
open System.Net
open System.Threading.Tasks
open System.Xml
open Funcaster
open Funcaster.Domain
open Microsoft.Azure.Functions.Worker
open Microsoft.Azure.Functions.Worker.Http
open Microsoft.Extensions.Logging


type Functions() =

    [<Function("RssFeed")>]
    member _.RssFeed ([<HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "rss")>] req: HttpRequestData, ctx: FunctionContext) =
        
        let channel : Channel = {
            Title = "MyTitle"
            Link = Uri("http://example.com")
            Description = "todo"
            Language = Some "cs"
            Author = "Roman"
            Owner = { Name = "Romic"; Email = "neco"}
            Explicit = false
            Image = Uri("http://example.com/image")
            Category = Some "Technology"
            Type = ChannelType.Episodic
            Restrictions = []
        }
        
        let item : Item = {
            Guid = "todo"
            Episode = Some { Season = 1; Episode = 1}
            Enclosure = {
                Url = Uri("http://example.com")
                Type = "audio/mpeg"
                Lenght = 1231231 }
            Publish = DateTimeOffset.UtcNow
            Title = "Super dil"
            Description = "Mrda jak svina"
            Restrictions = []
            Duration = TimeSpan.FromMinutes 25.
            Explicit = false
            Image = None
            Keywords = []
            EpisodeType = EpisodeType.Full
        }
        
        let res = req.CreateResponse(HttpStatusCode.OK)
        res.Headers.Add("Content-Type", "application/rss+xml; charset=utf-8");
        RssXml.getDoc channel [item] |> RssXml.toString |> res.WriteString
        res