module Funcaster.Domain

open System

type Episode = {
    Season : int
    Episode : int
}

type Enclosure = {
    Url : Uri
    Type : string
    Lenght : int
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

type Item = {
    Guid : string
    Episode : Episode option
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