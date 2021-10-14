open Fake
open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators
open System.IO.Compression

open BuildHelpers

initializeContext()

module Paths =
    let private publishDir = Path.getFullName "publish"
    let private srcDir = Path.getFullName "src"
    let projectSrc = srcDir </> "Funcaster"
    let projectPublish = publishDir </> "Funcaster"

// Targets
let clean proj = [ proj </> "bin"; proj </> "obj" ] |> Shell.cleanDirs
let ensureDevFilesDeleted proj =
    [
        proj </> "appsettings.development.json"
        proj </> "local.settings.json"
    ] |> File.deleteAll

let publish srcDir publishDir =
    clean srcDir
    publishDir |> Directory.delete
    Tools.dotnet $"publish -c Release -o \"%s{publishDir}\"" srcDir
    publishDir |> ensureDevFilesDeleted

Target.create "Run" (fun _ ->
    Tools.dotnet "watch msbuild /t:RunFunctions" Paths.projectSrc
)

Target.create "Publish" (fun _ ->
    publish Paths.projectSrc Paths.projectPublish
    let zipFile = Paths.projectPublish + ".zip"
    File.delete zipFile
    ZipFile.CreateFromDirectory(Paths.projectPublish, zipFile)
)

let dependencies = [] // For future use

[<EntryPoint>]
let main args = runOrDefault args