module Variables
open System.Speech.Recognition
open System.Speech.Synthesis
open System
// open Windows.Media.SpeechRecognition

open Types 
open Settings
let settings = loadSettings()
let mode     = ref Normal

let reco     = new SpeechRecognitionEngine()
try          reco.SetInputToDefaultAudioDevice()
with _ ->    failwith "No default audio device. Connect a microphone and make sure its enabled and not muted in audio settings, or search the internet for how to enable your laptops microphone."

let ctags = None 
let synth = new SpeechSynthesizer()
let ones =
    Choice [
        Word ("one",   "1", None)
        Word ("two",   "2", None)
        Word ("three", "3", None)
        Word ("four",  "4", None)
        Word ("five",  "5", None)
        Word ("six",   "6", None)
        Word ("seven", "7", None)
        Word ("eight", "8", None)
        Word ("nine",  "9", None)]

let count =
    let teens =
        Choice [
            ones
            Word ("ten",      "10", None)
            Word ("eleven",   "11", None)
            Word ("twelve",   "12", None)
            Word ("thirteen", "13", None)
            Word ("fourteen", "14", None)
            Word ("fifteen",  "15", None)
            Word ("sixteen",  "16", None)
            Word ("seventeen","17", None)
            Word ("eighteen", "18", None)
            Word ("nineteen", "19", None)]
    let tens =
        Choice [
            Word ("twenty",  "20", None)
            Word ("thirty",  "30", None)
            Word ("fourty",  "40", None)
            Word ("fifty",   "50", None)
            Word ("sixty",   "60", None)
            Word ("seventy", "70", None)
            Word ("eighty",  "80", None)
            Word ("ninety",  "90", None)]
    let hundreds = Sequence [teens; Word ("hundred", "00", None)]
    let thousands = Sequence [Choice [teens; hundreds]; Word ("thousand", "000", None)]
    Sequence [Optional thousands; Optional hundreds; Optional tens; Optional teens]

let letter =
    Choice [
        Word ("a",         "a", None)
        Word ("b",         "b", None)
        Word ("c",         "c", None)
        Word ("d",         "d", None)
        Word ("e",         "e", None)
        Word ("f",         "f", None)
        Word ("g",         "g", None)
        Word ("h",         "h", None)
        Word ("i",         "i", None)
        Word ("j",         "j", None)
        Word ("k",         "k", None)
        Word ("l",         "l", None)
        Word ("m",         "m", None)
        Word ("n",         "n", None)
        Word ("o",         "o", None)
        Word ("p",         "p", None)
        Word ("q",         "q", None)
        Word ("r",         "r", None)
        Word ("s",         "s", None)
        Word ("t",         "t", None)
        Word ("u",         "u", None)
        Word ("v",         "v", None)
        Word ("w",         "w", None)
        Word ("x",         "x", None)
        Word ("y",         "y", None)
        Word ("z",         "z", None)
        Word ("capital-a", "A", None)
        Word ("capital-b", "B", None)
        Word ("capital-c", "C", None)
        Word ("capital-d", "D", None)
        Word ("capital-e", "E", None)
        Word ("capital-f", "F", None)
        Word ("capital-g", "G", None)
        Word ("capital-h", "H", None)
        Word ("capital-i", "I", None)
        Word ("capital-j", "J", None)
        Word ("capital-k", "K", None)
        Word ("capital-l", "L", None)
        Word ("capital-m", "M", None)
        Word ("capital-n", "N", None)
        Word ("capital-o", "O", None)
        Word ("capital-p", "P", None)
        Word ("capital-q", "Q", None)
        Word ("capital-r", "R", None)
        Word ("capital-s", "S", None)
        Word ("capital-t", "T", None)
        Word ("capital-u", "U", None)
        Word ("capital-v", "V", None)
        Word ("capital-w", "W", None)
        Word ("capital-x", "X", None)
        Word ("capital-y", "Y", None)
        Word ("capital-z", "Z", None)
        Word ("space",     " ", None)
   ]

let symbol =
    Choice [
        Word ("bang",                   "!",  None)
        Word ("exclamation-point",      "!",  None)
        Word ("at",                     "@",  None)
        Word ("pound",                  "#",  None)
        Word ("dollar",                 "$",  None)
        Word ("percent",                "%",  None)
        Word ("caret",                  "^",  None)
        Word ("ampersand",              "&",  None)
        Word ("star",                   "*",  None)
        Word ("asterisk",               "*",  None)
        Word ("underscore",             "_",  None)
        Word ("dash",                   "-",  None)
        Word ("minus",                  "-",  None)
        Word ("plus",                   "+",  None)
        Word ("equals",                 "=",  None)
        Word ("pipe",                   "|",  None)
        Word ("backslash",              "\\", None)
        Word ("tilde",                  "~",  None)
        Word ("backtick",               "`",  None)
        Word ("tick",                   "'",  None)
        Word ("single-quote",           "'",  None)
        Word ("appostrophe",            "'",  None)
        Word ("quote",                  "\"", None)
        Word ("quotes",                 "\"", None)
        Word ("double-quote",           "\"", None)
        Word ("double-quotes",          "\"", None)
        Word ("comma",                  ",",  None)
        Word ("less-than",              "<",  None)
        Word ("period",                 ".",  None)
        Word ("dot",                    ".",  None)
        Word ("greater-than",           ">",  None)
        Word ("slash",                  "/",  None)
        Word ("question-mark",          "?",  None)
        Word ("colon",                  ":",  None)
        Word ("semi-colon",             ";",  None)
        Word ("opening-angle-bracket",  "<",  None)
        Word ("opening-curly-brace",    "{",  None)
        Word ("opening-brace",          "{",  None)
        Word ("opening-square-bracket", "[",  None)
        Word ("opening-bracket",        "[",  None)
        Word ("opening-parenthesis",    "(",  None)
        Word ("opening-paren",          "(",  None)
        Word ("closing-angle-bracket",  ">",  None)
        Word ("closing-curly-brace",    "}",  None)
        Word ("closing-brace",          "}",  None)
        Word ("closing-square-bracket", "]",  None)
        Word ("closing-bracket",        "]",  None)
        Word ("closing-parenthesis",    ")",  None)
        Word ("closing-paren",          ")",  None)
        Word ("left-angle-bracket",     "<",  None)
        Word ("left-curly-brace",       "{",  None)
        Word ("left-brace",             "{",  None)
        Word ("left-square-bracket",    "[",  None)
        Word ("left-bracket",           "[",  None)
        Word ("left-parenthesis",       "(",  None)
        Word ("left-paren",             "(",  None)
        Word ("right-angle-bracket",    ">",  None)
        Word ("right-curly-brace",      "}",  None)
        Word ("right-brace",            "}",  None)
        Word ("right-square-bracket",   "]",  None)
        Word ("right-bracket",          "]",  None)
        Word ("right-parenthesis",      ")",  None)
        Word ("right-paren",            ")",  None)]

let char = Choice [letter; symbol]

let register =
    Sequence [
        Word ("register", "\"", Some "prompt: register %s")
        Choice [
            ones
            letter
            Word ("unnamed",      "\"", None)
            Word ("black-hole",   "_",  None)
            Word ("small-delete", "-",  None)
            Word ("colon",        ":",  None)
            Word ("dot",          ".",  None)
            Word ("percent",      "%",  None)
            Word ("pound",        "#",  None)
            Word ("expression",   "=",  None)
            Word ("star",         "*",  None)
            Word ("plus",         "+",  None)
            Word ("tilde",        "~",  None)
            Word ("last-search",  "/",  None)]]

let motion =
    Sequence [
        Optional ones // TODO: support full counts - why does GrammarBuilder freak?
        Choice [
            Word ("back",                           "b",        None)
            Word ("back-word",                      "b",        None)
            Word ("big-back",                       "B",        None)
            Word ("big-back-word",                  "B",        None)
            Word ("end",                            "e",        None)
            Word ("big-end",                        "E",        None)
            Word ("back-end",                       "ge",       None)
            Word ("back-big-end",                   "gE",       None)
            Word ("left",                           "h",        None)
            Word ("down",                           "j",        None)
            Word ("up",                             "k",        None)
            Word ("right",                          "l",        None)
            Word ("next",                           "n",        None)
            Word ("next-reversed",                  "N",        None)
            Word ("previous",                       "N",        None)
            Word ("column-zero",                    "0",        None)
            Word ("column",                         "|",        None)
            Word ("start-of-line",                  "^",        None)
            Word ("end-of-line",                    "$",        None)
            Word ("search-under-cursor",            "*",        None)
            Word ("search-under-cursor-reversed",   "#",        None)
            Word ("again",                          ";",        None)
            Word ("again-reversed",                 ",",        None)
            Word ("down-sentence",                  ")",        None)
            Word ("up-sentence",                    "(",        None)
            Word ("down-paragraph",                 "}",        None)
            Word ("up-paragraph",                   "{",        None)
            Word ("start-of-next-section",          "]]",       None)
            Word ("start-of-previous-section",      "[[",       None)
            Word ("end-of-next-section",            "][",       None)
            Word ("end-of-previous-section",        "[]",       None)
            Word ("matching",                       "%",        None)
            Word ("down-line",                      "+",        None)
            Word ("up-line",                        "-",        None)
            Word ("first-character",                "_",        None)
            Word ("cursor-home",                    "H",        None)
            Word ("cursor-middle",                  "M",        None)
            Word ("cursor-last",                    "L",        None)
            Word ("start-of-document",              "gg",       None)
            Word ("end-of-document",                "G",        None)
            Word ("retrace-movements",              "<C-o>",    None)
            Word ("retrace-movements-forward",      "<C-i>",    None)
            Sequence [Word ("jump-to-mark",    "'", None); char]
            Sequence [Word ("find",            "f", None); char]
            Sequence [Word ("find-reversed",   "F", None); char]
            Sequence [Word ("till",            "t", None); char]
            Sequence [Word ("till-reversed",   "T", None); char]
            Sequence [Word ("search",          "/", Some "search"); Dictation]
            Sequence [Word ("search-reversed", "?", Some "search"); Dictation]
            Choice [Word ("word", "w", None); Word ("words", "w", None)]
            Choice [Word ("big-word", "W", None); Word ("big-words", "W", None)]]]

let jump =
    Sequence [
        Choice [
            Word ("jump-to-line-of",      "'", None)
            Word ("jump-to-character-of", "`", None)]
        Choice [
            Word ("start-of-last-selection", "<",  None)
            Word ("end-of-last-selection",   ">",  None)
            Word ("latest-jump",             "'",  None)
            Word ("last-exit",               "\"", None)
            Word ("last-insert",             "^",  None)
            Word ("last-change",             ".",  None)]]

let line = 
    Sequence [
        count; 
        Choice [
            Word ("lines",   "G", None)
            Word ("percent", "%", None)
        ]
    ]
let lineNum = 
    Sequence [
        Choice [
           Word ("line-number", ":", Some "command")
           Word ("line",        ":", Some "command")
        ]
        count
    ]

let command =
    Choice [
        Word ("change",               "c",  Some "insert")
        Word ("delete",               "d",  Some "normal")
        Word ("indent",               ">",  None)
        Word ("unindent",             "<",  None)
        Word ("join",                 "J",  None)
        Word ("format",               "=",  None)
        Word ("put",                  "p",  None)
        Word ("paste",                "p",  None)
        Word ("paste-before",         "P",  None)
        Word ("undo",                 "u",  None)
        Word ("yank",                 "y",  Some "normal")
        Word ("copy",                 "y",  Some "normal")
        Word ("big-letters",          "gU", Some "normal")
        Word ("capitalize-letters",   "gU", Some "normal")
        Word ("small-letters",        "gu", Some "normal")
        Word ("uncapitalize-letters", "gu", Some "normal")
    ]

let textObject =
    Sequence [
        //Optional ones // TODO: support full counts
        Optional (
            Choice [
                Word ("two",   "2", None)
                Word ("three", "3", None)
                Word ("four",  "4", None)
                Word ("five",  "5", None)
                Word ("six",   "6", None)
                Word ("seven", "7", None)
                Word ("eight", "8", None)
                Word ("nine",  "9", None)])
        Choice [
            Word ("inner",  "i", None)
            Word ("around", "a", None)]
        Choice [
            Word ("word",            "w",  None)
            Word ("big-word",        "W",  None)
            Word ("block",           "b",  None)
            Word ("big-block",       "B",  None)
            Word ("quotes",          "\"", None)
            Word ("double-quotes",   "\"", None)
            Word ("single-quotes",   "'",  None)
            Word ("parens",          "(",  None)
            Word ("parenthesis",     "(",  None)
            Word ("angle-brackets",  "<",  None)
            Word ("curly-braces",    "{",  None)
            Word ("braces",          "{",  None)
            Word ("square-brackets", "[",  None)
            Word ("brackets",        "[",  None)
            Word ("backticks",       "`",  None)
            Word ("sentence",        "s",  None)
            Word ("paragraph",       "p",  None)
            Word ("tag-block",       "t",  None)]]

let escape = Word ("escape", "<esc>", Some "normal")

let surroundTarget =
    Choice [ // TODO: share with text objects?
        symbol
        Word ("stars",                 "*",     None)
        Word ("asterisks",             "*",     None)
        Word ("word",                  "w",     None)
        Word ("big-word",              "W",     None)
        Word ("block",                 "b",     None)
        Word ("big-block",             "B",     None)
        Word ("quotes",                "\"",    None)
        Word ("double-quotes",         "\"",    None)
        Word ("single-quotes",         "'",     None)
        Word ("loose-parens",          "(",     None)
        Word ("loose-parenthesis",     "(",     None)
        Word ("loose-angle-brackets",  "<",     None)
        Word ("loose-curly-braces",    "{",     None)
        Word ("loose-braces",          "{",     None)
        Word ("loose-square-brackets", "[",     None)
        Word ("loose-brackets",        "[",     None)
        Word ("tight-parens",          ")",     None)
        Word ("tight-parenthesis",     ")",     None)
        Word ("tight-angle-brackets",  ">",     None)
        Word ("tight-curly-braces",    "}",     None)
        Word ("tight-braces",          "}",     None)
        Word ("tight-square-brackets", "]",     None)
        Word ("tight-brackets",        "]",     None)
        Word ("parens",                ")",     None)
        Word ("parenthesis",           ")",     None)
        Word ("angle-brackets",        ">",     None)
        Word ("curly-braces",          "}",     None)
        Word ("braces",                "}",     None)
        Word ("square-brackets",       "]",     None)
        Word ("brackets",              "]",     None)
        Word ("backticks",             "`",     None)
        Word ("sentence",              "s",     None)
        Word ("paragraph",             "p",     None)
        Word ("tags",                  "t",     None)
        Word ("h1-tags",               "<h1>",  None)
        Word ("h2-tags",               "<h2>",  None)
        Word ("div-tags",              "<div>", None)
        Word ("bold-tags",             "<b>",   None)
    ]

let commentary =
    Choice [
        Word ("comment",   @"\\", None)
        Word ("uncomment", @"\\", None)

let normalMode =
    let countedCommand =
        Sequence [
            Optional count
            Choice [
                Sequence [command]
                Sequence [command; motion]
                Sequence [command; textObject]
                Sequence [command; jump]
                Word ("format-line",    "==",  None)
                Word ("delete-line",    "dd",  None)
                Word ("yank-line",      "Y",   None)
                Word ("copy-line",      "Y",   None)]]
    let countedAction =
        Sequence [
            Optional count
            Choice [
                Word ("after",                    "a",     Some "insert")
                Word ("append",                   "a",     Some "insert")
                Word ("after-line",               "A",     Some "insert")
                Word ("append-line",              "A",     Some "insert")
                Word ("insert",                   "i",     Some "insert")
                Word ("insert-before-line",       "I",     Some "insert")
                Word ("insert-column-zero",       "gI",    Some "insert")
                Word ("open",                     "o",     Some "insert")
                Word ("open-below",               "o",     Some "insert")
                Word ("open-above",               "O",     Some "insert")
                Word ("substitute",               "s",     Some "insert")
                Word ("substitute-line",          "S",     Some "insert")
                Word ("undo",                     "u",     None)
                Word ("undo-line",                "U",     None)
                Word ("redo",                     "<C-r>", None)
                Word ("erase",                    "x",     None)
                Word ("erase-reversed",           "X",     None)
                Word ("erase-back",               "X",     None)
                Word ("put",                      "p",     None)
                Word ("paste",                    "p",     None)
                Word ("put-before",               "P",     None)
                Word ("paste-before",             "P",     None)
                Word ("put-above",                "P",     None)
                Word ("paste-above",              "P",     None)
                Word ("repeat",                   ".",     None)
                Word ("scroll-up",                "<C-y>", None)
                Word ("scroll-down",              "<C-e>", None)
                Word ("page-down",                "<C-f>", None)
                Word ("page-up",                  "<C-b>", None)
                Word ("half-page-down",           "<C-d>", None)
                Word ("half-page-up",             "<C-u>", None)
                Word ("indent-line",              ">>",    None)
                Word ("unindent-line",            "<<",    None)
                Word ("toggle-case",              "~",     None)
                Word ("comment-line",             @"\\\",  None)
                Word ("comment-lines",            @"\\\",  None)
                Word ("uncomment-line",           @"\\\",  None)
                Word ("uncomment-lines",          @"\\\",  None)
                Word ("scroll-left",              "zh",    None)
                Word ("scroll-right",             "zl",    None)
                Word ("scroll-half-screen-left",  "zH",    None)
                Word ("scroll-half-screen-right", "zL",    None)
                Word ("scroll-start",             "zs",    None)
                Word ("scroll-end",               "ze",    None)
                Word ("play-again",               "@@",    None)
                Sequence [Word ("play-macro", "@", None); char]]]
    let nonCountedAction =
        Choice [
            Sequence [commentary; motion]
            Sequence [Word ("align-on", ":Tab/", Some "command"); char] // TODO: complete Tabular support
            Sequence [Word ("mark",           "m",   Some "prompt: marked %s"); char]
            Sequence [Word ("record-macro",   "q",   Some "prompt: recording %s"); char]
            Sequence [Word ("replace",        "r",   None); char]
            Word ("display-current-line-number", "<C-g>",     None)
            Word ("delete-remaining-line", "D",     None)
            Word ("change-remaining-line", "C",     Some "insert")
            Word ("change-line",           "cc",    Some "insert")
            Word ("duplicate-line",        "yyp",   None)
            Word ("swap-characters",       "xp",    None)
            Word ("swap-words",            "dwwP",  None)
            Word ("swap-lines",            "ddp",   None)
            Word ("stop-recording",        "q",     Some "prompt: stopped")
            Word ("replace-mode",          "R",     Some "replace")
            Word ("overwrite",             "R",     Some "replace")
            Word ("visual",                "v",     Some "visual")
            Word ("select",                "v",     Some "visual")
            Word ("visual-line",           "V",     Some "visual")
            Word ("select-line",           "V",     Some "visual")
            Word ("visual-all",            "ggVG",  Some "visual")
            Word ("select-all",            "ggVG",  Some "visual")
            Word ("visual-block",          "<C-v>", Some "visual")
            Word ("select-block",          "<C-v>", Some "visual")
            Word ("scroll-top",            "zt",    None)
            Word ("scroll-middle",         "zz",    None)
            Word ("scroll-botton",         "zb",    None)
            Word ("scroll-top-reset-cursor",    "z<enter>", None)
            Word ("scroll-middle-reset-cursor", "z.",        None)
            Word ("scroll-botton-reset-cursor", "z-",        None)] 
    let selectMotion = Sequence [Word ("select", "v", Some "visual"); Choice [motion; jump]]
    let selectTextObject = Sequence [Word ("select", "v", Some "visual"); textObject]
    let commandLine =
        Choice [
            Sequence [Word ("edit", ":e", Some "command"); Dictation]
            Word ("save", ":w", Some "command")
            Word ("quit", ":q", Some "command")
            Word ("save-and-quit", ":x", Some "command")
            Word ("quit-without-saving", ":q!", Some "command")
            Word ("set-number", ":set nu", Some "command")
            Word ("set-no-number", ":set nonu", Some "command")
            Word ("set-highlight-search", ":set hls", Some "command")
            Word ("set-no-highlight-search", ":set nohls", Some "command")]
    let globalReplace =
        Choice [
            Sequence [
                Word ("change-occurrences-of", ":%s/", Some "command")
                Dictation
                Word ("into", "/", None) // "to" is used by surround.vim
                Dictation
                Word ("globally", "/g", None) // TODO: optional
                Optional (Word ("ignore-case", "i", None))
                Optional (Word ("confirm", "c", None))]
            Choice [
                Word ("yes", "y", None)
                Word ("no", "n", None)]]
    let surround =
        Choice [
            Sequence [
                Choice [
                    Choice [Word ("surround", "ys", None); Word ("you-surround", "ys", None)]
                    Choice [Word ("surround-and-indent", "yS", None); Word ("you-surround-and-indent", "yS", None)]]
                Choice [motion; textObject]
                Word ("with", "", None)
                surroundTarget]
            Sequence [
                Choice [
                    Word ("surround-line-with", "yss", None)
                    Word ("you-surround-line-with", "yss", None)]
                surroundTarget]
            Sequence [
                Choice [
                    Word ("surround-and-indent-line-with", "ySS", None)
                    Word ("you-surround-and-indent-line-with", "ySS", None)]
                surroundTarget]
            Sequence [
                Word ("delete", "d", None)
                Word ("surrounding", "s", None)
                surroundTarget]
            Sequence [
                Word ("change", "c", None)
                Word ("surrounding", "s", None)
                surroundTarget
                Word ("to", "", None)
                surroundTarget]]
    [register; motion; jump; countedCommand; countedAction; nonCountedAction; selectMotion; selectTextObject; commandLine; line; lineNum; escape; globalReplace; surround]

let visualMode =
    let visualCommand =
        Choice [
            command
            Word ("opposite", "o", None)
            Word ("insert", "I", Some "insert")
            commentary]
    let surround =
        Sequence [
            Choice [
                Choice [
                    Word ("surround-with", "S", None)
                    Word ("you-surround-with", "S", None)]
                Choice [
                    Word ("surround-and-indent-with", "gS", None)
                    Word ("you-surround-and-indent-with", "gS", None)]]
            surroundTarget]
    [visualCommand; motion; jump; textObject; escape; surround]

let insertMode =
    let insertCommands =
        Choice [
            Word ("undo", "(^o)u",              None)
            Word ("complete", "<C-n>",          None)
            Word ("complete-next", "<C-n>",     None)
            Word ("complete-previous", "<C-p>", None)
            Word ("space", "<space>",           None)
            Word ("backspace", "<backspace>",   None)
            Word ("tab", "<tab>",               None)
            Word ("enter", "<enter>",           None)
            Word ("return", "<enter>",          None)]
    [Dictation; insertCommands; escape]
    

