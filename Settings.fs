module Settings

open System
open System.IO
open System.Text.Json
open System.Text.Json.Nodes
open FSharp.Reflection

// Define a type to represent your settings
type AppSettings = {
    DontListenWhileGivingAudioFeedbackToUser: bool
    DontSayOk: bool
    DontSayAction: bool

    TranscriberOpenKeybinding: string
    TranscriberCloseKeybinding: string

    AssistantPathOrUrl: string
    AssistantArgs: string
    AssistantSecondsToWaitAfterOpeningToPressTabs: int
    AssistantNumberOfTimesToPressTabAfterOpeningToSelectTheTextInputBox: int
    //UseMicrosoftOnlineSpeechRecognition: bool

    DefaultFeedbackVolume: int
    DefaultFeedbackSpeed: int

    CustomKey1: string
    CustomKey2: string
    CustomKey3: string
    CustomKey4: string
    CustomKey5: string
    CustomKey6: string
    CustomKey7: string
    CustomKey8: string
    CustomKey9: string
}

// Path to the settings file
let settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VimVoice", "settings.json")

// Function to create default settings
let createDefaultSettings () = {
    DefaultFeedbackVolume   = 60
    DefaultFeedbackSpeed    = 4
    DontListenWhileGivingAudioFeedbackToUser = false
    DontSayOk       = true
    DontSayAction   = false
     
    TranscriberOpenKeybinding  = "WINALTspace"
    TranscriberCloseKeybinding = "WINALTspace"
    
    AssistantPathOrUrl  = "https://chat.deepseek.com" //Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)+"\\scoop\\apps\\firefoxpwa\\current\\firefoxpwa.exe"
    AssistantArgs  = ""
    AssistantSecondsToWaitAfterOpeningToPressTabs = 3
    AssistantNumberOfTimesToPressTabAfterOpeningToSelectTheTextInputBox = 4
    //UseMicrosoftOnlineSpeechRecognition = false
    CustomKey1   = ""
    CustomKey2   = ""
    CustomKey3   = ""
    CustomKey4   = ""
    CustomKey5   = ""
    CustomKey6   = ""
    CustomKey7   = ""
    CustomKey8   = ""
    CustomKey9   = ""
}


let saveSettings (settings: AppSettings) =
    let options = JsonSerializerOptions(WriteIndented = true) 
    let json = JsonSerializer.Serialize(settings, options)
    let directory = Path.GetDirectoryName(settingsFilePath)
    if not (Directory.Exists(directory)) then
        Directory.CreateDirectory(directory) |> ignore
    File.WriteAllText(settingsFilePath, json)

let mergeSettings (userSettings: JsonNode) (defaultSettings: AppSettings) =
    let mergedSettings = JsonObject()

    let properties = FSharpType.GetRecordFields(typeof<AppSettings>)

    for prop in properties do
        let propName = prop.Name
        let defaultValue = prop.GetValue(defaultSettings)

        try
            // Check if the user's settings contain the property
            if userSettings.AsObject().ContainsKey(propName) then
                // Use the user's value if it exists
                let userValue = userSettings[propName]
                // Ensure the value can be converted to the correct type
                match prop.PropertyType with
                | t when t = typeof<string> -> mergedSettings[propName] <- JsonValue.Create(userValue.ToString())
                | t when t = typeof<int>    -> mergedSettings[propName] <- JsonValue.Create(userValue.ToString() |> int)
                | t when t = typeof<bool>   -> mergedSettings[propName] <- JsonValue.Create(userValue.ToString() |> bool.Parse)
                | t when t = typeof<float>  -> mergedSettings[propName] <- JsonValue.Create(userValue.ToString() |> float)
                | _ -> mergedSettings[propName] <- JsonValue.Create(defaultValue)
            else
                mergedSettings[propName] <- JsonValue.Create(defaultValue)
        with
        | ex ->
            printfn "-- ERROR --" 
            printfn "-- Error occured when parsing one of the keys in your settings file"
            printfn "-- File:  %s" settingsFilePath
            printfn "-- Key:   %s" propName
            printfn "-- Error: %s" ex.Message
            printfn "Would you like to"
            printfn "1. Override this with the default value."
            printfn "2. Override your entire settings file with new defaults."
            printf  "Enter your choice [1 or 2]: "
            let userInput = Console.ReadLine()
            match userInput with
            | "1" ->
                printfn "Using default value for '%s'." propName
                mergedSettings[propName] <- JsonValue.Create(defaultValue) 
            | "2" ->
                printfn "Raising an error so that you will be prompted to reset the file."
                raise (JsonException("Error for the property named " + propName + ": " + ex.Message))
            | _ ->
                printfn "Invalid choice. Exiting the program."
                Environment.Exit(1) 

    mergedSettings

let loadSettings () =
    if File.Exists(settingsFilePath) then
        try
            let json = File.ReadAllText(settingsFilePath)
            let userSettings = JsonNode.Parse(json)

            // Ensure the parsed JSON is an object
            if userSettings :? JsonObject then
                let defaultSettings = createDefaultSettings()

                // Merge user settings with defaults (Done so that old settings files can be kept and updated automatically)
                let mergedSettings = mergeSettings userSettings defaultSettings

                // Convert json to AppSettings
                let settings = JsonSerializer.Deserialize<AppSettings>(mergedSettings.ToJsonString())
                saveSettings settings // Save the merged settings to ensure the file is up-to-date
                settings
            else
                raise (Exception("The settings file is not a valid JSON object"))
        with
        | :? JsonException as ex ->
            printfn "-- ERROR --" 
            printfn "-- Error occured when parsing your settings file"
            printfn "-- The settings file is likely corrupted or has formatting errors."
            printfn "-- File:  %s" settingsFilePath
            printfn "-- Error: %s" ex.Message
            printfn "Would you like to"
            printfn "1. Exit the program without changing your settings file, so you can manually repair it."
            printfn "2. Overwrite your settings file with working defaults, fixing it, and continue."
            printfn "3. Load default settings, but without fixing your settings file, and continue."
            printf  "Enter your choice [1 or 2 or 3]: "
            let userInput = Console.ReadLine()
            match userInput with
            | "1" ->
                printfn "Exiting the program."
                Environment.Exit(0)
                createDefaultSettings() // required for type consistency
            | "2" ->
                printfn "Overwriting settings with defaults."
                let defaultSettings = createDefaultSettings()
                saveSettings defaultSettings
                defaultSettings
            | "3" ->
                printfn "Loading defaults without overwriting settings file."
                let defaultSettings = createDefaultSettings()
                defaultSettings
            | _ ->
                printfn "Invalid choice. Exiting the program."
                Environment.Exit(1)
                createDefaultSettings() // required for type consistency
        | :? IOException as ex ->
            printfn "Error reading settings file: %s" ex.Message
            printfn "Exiting the program."
            Environment.Exit(1)
            createDefaultSettings() // required for type consistency
    else
        let defaultSettings = createDefaultSettings()
        saveSettings defaultSettings
        defaultSettings 



