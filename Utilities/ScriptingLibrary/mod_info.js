({
   Id : "Zy.JavaScript",
   Name : "JavaScript Runtime",
   Description : '
Provide a JavaScript runtime, allowing JavaScript-based mods to run,
plus a small helper library that helps them get the job done.

Also provide the "JavaScirpt" console command, and "eval.js" API,
and game def lookup through "pp.def" and "pp.defs" API.
',
   LoadIndex : -200, // Load early to pre-load scripting engine.
   Requires: [{ Id: "Modnix", Min: "3.0.2021.0125" }],
   Url : {
      "Nexus" : "https://nexusmods.com/phoenixpoint/mods/",
      "GitHub" : "https://github.com/Sheep-y/PhoenixPt-Mods/",
   },
})