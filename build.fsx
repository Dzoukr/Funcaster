#r "paket: groupref build //"
#load "./.fake/build.fsx/intellisense.fsx"

open Fake
open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.DotNet
open System.IO.Compression

module Tools =
    let private findTool tool winTool =
        let tool = if Environment.isUnix then tool else winTool
        match ProcessUtils.tryFindFileOnPath tool with
        | Some t -> t
        | _ ->
            let errorMsg =
                tool + " was not found in path. " +
                "Please install it and make sure it's available from your path. "
            failwith errorMsg
            
    let private runTool (cmd:string) args workingDir =
        let arguments = args |> String.split ' ' |> Arguments.OfArgs
        Command.RawCommand (cmd, arguments)
        |> CreateProcess.fromCommand
        |> CreateProcess.withWorkingDirectory workingDir
        |> CreateProcess.ensureExitCode
        |> Proc.run
        |> ignore
        
    let dotnet cmd workingDir =
        let result =
            DotNet.exec (DotNet.Options.withWorkingDirectory workingDir) cmd ""
        if result.ExitCode <> 0 then failwithf "'dotnet %s' failed in %s" cmd workingDir
    
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

Target.runOrDefaultWithArguments "Run"