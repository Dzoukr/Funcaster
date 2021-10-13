module Funcaster.RssXml

open System
open System.Text
open System.Xml.Linq
open Funcaster.Domain
open Funcaster.Xml

module private Namespaces =
    let itunes = "http://www.itunes.com/dtds/podcast-1.0.dtd"
    let atom = "http://www.w3.org/2005/Atom"
    let media = "https://search.yahoo.com/mrss/"
    let dcterms = "https://purl.org/dc/terms/"
    let spotify = "https://www.spotify.com/ns/rss"
    let psc = "https://podlove.org/simple-chapters/"

[<RequireQualifiedAccess>]
module RssXml =
    let private getDocument () = XDocument(XDeclaration("1.0", "UTF-8", null))

    let private getRoot () =
        Xml.create "rss"
        |> Xml.attributeNs XNamespace.Xmlns "itunes" Namespaces.itunes
        |> Xml.attributeNs XNamespace.Xmlns "atom" Namespaces.atom
        |> Xml.attributeNs XNamespace.Xmlns "media" Namespaces.media
        |> Xml.attributeNs XNamespace.Xmlns "dcterms" Namespaces.dcterms
        |> Xml.attributeNs XNamespace.Xmlns "spotify" Namespaces.spotify
        |> Xml.attributeNs XNamespace.Xmlns "psc" Namespaces.psc

    let private tryGetRestrictions countries =
        match countries with
        | [] -> None
        | list ->
            Xml.createNs Namespaces.media "restriction"
            |> Xml.attribute "type" "country"
            |> Xml.attribute "relationship" "allow"
            |> Xml.value (list |> String.concat " ")
            |> Some

    let private tryGetKeywords keys =
        match keys with
        | [] -> None
        | list -> Xml.createValueNs Namespaces.itunes "keywords" (list |> String.concat ",") |> Some

    let private getItem (item:Item) =
        Xml.create "item"
        |> Xml.append (Xml.createValue "guid" item.Guid |> Xml.attribute "isPermalink" "false")
        |> Xml.appendOpt (item.Episode |> Option.map (fun e -> Xml.createValueNs Namespaces.itunes "episode" $"{e.Episode}"))
        |> Xml.appendOpt (item.Episode |> Option.map (fun e -> Xml.createValueNs Namespaces.itunes "season" $"{e.Season}"))
        |> Xml.append (
            Xml.create "enclosure"
            |> Xml.attribute "url" (string item.Enclosure.Url)
            |> Xml.attribute "type" item.Enclosure.Type
            |> Xml.attribute "length" (string item.Enclosure.Length)
        )
        |> Xml.append (Xml.createValue "pubDate" (item.Publish.ToString("r")))
        |> Xml.append (Xml.createValue "title" item.Title)
        |> Xml.append (Xml.createValueXCData "description" item.Description)
        |> Xml.appendOpt (item.Restrictions |> tryGetRestrictions) // opt
        |> Xml.append (Xml.createValueNs Namespaces.itunes "duration" (string item.Duration))
        |> Xml.append (Xml.createValueNs Namespaces.itunes "explicit" (item.Explicit.ToString().ToLower())) // opt
        |> Xml.appendOpt (item.Image |> Option.map (fun i -> Xml.createNs Namespaces.itunes "image" |> Xml.attribute "href" (string i)))
        |> Xml.appendOpt (item.Keywords |> tryGetKeywords) // opt
        |> Xml.append (Xml.createValueNs Namespaces.itunes "episodeType" (item.EpisodeType |> EpisodeType.value)) // opt

    let private getChannel (channel:Channel) (xs:Item list) =
        let ch =
            Xml.create "channel"
            |> Xml.append (Xml.createValue "title" channel.Title)
            |> Xml.append (Xml.createValue "link" (string channel.Link))
            |> Xml.append (Xml.createValueXCData "description" channel.Description)
            |> Xml.appendOpt (channel.Language |> Option.map (fun l -> Xml.createValue "language" l))                         // opt
            |> Xml.append (Xml.createValueNs Namespaces.itunes "author" channel.Author)
            |> Xml.append (
                Xml.createNs Namespaces.itunes "owner"
                |> Xml.append (Xml.createValueNs Namespaces.itunes "name" channel.Owner.Name)
                |> Xml.append (Xml.createValueNs Namespaces.itunes "email" channel.Owner.Email)
            )
            |> Xml.append (Xml.createNs Namespaces.itunes "image" |> Xml.attribute "href" (string channel.Image))
            |> Xml.append (Xml.createValueNs Namespaces.itunes "explicit" (channel.Explicit.ToString().ToLower()))  // opt
            |> Xml.appendOpt (channel.Category |> Option.map (fun x -> Xml.createNs Namespaces.itunes "category" |> Xml.attribute "text" x))          // opt
            |> Xml.append (Xml.createValueNs Namespaces.itunes "type" "episodic")  // opt
            |> Xml.appendOpt (channel.Restrictions |> tryGetRestrictions) // opt
            
        xs |> List.fold (fun acc item -> acc |> Xml.append (getItem item)) ch
        

    let getDoc (ch:Channel) (xs:Item list) =
        getDocument()
        |> Xml.appendDoc (getRoot())
        |> Xml.append (getChannel ch xs)

    let toString (doc:XElement) =
        use memory = new System.IO.MemoryStream()
        doc.Save(memory)
        Encoding.UTF8.GetString(memory.ToArray())