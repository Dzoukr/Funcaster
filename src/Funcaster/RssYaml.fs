module Funcaster.RssYaml

open System
open System.Collections.Generic
open Domain
open YamlDotNet
open YamlDotNet.Serialization
open YamlDotNet.Serialization.NamingConventions

let listToOpt = function
    | [] -> None
    | l -> Some l

let fromListToNullable =
    listToOpt
    >> Option.map List.toArray
    >> Option.toObj

let fromNullableToList =
    Option.ofObj<string []>
    >> Option.map Array.toList
    >> Option.defaultValue []

type YamlOwner() =
    member val Name = "" with get, set
    member val Email = "" with get, set
    
    static member OfOwner (o:Owner) =
        let y = YamlOwner()
        y.Name <- o.Name 
        y.Email <- o.Email
        y
    
    static member ToOwner (y:YamlOwner) =
        {
            Name = y.Name
            Email = y.Email
        }

type YamlChannel() =
    member val Title : string = "" with get, set
    member val Link : string = "" with get, set
    member val Description : string = "" with get, set
    member val Language : string = null with get, set
    member val Author : string = "" with get, set
    member val Owner : YamlOwner = YamlOwner() with get, set
    member val Explicit = false with get, set
    member val Image : string = "" with get, set
    member val Category : string = null with get, set
    member val Type : string = "" with get, set
    member val Restrictions : string [] = null with get, set
    
    static member OfChannel (ch:Channel) =
        let y = YamlChannel()
        y.Title <- ch.Title
        y.Link <- ch.Link |> string
        y.Description <- ch.Description
        y.Language <- ch.Language |> Option.toObj
        y.Author <- ch.Author
        y.Owner <- ch.Owner |>YamlOwner.OfOwner
        y.Explicit <- ch.Explicit
        y.Image <- ch.Image |> string
        y.Category <- ch.Category |> Option.toObj
        y.Type <- ch.Type |> ChannelType.value
        y.Restrictions <- ch.Restrictions |> fromListToNullable //|> listToOpt |> Option.map List.toArray |> Option.toObj
        y
        
    static member ToChannel (y:YamlChannel) : Channel =
        {
            Title = y.Title
            Link = y.Link |> Uri
            Description = y.Description
            Language = y.Language |> Option.ofObj
            Author = y.Author
            Owner = y.Owner |> YamlOwner.ToOwner
            Explicit = y.Explicit
            Image = y.Image |> Uri
            Category = y.Category |> Option.ofObj
            Type = y.Type |> ChannelType.create
            Restrictions = y.Restrictions |> fromNullableToList
        }

[<AllowNullLiteral>]
type YamlEpisode() =
    member val Season = 0 with get, set
    member val Episode = 0 with get, set
    
    static member OfEpisode (e:Episode) =
        let y = YamlEpisode()
        y.Season <- e.Season
        y.Episode <- e.Episode
        y
    
    static member ToEpisode (y:YamlEpisode) =
        {
            Season = y.Season
            Episode = y.Episode
        }

type YamlEnclosure() =
    member val Url = "" with get, set
    member val Type = "" with get, set
    member val Length = 0L with get, set
    
    static member OfEnclosure (e:Enclosure) =
        let y = YamlEnclosure()
        y.Url <- e.Url |> string
        y.Type <- e.Type
        y.Length <- e.Length
        y
    
    static member ToEnclosure (y:YamlEnclosure) =
        {
            Url = y.Url |> Uri
            Type = y.Type
            Length = y.Length
        }

type YamlItem() =
    member val Guid = "" with get, set
    member val Episode : YamlEpisode = null with get, set
    member val Enclosure = YamlEnclosure() with get, set
    member val Publish = "" with get, set
    member val Title = "" with get, set
    member val Description = "" with get, set
    member val Restrictions : string [] = null with get, set
    member val Duration = "" with get, set
    member val Explicit = false with get, set
    member val Image = null with get, set
    member val Keywords : string [] = null with get, set
    member val EpisodeType = "" with get, set
    
    static member OfItem (e:Item) =
        let y = YamlItem()
        y.Guid <- e.Guid
        y.Episode <- e.Episode |> Option.map YamlEpisode.OfEpisode |> Option.toObj
        y.Enclosure <- e.Enclosure |> YamlEnclosure.OfEnclosure
        y.Publish <- e.Publish.ToString("o")
        y.Title <- e.Title
        y.Description <- e.Description
        y.Restrictions <- e.Restrictions |> fromListToNullable
        y.Duration <- e.Duration |> string
        y.Explicit <- e.Explicit
        y.Image <- e.Image |> Option.map string |> Option.toObj
        y.Keywords <- e.Keywords |> fromListToNullable
        y.EpisodeType <- e.EpisodeType |> EpisodeType.value
        y

    static member ToItem (y:YamlItem) =
        {
            Guid = y.Guid
            Episode = y.Episode |> Option.ofObj |> Option.map YamlEpisode.ToEpisode
            Enclosure = y.Enclosure |> YamlEnclosure.ToEnclosure
            Publish = DateTimeOffset.Parse y.Publish
            Title = y.Title
            Description = y.Description
            Restrictions = y.Restrictions |> fromNullableToList
            Duration = y.Duration |> TimeSpan.Parse
            Explicit = y.Explicit
            Image = y.Image |> Option.ofObj |> Option.map Uri
            Keywords = y.Keywords |> fromNullableToList
            EpisodeType = y.EpisodeType |> EpisodeType.create
        }

let serializer =
    SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build()

let deserializer =
    DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build()