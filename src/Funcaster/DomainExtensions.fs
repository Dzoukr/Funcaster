module Funcaster.DomainExtensions

open System
open Funcaster.Domain

module CdnSetup =
    let private _rewriteUrl (old:Uri) (new':Uri) =
        let builder = UriBuilder(old)
        builder.Host <- new'.Host
        builder.Uri
    
    let rewriteUrl (cdn:CdnSetup) (url:Uri) =
        if cdn.IsEnabled then _rewriteUrl url cdn.CdnUrl else url
    
    let rewriteItem (cdn:CdnSetup) (item:Item) =
        { item
            with
                Enclosure = { item.Enclosure with Url = rewriteUrl cdn item.Enclosure.Url }
                Image = item.Image |> Option.map (rewriteUrl cdn)
        }
    
    let rewriteChannel (cdn:CdnSetup) (channel:Channel) =
        { channel with Image = rewriteUrl cdn channel.Image }