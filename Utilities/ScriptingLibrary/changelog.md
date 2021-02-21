Changelog of JavaScript Runtime, a Phoenix Point Mod by Sheepy

# Version 2.2, 2021-02-21

* Allow reflection, such as DotNetObject.GetType().
* New patch helper, reflection helper (espy), and reflection extenstions.
* console log messages are now logged under the mod's name, without tagging JS Runtime.

# Version 2.1, 2021-02-02

* Added all Unity namespaces as the Unity object.
* Import all UnityEngine.CoreModule classes to global.

# Version 2, 2021-02-01

* Switch to ClearScript to support V8 engine.  Renamed from Scripting Library to JavaScript Runtime.
* Actions now require both "Eval" and "Script" to run. Script must be JS, JavaScript, or ECMAScript (case insensitive).
* Mod id changeed to "Zy.JavaScript".
* Renamed Eval console command to Javascript.

* Add and refactor helpers due to engine switch.
* Moved console shell to Debug Console.  The one-line Eval command still works.
* Tested on Phoenix Point 1.9.3.66065.

# Version Beta, never released

* Mod API now run in the Eval context of the caller, inheriting its variables and state.
* Giving up on 2021 Jan, because can't get the mod to work on on Year One Edition.

# Version Alpha 0.0.2020.0605

* Mod id changed to "Zy.cSharp" which is shorter and more precise than "Zy.Scripting".

# Version Alpha 0.0.2020.0603

* Support "Eval" mod actions, with each mod having their own shell.
* Support "Eval" console command (single line / Eval mode), "eval.cs" Api, "pp.def" Api, "pp.defs" Api.
* GetDef, GetDefs, and assorted weapon damage helpers.