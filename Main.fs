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



(*/*
 -- Notes --
    The quality of voice recognition changes from person to person based on their voice, and the system they use.
    Changing existing words shouldn't be done without consideration, as what is easily recognized for someone's system is harder for others.
    This voice-recognition subjectivity is also the reason for some of the unusual wordings, like "insertion" for switching to insert mode. 
    If you want to change things, keep this in mind, and preferrably add aliases instead of replacements.

    If you're okay with using Microsofts more accurate online text-to-speech service, Microsoft.Speech.Recognition might be a drop-in replacement.

-- Settings and keys --
   This program uses Microsoft's .NET to send keys, so refer to the documentation below to see how to spend special keys. Notably, special keys are sent with braces surrounding them, but due to parsing issues, this is a bit different in this program. If you want to write e.g. tab, you need to surround it with braces for .NET, and then additionally place \ in front of the braces, so that they are not interpreted as literal braces. E.g. "\{tab\}" will hit the tab button, while "tab" will press the keys T, A and B, individually.
   For shift/alt/tab, use the special cases "\ALT", "\SHIFT", "\CTRL".
   In the JSON settings file, backslashes aren't supported, so use the following substitutions:
   - "\{"     -> "LBR"
   - "\}"     -> "RBR"
   - "\ALT"   -> "WINALT"
   - "\SHIFT" -> "WINSHIFT"
   - "\CTRL"  -> "WINCTRL"


   
-- References --
    Keys:                   
        https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.sendkeys?view=windowsdesktop-9.0
    Vim setup for .fs files, to put in .vimrc, or just use the `set` commands:
        |augroup fs_ft
        |    au!
        |    function! SetupFiletypeFt()
        |        set tabstop=4 shiftwidth=4 expandtab
        |        setfiletype cs
        |    endfunction
        |    autocmd BufNewFile,BufRead *.fs call SetupFiletypeFt()
        |augroup END
*/*)




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
                    
                elif externalTranscriberInvoked then
                    match wordValue with
                    | "§transcribe:stop" ->  // Separated from voice recog, to ensure e.g. "finland" doesn't undeafen.
                        closeTranscriber() |> ignore
                        externalTranscriberInvoked <- false
                        promptRecognized(res); promptKeystroke(wordValue); promptMode("started listening"); promptClose()
                        speak "Stopped transcribing."
                    | _ ->
                        promptInaction("ignoring"); promptExtra("Say \"finland\" to finish transcribing and unpause VimVoice."); promptCloseMuted()
                    
                else

                    // Announce recognized input
                    promptRecognized(res) 
                    if wordValue.StartsWith("§")  then promptKeystroke(wordValue)  // Because non meta-actions are parsed before printing
                    if not settings.DontSayAction then speak res.Text
                    
                    // Check for meta-action
                    if wordValue.StartsWith("§") then
                        match wordValue.[1..] with
                        | "voicerecog:stop" ->
                            voiceRecognitionPaused <- true
                            promptMode("stopped listening"); promptExtra( "Say \"begin-hearing\" to resume.")
                            speak "Paused voice recognition, say begin-hearing to continue."
                        | "transcribe:start" | "wizard:summon_with_scribe" ->
                            externalTranscriberInvoked <- true
                            invokeTranscriber() |> ignore
                            promptMode("stopped listening"); promptExtra( "Say \"finish transcribing\" to resume.")
                            speak "Started transcribing, say finish transcribing to continue."
                            if wordValue = "§wizard:summon_with_scribe" then openAssistantProgram () |> Async.RunSynchronously
                        | "repeatkeystroke"            ->
                            // Note: Could add a Sequence option to put numbers before this command, and for-loop repeat it, but that feels too much, VIM more than covers that need.
                            //       If its added in the future, make sure to only catch "\d\+ last", and not e.g. catch "\d\+ last another command with the ward last in it here".
                            promptKeystroke(lastKeystroke)
                            SendKeys.SendWait(lastKeystroke)
                            promptExtra("Repeated VimVoice's last keystroke")
                        | "wizard:summon_scribelessly" -> openAssistantProgram () |> Async.RunSynchronously
                        | "chain:clear"                -> queuedPrepend <- "";      promptExtra("Cleared the keystroke modifier queue.")
                        | "chain:ctrl"                 -> queuedPrepend <- "\CTRL"; promptExtra("Queued CTRL for the next keystroke")
                        | "chain:shift"                -> queuedPrepend <- "\SHIFT";promptExtra("Queued SHIFT for the next keystroke")
                        | "chain:alt"                  -> queuedPrepend <- "\ALT";  promptExtra("Queued ALT for the next keystroke")
                        | "print:normal-mode"          -> printEntireGrammarList(normalMode)
                        | "print:insert-mode"          -> printEntireGrammarList(insertMode)
                        | "print:windows-mode"         -> printEntireGrammarList(windowsMode)
                        | "print:visual-mode"          -> printEntireGrammarList(visualMode)
                        | "print:universal"            -> printEntireGrammarList([universalCommands])
                        | "print:help"                 ->
                            promptExtra("Say \"printout {insert/windows/visual/universal} mode\" to see all available actions.")
                            speak "To see all available actions, say \"printout, mode-name, mode\", with one of the modes insert, normal, windows, visual or universal. E.g. \"printout normal mode\"."
                        | "vol:+"                      -> synth.Volume <- if (synth.Volume+volumeStep) <= 100 then (synth.Volume+volumeStep) else 100
                        | "vol:-"                      -> synth.Volume <- if (synth.Volume-volumeStep) >= 0   then (synth.Volume-volumeStep) else 0
                        | "vol:0"                      -> synth.Volume <- 0
                        | "vol:100"                    -> synth.Volume <- 100
                        | "vol:unmute"                 -> synth.Volume <- settings.DefaultFeedbackVolume
                        | "speech:faster"              -> synth.Rate <- if synth.Rate <  10 then synth.Rate + 1 else synth.Rate
                        | "speech:slower"              -> synth.Rate <- if synth.Rate > -10 then synth.Rate - 1 else synth.Rate
                        | "open:settings"              -> openExternalProgramWithArgs settingsFilePath ""
                        | _ -> 
                            promptInaction("not implemented")
                            promptExtra("Unrecognized command: "+wordValue) 
                            promptCloseMuted()

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
                    let mutable alts = ""
                    for alt in res.Alternates do
                        alts <- alts + "\n  " + alt.Text 
                        // Only doing if voice recog isn't paused, as it can log some speech. More important than this convenience.
                    promptExtra("(Did you mean: "+alts+")")  
                
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
