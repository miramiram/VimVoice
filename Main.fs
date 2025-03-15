open System
open System.IO
open System.Speech.Synthesis
open System.Speech.Recognition
open System.Windows.Forms
let rec speechGrammar = function
    | Word (say, _, Some value) ->
        let g = new GrammarBuilder(say)
        g.Append(new SemanticResultValue(value.ToString()))
        g
    | Word (say, _, None) -> new GrammarBuilder(say)
    | Optional g -> new GrammarBuilder(speechGrammar g, 0, 1)
    | Sequence gs ->
        let builder = new GrammarBuilder()
        List.iter (fun g -> builder.Append(speechGrammar g)) gs
        builder
    | Choice cs -> new GrammarBuilder(new Choices(List.map speechGrammar cs |> Array.ofList))
    | Dictation ->
        let dict = new GrammarBuilder()
        dict.AppendDictation()
        let spelling = new GrammarBuilder()
        spelling.AppendDictation("spelling")
        new GrammarBuilder(new Choices(dict, spelling))


let modeKeys = ref Map.empty

let recoToKeys (reco : RecognitionResult) =
    let concat (keys : string) (word : RecognizedWordUnit) =
        match Map.tryFind word.Text !modeKeys with
        | Some (m : string) ->
            if m.Length > 0 && Char.IsDigit m.[0] then
                let len = keys.Length
                if len > 0 && keys.[len - 1] = '0' then
                    keys.Substring(0, len - m.Length) + m
                else keys + m
            else keys + m
        | None -> keys + word.Text
    if reco = null then "" else Seq.fold concat "" reco.Words

let fixKeys (keys : string) = // TODO: this is silly!
    keys.Replace("{", "<left-brace>") // re-replaced with escaped brace below
        .Replace("}", "<right-brace>") // re-replaced with escaped brace below
        .Replace("(", "{(}")
        .Replace(")", "{)}")
        .Replace("~", "{~}")
        .Replace("+", "{+}")
        .Replace("^", "{^}")
        .Replace("%", "{%}")
        .Replace("!", "{!}")
        .Replace("<esc>", "{esc}")
        .Replace("<space>", " ")
        .Replace("<C-b>", "^b")
        .Replace("<C-d>", "^d")
        .Replace("<C-e>", "^e")
        .Replace("<C-f>", "^f")
        .Replace("<C-g>", "^g")
        .Replace("<C-h>", "{bs}")
        .Replace("<C-i>", "^i")
        .Replace("<C-n>", "^n")
        .Replace("<C-o>", "^o")
        .Replace("<C-p>", "^p")
        .Replace("<C-r>", "^r")
        .Replace("<C-u>", "^u")
        .Replace("<C-v>", "^v")
        .Replace("<C-y>", "^y")
        .Replace("<C-o>u", "(^o)u")
        .Replace("<tab>", "{tab}")
        .Replace("<enter>", "{enter}")
        .Replace("<left-brace>", "{{}")
        .Replace("<right-brace>", "{}}")

let insertKeys (keys : string) =
    if keys = "escape" then "<esc>" // HACK
    elif keys = "undo" then "<C-o>u" // HACK
    elif keys = "space" then "<space>" // HACK
    elif keys = "backspace" then "<C-h>" // HACK
    elif keys = "tab" then "<tab>" // HACK
    elif keys = "enter" || keys = "return" then "<enter>" // HACK
    elif keys.StartsWith "search " then "/" + keys.Substring 7
    elif keys.StartsWith "search-reversed " then "?" + keys.Substring 16
    else keys

let reco = new SpeechRecognitionEngine()
try
    reco.SetInputToDefaultAudioDevice()
with _ -> failwith "No default audio device! Plug in a microphone, man."

let grammarsToWordKeys gs =
    let rec grammarToWordKeys (map : Map<string,string>) = function
        | Word (w, ks, _) -> Map.add w ks map
        | Optional g -> grammarToWordKeys map g
        | Sequence gs | Choice gs -> List.fold grammarToWordKeys map gs
        | Dictation -> map
    List.fold grammarToWordKeys Map.empty gs

let mode = ref Normal

let ctagsGrammar =
    match ctags with
    | Some file ->
        printfn "Loading ctags..."
        file
        |> File.ReadLines
        |> Seq.map (fun x -> x.Substring(0, x.IndexOf '\t')) // parse tag names
        |> Set.ofSeq // distinct set
        |> Set.toList
        |> List.map (fun w -> Word (w, w, None))
        |> Choice
        |> speechGrammar
        |> fun s -> new Grammar(s)
        |> Some
    | None -> None

let tests () =
    printfn "Running tests..."
    let test mode phrase expected =
        reco.UnloadAllGrammars()
        List.iter (fun g -> reco.LoadGrammar(new Grammar(speechGrammar g))) mode
        modeKeys := grammarsToWordKeys mode
        let res = reco.EmulateRecognize phrase
        let keys = recoToKeys res
        printfn "'%s' -> %s" phrase keys
        if keys <> expected then
            Console.ForegroundColor <- ConsoleColor.Red
            if res = null then printfn "FAILURE: UNRECOGNIZED (expected: %s)" expected
            else printfn "FAILURE: %s (%f) -> %s (expected: %s)" res.Text res.Confidence keys expected
    test normalMode "word" "w"
    test normalMode "one line" "1G"
    test normalMode "two line" "2G"
    test normalMode "ten line" "10G"
    test normalMode "fifteen line" "15G"
    test normalMode "fifty six line" "56G"
    test normalMode "one hundred line" "100G"
    test normalMode "one hundred five line" "105G"
    test normalMode "seven hundred thirty two line" "732G"
    test normalMode "twelve hundred six line" "1206G"
    test normalMode "six thousand line" "6000G"
    test normalMode "six thousand ninety two line" "6092G"
    test normalMode "nine hundred ninety nine line" "999G"
    test normalMode "register a" "\"a" // TODO: should work as part of command
    test normalMode "line" "G"
    test normalMode "five words" "5w"
    test normalMode "six back" "6b"
    test normalMode "6 back" "6b"
    test normalMode "90 line" "90G"
    test normalMode "ninety nine line" "99G"
    test normalMode "900 line" "900G"
    test normalMode "987 line" "987G"
    test normalMode "select inner quotes" "vi\""
    test normalMode "escape" "<esc>"
    test normalMode "find left-square-bracket" "f["
    test normalMode "find closing-square-bracket" "f]"
    test normalMode "find star" "f*"
    test normalMode "5 after" "5a"
    test normalMode "select word" "vw"
    test normalMode "select two words" "v2w"
    test normalMode "visual" "v"
    test normalMode "line-number one hundred twenty three" ":123"
    test normalMode "change-occurrences-of foo into bar globally ignore-case confirm" ":%s/foo/bar/gic"
    test normalMode "surround word with stars" "ysw*"
    test normalMode "you-surround word with stars" "ysw*"
    test normalMode "surround three words with quotes" "ys3w\""
    test normalMode "surround-and-indent three words with quotes" "yS3w\""
    test normalMode "surround find x with braces" "ysfx}"
    test normalMode "surround around big-word with brackets" "ysaW]"
    test normalMode "delete surrounding quotes" "ds\""
    test normalMode "delete surrounding parenthesis" "ds)"
    test normalMode "delete surrounding tags" "dst"
    test normalMode "change surrounding brackets to parens" "cs])"
    test normalMode "change surrounding quotes to h1-tags" "cs\"<h1>"
    test normalMode "surround-line-with stars" "yss*"
    test normalMode "surround-and-indent-line-with stars" "ySS*"
    test normalMode "surround inner word with pipe" "ysiw|"
    test normalMode "comment-line" @"\\\"
    test normalMode "uncomment-line" @"\\\"
    test normalMode "three comment-line" @"3\\\"
    test normalMode "comment three down" @"\\3j"
    test normalMode "uncomment six up" @"\\6k"
    test normalMode "align-on equals" ":Tab/="
    test normalMode "delete word" "dw"
    test visualMode "two words" "2w"
    test visualMode "around parens" "a)"
    test visualMode "three around parens" "3a)"
    test visualMode "inner quotes" "i\""
    test visualMode "opposite" "o"
    test visualMode "surround-with stars" "S*"
    test visualMode "you-surround-with stars" "S*"
    test visualMode "surround-with quotes" "S\""
    test visualMode "surround-and-indent-with quotes" "gS\""
    test visualMode "surround-with braces" "S}"
    test visualMode "surround-with brackets" "S]"
    test visualMode "comment" @"\\"
    test visualMode "uncomment" @"\\"
//tests ()

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
