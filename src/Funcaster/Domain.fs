module Funcaster.Domain

open System

type Enclosure = {
    Url : Uri
    Type : string
    Length : int64
}

type EpisodeType =
    | Full
    | Trailer
    | Bonus
    
module EpisodeType =
    let value = function
        | Full -> "full"
        | Trailer -> "trailer"
        | Bonus -> "bonus"
    
    let create (v:string) =
        match v.ToUpper() with
        | "FULL" -> Full
        | "TRAILER" -> Trailer
        | "BONUS" -> Bonus
        | x -> failwith $"Unrecognized value for EpisodeType {x}"
    
type Item = {
    Guid : string
    Season : int option
    Episode : int option
    Enclosure : Enclosure
    Publish : DateTimeOffset
    Title : string
    Description : string
    Restrictions : string list
    Duration : TimeSpan
    Explicit : bool
    Image : Uri option
    Keywords : string list
    EpisodeType : EpisodeType
}

type Owner = {
    Name : string
    Email : string
}

type ChannelType = Episodic | Serial

module ChannelType =
    let value = function
        | Episodic -> "episodic"
        | Serial -> "serial"
    
    let create (v:string) =
        match v.ToUpper() with
        | "EPISODIC" -> Episodic
        | "SERIAL" -> Serial
        | x -> failwith $"Unrecognized value for ChannelType {x}"

type Channel = {
    Title : string
    Link : Uri
    Description : string
    Language : string option
    Author : string
    Owner : Owner
    Explicit : bool
    Image : Uri
    Category : string option
    Type : ChannelType
    Restrictions : string list
}

type CdnSetup = {
    IsEnabled : bool
    CdnUrl : Uri
}

module CdnSetup =
    let none = { IsEnabled = false; CdnUrl = Uri("https://www.example.com") }