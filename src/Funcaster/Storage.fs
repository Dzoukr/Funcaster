module Funcaster.Storage

open System
open Funcaster.Domain
open Azure.Data.Tables
open Azure.Data.Tables.FSharp
open Newtonsoft.Json

let key (s:string) =
    ["/";"\\";"#";"?"]
    |> List.fold (fun (acc:string) item -> acc.Replace(item, "")) s
    |> (fun x -> x.ToLower())

module private Helpers =
    let toSeparatedString = function
        | [] -> null
        | r -> r |> String.concat "|"

    let fromSeparatedString (s:string) =
        s
        |> Option.ofObj
        |> Option.map (fun x -> x.Split("|", StringSplitOptions.RemoveEmptyEntries))
        |> Option.map (Seq.toList >> List.map (fun x -> x.Trim()))
        |> Option.defaultValue []

type PodcastTable = PodcastTable of TableClient

module PodcastTable =
    let create (conn:string) =
        TableClient(conn, "Podcast") |> PodcastTable

module Channel =
    let toEntity (c:Channel) : TableEntity =
        let e = TableEntity()
        e.PartitionKey <- "podcast"
        e.RowKey <- "podcast"
        e.["Title"] <- c.Title
        e.["Link"] <- c.Link |> string
        e.["Description"] <- c.Description
        e.["Language"] <- c.Language |> Option.toObj
        e.["Author"] <- c.Author
        e.["Owner"] <- c.Owner |> JsonConvert.SerializeObject
        e.["Explicit"] <- c.Explicit
        e.["Image"] <- c.Image |> string
        e.["Category"] <- c.Category |> Option.toObj
        e.["Type"] <- c.Type |> ChannelType.value
        e.["Restrictions"] <- c.Restrictions |> Helpers.toSeparatedString
        e

    let fromEntity (e:TableEntity) : Channel =
        {
            Title = e.GetString("Title")
            Link = e.GetString("Link") |> Uri
            Description = e.GetString("Description")
            Language = e.GetString("Language") |> Option.ofObj
            Author = e.GetString("Author")
            Owner = e.GetString("Owner") |> JsonConvert.DeserializeObject<Owner>
            Explicit = e.GetBoolean("Explicit") |> Option.ofNullable |> Option.defaultValue false
            Image = e.GetString("Image") |> Uri
            Category = e.GetString("Category") |> Option.ofObj
            Type = e.GetString("Type") |> ChannelType.create
            Restrictions = e.GetString("Restrictions") |> Helpers.fromSeparatedString
        }

let getPodcast (PodcastTable podcastTable) () =
    task {
        return
            tableQuery {
                filter (pk "podcast" + rk "podcast")
            }
            |> podcastTable.Query<TableEntity>
            |> Seq.tryHead
            |> Option.map Channel.fromEntity
    }

let upsertPodcast (PodcastTable podcastTable) (channel:Channel) =
    task {
        let entity = channel |> Channel.toEntity
        let! _ = podcastTable.UpsertEntityAsync(entity, TableUpdateMode.Merge)
        return ()
    }

type EpisodesTable = EpisodesTable of TableClient

module EpisodesTable =
    let create (conn:string) =
        TableClient(conn, "Episodes") |> EpisodesTable

module Item =
    let toPartialEnclosureEntity (guid:string) (c:Enclosure) : TableEntity =
        let e = TableEntity()
        e.PartitionKey <- guid |> key
        e.RowKey <- guid.ToLower()
        e.["Enclosure"] <- c |> JsonConvert.SerializeObject
        e

    let toEntity (c:Item) : TableEntity =
        let e = TableEntity()
        e.PartitionKey <- c.Guid |> key
        e.RowKey <- c.Guid |> key
        e.["Guid"] <- c.Guid
        e.["Season"] <- c.Season |> Option.toNullable
        e.["Episode"] <- c.Episode |> Option.toNullable
        e.["Enclosure"] <- c.Enclosure |> JsonConvert.SerializeObject
        e.["Publish"] <- c.Publish
        e.["Title"] <- c.Title
        e.["Description"] <- c.Description
        e.["Restrictions"] <- c.Restrictions |> Helpers.toSeparatedString
        e.["Duration"] <- c.Duration |> string
        e.["Explicit"] <- c.Explicit
        e.["Image"] <- c.Image |> Option.map string |> Option.toObj
        e.["Keywords"] <- c.Keywords |> Helpers.toSeparatedString
        e.["EpisodeType"] <- c.EpisodeType |> EpisodeType.value
        e

    let fromEntity (e:TableEntity) : Item =
        {
            Guid = e.GetString "Guid"
            Season = e.GetInt32 "Season" |> Option.ofNullable
            Episode = e.GetInt32 "Episode" |> Option.ofNullable
            Enclosure = e.GetString "Enclosure" |> JsonConvert.DeserializeObject<Enclosure>
            Publish = e.GetDateTimeOffset("Publish").Value
            Title = e.GetString "Title"
            Description = e.GetString "Description"
            Restrictions = e.GetString "Restrictions" |> Helpers.fromSeparatedString
            Duration = e.GetString "Duration" |> TimeSpan.Parse
            Explicit = e.GetBoolean("Explicit").Value
            Image = e.GetString "Image" |> Option.ofObj |> Option.map Uri
            Keywords = e.GetString "Keywords" |> Helpers.fromSeparatedString
            EpisodeType = e.GetString "EpisodeType" |> EpisodeType.create
        }

let getEpisodes (EpisodesTable episodesTable) () =
    task {
        return
            TableQuery.Empty
            |> episodesTable.Query<TableEntity>
            |> Seq.map Item.fromEntity
            |> Seq.toList
    }

let getEpisode (EpisodesTable episodesTable) (g:string) =
    task {
        let g = g |> key
        return
            tableQuery {
                filter (pk g + rk g)
            }
            |> episodesTable.Query<TableEntity>
            |> Seq.map Item.fromEntity
            |> Seq.tryHead
    }

let upsertEpisode (EpisodesTable episodesTable) (item:Item) =
    task {
        let entity = item |> Item.toEntity
        let! _ = episodesTable.UpsertEntityAsync(entity, TableUpdateMode.Merge)
        return ()
    }

let deleteEpisode (EpisodesTable episodesTable) (guid:string) =
    task {
        let! _ = episodesTable.DeleteEntityAsync(key guid, key guid)
        return ()
    }

let updateEnclosure (EpisodesTable episodesTable) (guid:string) (enc:Enclosure) =
    task {
        let entity = enc |> Item.toPartialEnclosureEntity (guid |> key)
        let! _ = episodesTable.UpsertEntityAsync(entity, TableUpdateMode.Merge)
        return ()
    }