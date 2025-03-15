open System
open System.IO
open System.Speech.Synthesis
open System.Speech.Recognition
open System.Windows.Forms
let synth = new SpeechSynthesizer()
synth.Rate <- 2
synth.SelectVoiceByHints(VoiceGender.Female)

let switchGrammar grammar =
    reco.RecognizeAsyncCancel()
    reco.UnloadAllGrammars()
    List.iter (fun g -> reco.LoadGrammar(new Grammar(speechGrammar g))) grammar
    modeKeys := grammarsToWordKeys grammar
    reco.RecognizeAsync(RecognizeMode.Multiple)

let speak (text : string) =
    reco.RecognizeAsyncStop() // TODO: this is so speech doesn't get recognized!
    synth.Speak text |> ignore
    reco.RecognizeAsync(RecognizeMode.Multiple) // TODO: This causes about 1/2 sec. delay

reco.SpeechRecognized.Add(fun a ->
    let res = a.Result
    if res <> null && res.Confidence > 0.f then
        if res.Confidence > 0.85f then
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
Console.Clear()

switchGrammar normalMode
Console.ForegroundColor <- ConsoleColor.White
printfn "Welcome to VimSpeak"
printf "\n\n> "

let rec eatKeys () = Console.ReadKey(true) |> ignore; eatKeys ()
eatKeys ()

// TODO: Number parsing is broken (e.g. "nine thousand ninety nine" -> 90099)
// TODO: Registers as a separate phrase? Why does GrammarBuilder barf?
