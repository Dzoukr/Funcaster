namespace Funcaster.Functions

open System
open System.Collections.Generic
open System.IO
open System.Net
open System.Text.Json
open System.Threading.Tasks
open System.Xml
open Azure.Storage.Blobs
open Funcaster.RssXml
open Funcaster
open Funcaster.Domain
open Microsoft.Azure.Functions.Worker
open Microsoft.Azure.Functions.Worker.Http
open Microsoft.Extensions.Logging


type Functions(log:ILogger<Functions>, blobClient:BlobServiceClient) =
    
    let getDefaultItem guid url length contentType : Item =
        {
            Guid = guid
            Episode = None
            Enclosure = { Url = url; Type = contentType; Length = length }
            Publish = DateTimeOffset.MaxValue
            Title = ""
            Description = ""
            Restrictions = []
            Duration = TimeSpan.Zero
            Explicit = false
            Image = None
            Keywords = []
            EpisodeType = EpisodeType.Full
        }
    
//    let getBlobContainerClient (path:string) =
//        let containers = path.Split([| '\\'; '/' |])
//        for c in containers do
//            
    
    [<Function("PodcastUploaded")>]
    member _.PodcastUploaded ([<BlobTrigger("podcast/episodes/{name}", Connection = "PodcastStorage")>] ctx: FunctionContext) =
        let data = ctx.BindingContext.BindingData
        let uri = data.["Uri"].ToString().Trim('\"')
        let ext = uri |> Path.GetExtension
        
        let relativePath = data.["BlobTrigger"] |> string
        let filename = Path.GetFileName relativePath
        let container = (Path.GetDirectoryName relativePath).Replace("\\","/")
                
        let metadata = data.["Properties"] |> string |> JsonSerializer.Deserialize<{| Length : int64; ContentType : string |}>
        if ext <> ".yaml" then
            log.LogInformation $"Uri = {uri}; Ext = {ext}; Name = {filename}"
            let item = getDefaultItem uri (Uri uri) metadata.Length metadata.ContentType
            let client = blobClient.GetBlobContainerClient(container)
            let bc = client.GetBlobClient("test.yaml")
            bc.Upload(new MemoryStream())
            ()
        ()

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
            Restrictions = ["JSOU"]
        }
        
        let item : Item = {
            Guid = "todo"
            Episode = Some { Season = 1; Episode = 1}
            Enclosure = {
                Url = Uri("http://example.com")
                Type = "audio/mpeg"
                Length = 1231231L }
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
        //RssXml.getDoc channel [item] |> RssXml.toString |> res.WriteString
        
        //RssYaml.toString channel [item] |> res.WriteString
        res