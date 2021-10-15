namespace Funcaster.Functions

open System
open System.IO
open System.Net
open System.Text.Json
open Azure.Storage.Blobs
open Funcaster.Domain
open Funcaster.RssXml
open Funcaster.RssYaml
open Microsoft.Azure.Functions.Worker
open Microsoft.Azure.Functions.Worker.Http
open Microsoft.Extensions.Logging

module Paths =
    let [<Literal>] Root = "podcast/"
    let [<Literal>] Episodes = "episodes/"
    let [<Literal>] Index = "_index.yaml"
    let [<Literal>] Metadata = "podcast.yaml"

module Stubs =
    let getItemStub guid url length contentType : Item =
        {
            Guid = guid
            Season = None
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
    
    let getChannelStub : Channel =
        {
            Title = "Fill me!"
            Link = Uri("https://example.com")
            Description = "Fill me!"
            Language = Some "You can fill me or delete!"
            Author = "Fill me!"
            Owner = { Name = "Fill me!"; Email = "Fill me!" }
            Explicit = false
            Image = Uri("https://example.com")
            Category = Some "You can fill me or delete!"
            Type = ChannelType.Episodic
            Restrictions = []
        }

module Helpers =
    let downloadDeserialized<'a> (bc:BlobClient) =
        bc.
            DownloadContent()
            .Value
            .Content
            .ToString()
        |> deserializer.Deserialize<'a>
    
    let uploadSerialized (bc:BlobClient) (i:_) =
        i
        |> serializer.Serialize
        |> BinaryData.FromString
        |> (fun x -> bc.Upload(x, true))
        |> ignore

type Functions(log:ILogger<Functions>, blobClient:BlobServiceClient) =
    
    [<Function("PodcastFileChanged")>]
    member _.PodcastFileChanged ([<BlobTrigger(Paths.Root + Paths.Episodes + "{name}", Connection = "PodcastStorage")>] ctx: FunctionContext) =
        let data = ctx.BindingContext.BindingData
        let uri = data.["Uri"].ToString().Trim('\"')
        let ext = uri |> Path.GetExtension
        let originalFilename = Paths.Episodes + data.["Name"].ToString()
        
        let client = blobClient.GetBlobContainerClient(Paths.Root)
        
        // create yaml file stub
        if ext <> ".yaml" then
            let metadata = data.["Properties"] |> string |> JsonSerializer.Deserialize<{| Length : int64; ContentType : string |}>
            let filename = Path.ChangeExtension(originalFilename, ".yaml")
            log.LogInformation("Creating Yaml for newly added file {filename}", filename)
            let bc = client.GetBlobClient(filename)
            
            Stubs.getItemStub uri (Uri uri) metadata.Length metadata.ContentType
            |> YamlItem.OfItem
            |> Helpers.uploadSerialized bc
        // index yaml file
        else
            let newlyAddedItem = client.GetBlobClient(originalFilename) |> Helpers.downloadDeserialized<YamlItem>
            
            log.LogInformation("Reindexing episodes index file")
            let index = client.GetBlobClient(Paths.Index)
            if index.Exists().Value then
                let c = index.DownloadContent().Value
                let yamlItems = c.Content.ToString() |> deserializer.Deserialize<YamlItem []>
                
                // put the latest on the top
                yamlItems
                |> Array.filter (fun x -> x.Guid <> newlyAddedItem.Guid)
                |> Array.append [| newlyAddedItem |]
                |> Array.sortByDescending (fun x -> DateTimeOffset.Parse(x.Publish))
                |> Array.distinctBy (fun x -> x.Guid)
                |> Helpers.uploadSerialized index
            else
                newlyAddedItem |> Array.singleton |> Helpers.uploadSerialized index
        
        // ensure channel info always available
        let metadata = client.GetBlobClient(Paths.Metadata) 
        if metadata.Exists().Value |> not then
            Stubs.getChannelStub
            |> YamlChannel.OfChannel
            |> Helpers.uploadSerialized metadata
        ()

    [<Function("RssFeed")>]
    member _.RssFeed ([<HttpTrigger(AuthorizationLevel.Anonymous, "get", "head", Route = "rss")>] req: HttpRequestData, ctx: FunctionContext) =
        let client = blobClient.GetBlobContainerClient(Paths.Root)
        let channel = client.GetBlobClient(Paths.Metadata) |> Helpers.downloadDeserialized<YamlChannel> |> YamlChannel.ToChannel
        let items =
            client.GetBlobClient(Paths.Index)
            |> Helpers.downloadDeserialized<YamlItem []>
            |> Array.map YamlItem.ToItem
            |> Array.filter (fun x -> x.Publish <= DateTimeOffset.UtcNow)
            |> Array.toList
            
        let res = req.CreateResponse(HttpStatusCode.OK)
        res.Headers.Add("Content-Type", "application/rss+xml; charset=utf-8");
        RssXml.getDoc channel items |> RssXml.toString |> res.WriteString
        res