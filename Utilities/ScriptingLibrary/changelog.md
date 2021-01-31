Changelog of Scripting Library (C#), a Phoenix Point Mod by Sheepy

# Version 2 Beta, in develpoment

* Switch to ClearScript to support V8 engine.  It is working!
* Refactoring of helpers due to engine switch.
* Moved console shell to Debug Console.  The one-line Eval command still works.
* Renamed Eval console command to Javascript.
* Tested on Phoenix Point 1.9.3.66065.

# Version Beta ?

* Mod API now run in the Eval context of the caller, inheriting its variables and state.
* Giving up because failed to get the mod work on on Year One Edition. Development have ceased.

# Version Alpha 0.0.2020.0605

* Mod id changed to "Zy.cSharp" which is shorter and more precise than "Zy.Scripting".

# Version Alpha 0.0.2020.0603

* First public release.
* Support "Eval" mod actions, with each mod having their own shell.
* Support "Eval" console command (single line / Eval mode), "eval.cs" Api, "pp.def" Api, "pp.defs" Api.
* GetDef, GetDefs, and assorted weapon damage helpers.