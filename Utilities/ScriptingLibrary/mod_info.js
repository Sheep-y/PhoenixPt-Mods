({
   Id : "Zy.JavaScript",
   Name : "Scripting Library (Beta)",
   Description : 'Supports runtime JavaScript evaluation, such as "Eval" mod actions (Modnix 3), "Eval" console command, and "eval.js" api.\r\rProvides data lookup by name and path through "pp.def" and "pp.defs" api.\r\rMods may also directly import helpers from ScriptHelpers.',
   LoadIndex : -200, // Load early to pre-load scripting engine.
   Requires: [{ Id: "Modnix", Min: "3.0.2021.0125" }],
   Url : {
      "Nexus" : "https://nexusmods.com/phoenixpoint/mods/",
      "GitHub" : "https://github.com/Sheep-y/PhoenixPt-Mods/",
   },
})