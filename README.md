# VimVoice

This is a continuation of AshleyF's VimSpeak, with my own additions and expansions.

Notably, this version adds a lot of missing keys, has a new code structure, and also comes with:

- non-vim keystrokes like alt-tab
- window, tab and tmux/screen tab management
- alt+tab, ctrl+tab and other windows keys
- settings
- the option to deafen and mute
- open an external assistant (configured in settings)
- invocation of external transcribers (I recommend WhisperWriter).
- and more additions that you'll need to peek into `Main.fs` and `Variables.fs` to gander at, or you can get a large printout with the 'printout-(normal/windows/visual/insert)-mode' command.

## README from VimSpeak

VimVoice lets you control Vim with your voice using speech recognition. For instance, you can say _“select three words”_ to type `v3w` or _“change surrounding brackets to parens”_ to type `cs])` or crazy things like _“change occurrences of ‘foo’ into ‘bar’, globally, ignore case, confirm”_ to type `:%s/foo/bar/gic`. Of course in insert mode you may dictate whatever you like. To learn the grammar, have a look at the unit tests and the code (“use the source, Luke”). It’s quite declarative and easy to follow.

The idea is to run this console app in the background where it will listen for speech and do `SendKeys(...)` to the foreground app. The foreground app may be any editor expecting Vim keystrokes. It’s been tested with Visual Studio with [Jared Parson’s excellent VsVim extention](https://github.com/jaredpar/VsVim) (also written in F#, BTW) and with Sublime Text in Vintage Mode and, of course, with Vim itself.

---

This is written in F# and makes use `System.Speech.Recognition`/`Synthesis` (.NET 3+). It also relies on `SendKeys(...)` (.NET 1.1+). I’ve only tested it under Win8, but it may work with Mono on other platforms.

---

Here's a demo/explanation of the program this is forked from: http://www.youtube.com/watch?v=TEBMlXRjhZY
And another demo applying it to a VimGolf challenge: http://www.youtube.com/watch?v=qy84TYvXJbk

Have fun!
