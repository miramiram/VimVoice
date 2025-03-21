module Functions

open System
open System.IO
open System.Speech.Synthesis
open System.Speech.Recognition
open System.Windows.Forms
open System.Diagnostics
open System.Threading.Tasks
    

open Types
open Variables




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


// - Replace the special keys <> with \{ and \}, so they don't need replacements here.
let handleSpecialChars (keys : string) = 
    keys.Replace("\{",      "LBR") // Specially interpreted strings           
        .Replace("\}",      "RBR") // Rationale: Making every character literal unless prepended with backslash, for predictability in UI and code, due to Windows interpreting many characters automatically.
        .Replace("\CTRL",   "WINCTRL")
        .Replace("\ALT",    "WINALT")
        .Replace("\SHIFT",  "WINSHIFT")
        .Replace("\^",      "WINCTRL")
        .Replace("\+",      "WINSHIFT")
        .Replace("\%",      "WINALT")
        // Replacing the remaining braces with a placeholder, before inserting windows-interpreted braces
        .Replace("{", "<left-brace>")  
        .Replace("}", "<right-brace>") 
        // Ensuring the following aren't interpreted specially by Windows 
        .Replace("(", "{(}")
        .Replace(")", "{)}")
        .Replace("~", "{~}")
        .Replace("+", "{+}")
        .Replace("^", "{^}")
        .Replace("%", "{%}")
        .Replace("!", "{!}")
        // Re-inserting regular braces
        .Replace("<left-brace>", "{{}")
        .Replace("<right-brace>", "{}}")
        // Replacing placeholders special characters with their windows equivalent
        .Replace("LBR", "{") // Used for writing windows dotnet key codes directly, the string it uses cant contain anything replaced by the prior replacements.
        .Replace("RBR", "}")
        .Replace("WINCTRL", "^")
        .Replace("WINALT", "%")
        .Replace("WINSHIFT", "+")


let grammarsToWordKeys gs =
    let rec grammarToWordKeys (map : Map<string,string>) = function
        | Word (w, ks, _) -> Map.add w ks map
        | Optional g -> grammarToWordKeys map g
        | Sequence gs | Choice gs -> List.fold grammarToWordKeys map gs
        | Dictation -> map
    List.fold grammarToWordKeys Map.empty gs


let switchGrammar grammar =
    reco.RecognizeAsyncCancel()
    reco.UnloadAllGrammars()
    List.iter (fun g -> reco.LoadGrammar(new Grammar(speechGrammar g))) grammar
    modeKeys := grammarsToWordKeys grammar
    reco.RecognizeAsync(RecognizeMode.Multiple)


let speak (text : string) =
    if settings.DontListenWhileGivingAudioFeedbackToUser then
        reco.RecognizeAsyncStop() // this is so speech doesn't get recognized
    synth.SpeakAsync text |> ignore
    if settings.DontListenWhileGivingAudioFeedbackToUser then
        reco.RecognizeAsync(RecognizeMode.Multiple) // This causes about 1/2 sec. delay


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





let printfColor (color:ConsoleColor, printArg1:Printf.TextWriterFormat<_>, printArg2:_) =
    let originalColor = Console.ForegroundColor
    Console.ForegroundColor <- color
    printf printArg1 printArg2
    Console.ForegroundColor <- originalColor
let printfnColor (color:ConsoleColor, printArg1:Printf.TextWriterFormat<_>, printArg2:_) =
    printfColor (color, printArg1, printArg2)
    printfn "%s" ""  

let promptRecognized(result:RecognitionResult) =
    printfColor     (ConsoleColor.White, "%s", result.Text)
    printfnColor    (ConsoleColor.Gray, (Printf.TextWriterFormat<_>)" (%d%%)", ((int) (result.Confidence * (float32)100.00)))
let promptKeystroke (text:string) =
    printfnColor    (ConsoleColor.Yellow,  "  %s", text)
let promptMode      (text:string) =
    printfnColor    (ConsoleColor.Red,     "  %s", text.ToUpper())
let promptInaction  (text:string) =
    printfnColor    (ConsoleColor.Blue,    "%s", text.ToUpper())  // Not prepended with spaces as these appear inline
let promptExtra     (text:string) =
    printfnColor    (ConsoleColor.Gray,    "  %s", text)
let promptCloseMuted() =
    printfColor     (ConsoleColor.White,   "%s", "\n> ")
    Console.ForegroundColor <- ConsoleColor.White  // Returning default fg to white
let promptClose     () =
    if not settings.DontSayOk then speak "k"
    promptCloseMuted()







// Result tyoe for findMatchingWord
type MatchResult<'a> =
    | Match of GrammarAST<'a>
    | NoMatch

// Recurse through GrammarAST object to see if a Word object with a first component matching the given string exists, return MatchResult.
let rec findMatchingWord (grammar: GrammarAST<'a>) (input: string) : MatchResult<'a> =
    match grammar with
    | Word (word, _, _) as w -> 
        if word = input then Match w                                // Return the matching Word object
        else NoMatch
    | Optional innerGrammar -> findMatchingWord innerGrammar input  // Recurse 
    | Sequence grammarList ->                                       
        grammarList // Check each element in the sequence
        |> List.tryPick (fun g -> match findMatchingWord g input with Match w -> Some (Match w) | NoMatch -> None)
        |> Option.defaultValue NoMatch
    | Choice grammarList ->                                         
        grammarList // Check each choice
        |> List.tryPick (fun g -> match findMatchingWord g input with Match w -> Some (Match w) | NoMatch -> None)
        |> Option.defaultValue NoMatch
    | Dictation -> NoMatch
    | _ -> NoMatch


// Recurse through GrammarAST object to print everything
let rec printEntireGrammar (grammar: GrammarAST<'a>, indentCount: int) =
    let pre = String.replicate indentCount "|"
    printf "%s" pre
    let titleColor = ConsoleColor.Green
    match grammar with
    | Word (word_text, word_keystrokes, word_mode) as w -> 
        printfColor  (ConsoleColor.Gray,    "%s", " ")
        printfColor  (ConsoleColor.Yellow,  "%s", word_keystrokes.PadRight(20, ' '))
        printfColor  (ConsoleColor.Gray,    "%s", "<--| ")
        printfnColor (ConsoleColor.White,   "%s", word_text) 
    | Optional innerWord -> 
        printfnColor (titleColor,    "%s", "\\ Optional:")
        printEntireGrammar      (innerWord, indentCount + 1)
    | Sequence sequence -> 
        printfnColor (titleColor,    "%s", "\\ Sequence:")
        for g in sequence do
            printEntireGrammar (g,          indentCount + 1)
    | Choice choice -> 
        printfnColor (titleColor,    "%s", "\\ Choice:")
        for g in choice do
            printEntireGrammar (g,          indentCount + 1)

    | Dictation ->
        printfnColor (ConsoleColor.Yellow, "%s", " (Spoken dictation, like insert mode)")
    | _ ->
        printfnColor (ConsoleColor.Gray,   "%s", "(Unknown)")

let rec printEntireGrammarList (grammarList: GrammarAST<'a> list) =
    for grammar in grammarList do
        printEntireGrammar (grammar, 0)




let insertKeys (keys : string) =  
    if   keys.StartsWith "search " then "/" + keys.Substring 7
    elif keys.StartsWith "search-reversed " then "?" + keys.Substring 16
    else
        // Check if the string exists in any of the characters, and return the character representation if so.
        match findMatchingWord insertCommands keys with
        | Match (Word (word, conversion, _)) -> conversion
        | NoMatch  ->
            match findMatchingWord ones keys with
            | Match (Word (word, conversion, _)) -> conversion
            | NoMatch  -> keys


let closeTranscriber () =
    SendKeys.SendWait(handleSpecialChars settings.TranscriberCloseKeybinding)
    //async {
    //    do! Async.Sleep(4*1000) 
    //    SendKeys.SendWait("^{bs}^{bs}^{bs}")  // Removing the sentence that ends transcribing, e.g. "Stop transcribing. ".
    //}


let invokeTranscriber () =
    SendKeys.SendWait(handleSpecialChars settings.TranscriberOpenKeybinding)



let openExternalProgramWithArgs (path: string) (args: string) =
    try
        let processStartInfo = ProcessStartInfo(path)
        processStartInfo.UseShellExecute <- true
        processStartInfo.Arguments <- args
        let proc = Process.Start(processStartInfo)
        printfn "Program started."
    with
    | ex -> printfn "Failed to start program: %s" ex.Message


let openAssistantProgram () = async {
    openExternalProgramWithArgs settings.AssistantPathOrUrl settings.AssistantArgs
    // Wait for program to finish opening (It should be no problem to wait as speaking for windows/external transcription takes time)
    do! Async.Sleep(settings.AssistantSecondsToWaitAfterOpeningToPressTabs * 1000)
    // Press tab until inside the selection box
    for i in 1 .. settings.AssistantNumberOfTimesToPressTabAfterOpeningToSelectTheTextInputBox do
        SendKeys.SendWait("{tab}")

}

 



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

