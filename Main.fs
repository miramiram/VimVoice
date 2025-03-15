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
            if res.Confidence > 0.85f then //85f then

                let wordValue = recoToKeys res

                if voiceRecognitionPaused then 
                    match wordValue with
                    | "§voicerecog:start" ->
                        voiceRecognitionPaused <- false
                        promptRecognized(res); promptKeystroke(wordValue); promptMode("started listening"); promptClose()
                        speak "Resuming voice recognition."
                    | _ ->
                        promptInaction("ignoring"); promptExtra("Say \"begin-hearing\" to unpause VimVoice."); promptCloseMuted()
                    
                    
                else

                    // Announce recognized input
                    promptRecognized(res) 
                    else  // Not a meta-action
                        // Process resulting keystrokes 
                        let useInsertMode = !mode = Insert || (res.Semantics.Value <> null && res.Semantics.Value :?> string = "search")
                        let insertOrReco  = if useInsertMode then insertKeys res.Text else wordValue  //recoToKeys res 

                        // Send and report key strokes
                        if insertOrReco <> "" then  
                            let send = handleSpecialChars queuedPrepend + handleSpecialChars insertOrReco
                            queuedPrepend <- "" 
                            lastKeystroke <-  send
                            SendKeys.SendWait(send)
                            promptKeystroke  (send)
                        else
                            promptKeystroke("") 


                        // Voice responses
                        if   res.Text.StartsWith("yank ") then speak "yanked" // Note: can't do as prompt due to overloaded value
                        elif res.Text.StartsWith("copy ") then speak "copied" // Note: can't do as prompt due to overloaded value

                    // Detect special cases
                    match if res.Semantics.Value = null then None else Some (res.Semantics.Value :?> string) with
                    // Modes
                    | Some "normal" ->
                        if not (!mode = Normal) then // visual commands
                            switchGrammar normalMode;   mode := Normal;         promptMode("NORMAL");           speak "Normal mode"
                    | Some "insert" ->
                        switchGrammar insertMode;       
                        if ctagsGrammar.IsSome then reco.LoadGrammar(ctagsGrammar.Value)
                        mode := Insert;                                         promptMode("INSERT");           speak "Insert mode"
                    | Some "visual" ->      
                        switchGrammar visualMode;       mode := VisualMode;     promptMode("VISUAL");           speak "Visual mode"
                    | Some "replace" ->     
                        switchGrammar insertMode;       
                        if ctagsGrammar.IsSome then reco.LoadGrammar(ctagsGrammar.Value)
                        mode := Replace;                                        promptMode("REPLACE");          speak "Replace mode"
                    | Some "windows" ->     
                        switchGrammar windowsMode;      mode := Windows;        promptMode("MSWINDOWS MODE");   speak "Microsoft Windows mode"
                    // Specially handled modes
                    | Some "search" | Some "command" -> SendKeys.SendWait("{ENTER}")
                    | Some prompt ->
                        if prompt.StartsWith "prompt: " then
                            let say = prompt.Substring(8)
                            if say.EndsWith("%s") then
                                let x = say.Substring(0, say.Length - 2)
                                let y = res.Text.Substring(res.Text.LastIndexOf(' '))
                                let s = sprintf "%s %s" x y
                                speak s
                            else speak say
                        else failwith "Unknown mode."
                    | None -> ()
                        
                    promptClose()
            else
                promptInaction("unclear")
                if voiceRecognitionPaused || externalTranscriberInvoked then
                    promptCloseMuted()
                else     
                
                    promptCloseMuted()
                    speak "Say again?"
    )
                    

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
