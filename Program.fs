open Argu
open FSharp.Data

open System.IO

type OE0001_V10 = CsvProvider<"./OE10.csv", HasHeaders=true, Separators=";", EmbeddedResource="OETransform, OETransform.OE10.csv", Culture="de-DE">
type OE0001_V12 = CsvProvider<"./OE12.csv", HasHeaders=true, Separators=";", EmbeddedResource="OETransform, OETransform.OE12.csv", Culture="de-DE">
type IOF_V30 = XmlProvider<Schema="./iof-3.0.xsd", EmbeddedResource="OETransform, OETransform.iof-3.0.xsd">

//// type TargetFormat =
////     | CSV
////     | XML

type CliArguments =
    | [<Mandatory; AltCommandLine("-i")>] Input_File of inputFile:string
    | [<AltCommandLine("-o")>]Output_File of outputFile:string
    | Overwrite
////    | Target_Format of format:TargetFormat

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Input_File _ -> "specify an input file."
            | Output_File _ -> "specify an output file."
            | Overwrite -> "if set, exiting output file will be overwritten."
////            | Target_Format _ -> "target format of conversion. CSV or XML (default: CSV)"


[<EntryPoint>]
let main argv =

    let commandParser = ArgumentParser.Create<CliArguments>(
                            helpTextMessage = "Hello and welcome",
                            programName = "oetransform.exe",
                            errorHandler = ProcessExiter(),
                            checkStructure = false)
    let args = commandParser.ParseCommandLine(inputs = argv, raiseOnUsage = true)

    let buildOE12Row (obj:OE0001_V10.Row) =

        new OE0001_V12.Row("", "", "", "", obj.Chipnr, obj.``Datenbank Id``, "", obj.Nachname, obj.Vorname, "", obj.Jg, obj.G, "", obj.AK, "", "", "",
                        obj.Wertung, "", "", "", obj.``Club-Nr.``, "", obj.Ort, "", "", "", obj.Katnr, obj.Kurz, obj.Lang, 0, "", "",
                        "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "",
                        obj.Gemietet, obj.Startgeld, obj.Bezahlt, "", "", 0, "", 0.0m, 0, 0, "")

    let tryLocateFile (wDir:string) (f:string) =
        // check if we have rooted path
        if Path.IsPathRooted(f) && File.Exists(f) then
            Some f
        else
            // we have rel path
            let fn = Path.Combine(wDir, f)
            ////printfn "testing %s" fn
            if File.Exists(fn) then
                Some fn
            else
                None

    let iFile = tryLocateFile __SOURCE_DIRECTORY__ (args.GetResult Input_File)

    match iFile with
        | None -> ()
        | Some f ->
            let inp = OE0001_V10.Load(f)

            let transformedRows = inp.Rows |> Seq.map buildOE12Row |> Seq.toList
            let newCsv = new OE0001_V12(transformedRows)
            let s = newCsv.SaveToString()

            let o = (args.TryGetResult Output_File) |> Option.defaultValue ("out_" + Path.GetFileName(f))
            let oFile = tryLocateFile __SOURCE_DIRECTORY__ o
            match oFile with
            | Some x ->
                if args.Contains Overwrite then
                    File.WriteAllText(o, s);
                else
                    printfn "Output file already exists - use the '--overwrite' option if you want to override it."
            | None ->
                File.WriteAllText(o, s);

            ////let xmlOutput = IOF_V30.EntryList
            ////                [| for r in inp.Rows do
            ////                    yield IOF_V30.Person(r.G, None, ) |]
            ////printfn "%A" xmlOutput
    0