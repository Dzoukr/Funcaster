module Funcaster.Xml

open System.Xml.Linq

[<RequireQualifiedAccess>]
module Xml =
    let create name = XElement(XName.Get(name))
    let createNs ns name = XElement(XNamespace.Get(ns) + name)
    
    let value value (elm:XElement) =
        elm.SetValue(value)
        elm
    
    let valueXCData (value:string) (elm:XElement) =
        elm.Add(XCData(value))
        elm
    
    let createValue name v = name |> create |> value v
    let createValueXCData name v = name |> create |> valueXCData v
    let createValueNs ns name v = name |> createNs ns |> value v
        
    let append (elm:XElement) (parent:XElement) =
        parent.Add elm
        parent
    
    let appendOpt (elm:XElement option) (parent:XElement) =
        if elm.IsSome then 
            parent.Add elm.Value
        parent
    
    let appendDoc (elm:XElement) (parent:XDocument) =
        parent.Add elm
        parent.Root

    let attribute (name:string) (value:string) (parent:XElement) =
        parent.Add(XAttribute(XName.Get(name), value))
        parent
    
    let attributeNs (ns:XNamespace) (name:string) (value:string) (parent:XElement) =
        parent.Add(XAttribute(ns + name, value))
        parent