namespace Funcaster.Functions

open System
open System.Net
open Funcaster.Domain
open Funcaster.RssXml
open Funcaster.Storage
open Microsoft.Azure.Functions.Worker
open Microsoft.Azure.Functions.Worker.Http
open Microsoft.Extensions.Logging

module Stubs =
    let getEmptyChannel : Channel =
        let msg = "Please finish feed setup using FuncasterStudio on https://github.com/dzoukr/FuncasterStudio"
        {
            Title = msg
            Link = Uri("https://github.com/dzoukr/FuncasterStudio")
            Description = msg
            Language = None
            Author = msg
            Owner = { Name = msg; Email = msg }
            Explicit = false
            Image = Uri("https://github.com/dzoukr/FuncasterStudio")
            Category = None
            Type = ChannelType.Episodic
            Restrictions = []
        }

type Functions(log:ILogger<Functions>, podcast:PodcastTable, episodes:EpisodesTable) =

    [<Function("RssFeed")>]
    member _.RssFeed ([<HttpTrigger(AuthorizationLevel.Anonymous, "get", "head", Route = "rss")>] req: HttpRequestData, ctx: FunctionContext) =
        task {
            let! channelOpt = getPodcast podcast ()
            let channel = channelOpt |> Option.defaultValue Stubs.getEmptyChannel
            let! allItems = getEpisodes episodes ()
            let items = allItems |> List.filter (fun x -> x.Publish <= DateTimeOffset.UtcNow)
            let res = req.CreateResponse(HttpStatusCode.OK)
            res.Headers.Add("Content-Type", "application/rss+xml; charset=utf-8");
            RssXml.getDoc channel items |> RssXml.toString |> res.WriteString
            return res
        }
