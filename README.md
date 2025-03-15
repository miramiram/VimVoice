# VimVoice

This is a continuation of AshleyF's VimSpeak, with my own additions and expansions.

Notably, this version adds a lot of missing keys, has a new code structure, and also comes with:

- window, tab and tmux/screen tab management
- non-VIM keystrokes like alt+tab, ctrl+tab and other OS keys (Access with "use-windows-mode")
- settings (Say "open-settings")
- the option to deafen and mute ("begin-hearing" "stop-hearing")
- open an external assistant (configured in settings)
- invocation of external transcribers (I recommend WhisperWriter).
- and more additions that you'll need to peek into `Main.fs` and `Variables.fs` to gander at, or you can get a large printout with the 'printout-(normal/windows/visual/insert)-mode' command.
    - For an overview of the fancy commands, go to `Variables.fs` and search for "let universalCommands"

## Building

Building the program yourself is very straightforward:

- Download Visual Studio 2022
	- Make sure to select the Windows Development and .NET related packages in the installer
- Clone/download this repository
- Double-click the `VimVoice.sln` file and open it with Visual Studio 2022
- Now look for the "Build" button on the very top of the screen
	- To build an EXE
		1. Make sure the box underneath the "Build" button (to the left of the green arrow) which reads "Release" or "Debug" is set to "Release"
		2. Then click "Build" and then "Build VimVoice".
        3. Open the VimVoice folder, and open the "bin" folder, then "Release" and "net472". This is the app folder, open VimVoice with `VimVoice.exe`.
	- To debug
		1. Make sure the box underneath the "Build" button (to the left of the green arrow) which reads "Release" or "Debug" is set to "Debug"
		2. Click the green arrow to the right of this box, the one next to the text "VimVoice". 

## README from VimSpeak

VimVoice lets you control Vim with your voice using speech recognition. For instance, you can say _“select three words”_ to type `v3w` or _“change surrounding brackets to parens”_ to type `cs])` or crazy things like _“change occurrences of ‘foo’ into ‘bar’, globally, ignore case, confirm”_ to type `:%s/foo/bar/gic`. Of course in insert mode you may dictate whatever you like. To learn the grammar, have a look at the unit tests and the code (“use the source, Luke”). It’s quite declarative and easy to follow.

The idea is to run this console app in the background where it will listen for speech and do `SendKeys(...)` to the foreground app. The foreground app may be any editor expecting Vim keystrokes. It’s been tested with Visual Studio with [Jared Parson’s excellent VsVim extention](https://github.com/jaredpar/VsVim) (also written in F#, BTW) and with Sublime Text in Vintage Mode and, of course, with Vim itself.

---

This is written in F# and makes use `System.Speech.Recognition`/`Synthesis` (.NET 3+). It also relies on `SendKeys(...)` (.NET 1.1+). I’ve only tested it under Win8, but it may work with Mono on other platforms.

---

Here's a demo/explanation of the program this is forked from: http://www.youtube.com/watch?v=TEBMlXRjhZY
And another demo applying it to a VimGolf challenge: http://www.youtube.com/watch?v=qy84TYvXJbk

Have fun!
