open System
open System.IO
open System.Speech.Synthesis
open System.Speech.Recognition
open System.Windows.Forms
open System.Text.Json
open System.Threading.Tasks

open Types
open Variables
open Functions
open Settings


[<EntryPoint>]
let main argv = 

    let windowWidth  = 50
    let windowHeight = 10
    //let charWidth = 1.0
    //let windowWidthPixels = (float)windowWidth * charWidth
    //let posX = System.Windows.SystemParameters.PrimaryScreenWidth - windowWidthPixels
    //let posY = 20
    Console.Title <- "VimVoice coder"
    Console.SetWindowSize (windowWidth, windowHeight)
    //Console.SetWindowPosition ((int)posX, (int)posY)
    //Console.Beep()
    
    // Synthesizer setup:
    synth.Rate   <- settings.DefaultFeedbackSpeed
    synth.Volume <- settings.DefaultFeedbackVolume
    try 
        synth.SelectVoice("Zira")
    with 
        | _ -> synth.SelectVoiceByHints(VoiceGender.Female)





        
    // Preparations for speech recognition setup:
    speak "started"  // Spoken before voice recognition setup to avoid feedback loop for speaker users
    let mutable voiceRecognitionPaused = false
    let mutable externalTranscriberInvoked = false
    let mutable lastKeystroke = ""
    let mutable queuedPrepend = ""
    let volumeStep = 20

    // Speech recognition setup:
    reco.SpeechRecognized.Add(fun a ->
        let res = a.Result
        if res <> null && res.Confidence > 0.f then
            Console.ForegroundColor <- ConsoleColor.White
            printf "%s" res.Text
            Console.ForegroundColor <- ConsoleColor.Gray
            printfn " (%f)" res.Confidence
            let semantics = res.Semantics.Value
            let send =
                if !mode = Insert || (semantics <> null && semantics :?> string = "search")
                then insertKeys res.Text else recoToKeys res
                //Console.ForegroundColor <- ConsoleColor.DarkYellow
                //printfn "  %s" (fixKeys send)
            SendKeys.SendWait(fixKeys send)
            Console.ForegroundColor <- ConsoleColor.Yellow
            printfn "  %s" send
            if res.Text.StartsWith("yank ") then speak "yanked" // TODO: can't do as prompt because of overloaded value
            elif res.Text.StartsWith("copy ") then speak "copied" // TODO: can't do as prompt because of overloaded value
            match if res.Semantics.Value = null then None else Some (res.Semantics.Value :?> string) with
            | Some "normal" ->
                if not (!mode = Normal) then // visual commands
                    switchGrammar normalMode
                    mode := Normal
                    Console.ForegroundColor <- ConsoleColor.Red
                    printfn "  NORMAL"
                    speak "Normal mode"
            | Some "insert" ->
                switchGrammar insertMode
                if ctagsGrammar.IsSome then reco.LoadGrammar(ctagsGrammar.Value)
                mode := Insert
                Console.ForegroundColor <- ConsoleColor.Red
                printfn "  INSERT"
                speak "Insert mode"
            | Some "visual" ->
                switchGrammar visualMode
                mode := VisualMode
                Console.ForegroundColor <- ConsoleColor.Red
                printfn "  VISUAL"
                speak "Visual mode"
            | Some "replace" ->
                switchGrammar insertMode
                mode := Replace
                Console.ForegroundColor <- ConsoleColor.Red
                printfn "  REPLACE"
                speak "Replace mode"
            | Some "search" | Some "command" -> SendKeys.SendWait("{ENTER}")
            | Some prompt ->
                if prompt.StartsWith "prompt: " then
            if res.Confidence > 0.85f then //85f then
                    
                    let say = prompt.Substring(8)
                    if say.EndsWith("%s") then
                        let x = say.Substring(0, say.Length - 2)
                        let y = res.Text.Substring(res.Text.LastIndexOf(' '))
                        let s = sprintf "%s %s" x y
                        speak s
                    else speak say
                else failwith "Unknown mode."
            | None -> ()
            Console.ForegroundColor <- ConsoleColor.White
            printf "\n> "
        else
            Console.ForegroundColor <- ConsoleColor.Blue
            printfn "INVALID\n"
            speak "Say again?"
            Console.ForegroundColor <- ConsoleColor.White
            printf "\n> ")

    Console.BackgroundColor <- ConsoleColor.Black
    Console.ForegroundColor <- ConsoleColor.White
    Console.Clear()

    switchGrammar normalMode

    printfnColor(ConsoleColor.Green, "%s", "VimVoice")
    promptCloseMuted()

    let rec eatKeys () = Console.ReadKey(true) |> ignore; eatKeys ()

    try
        eatKeys ()
    with 
    | _ ->
        Environment.Exit 1


    // todo: Registers as a separate phrase? Why does GrammarBuilder barf?
    0
