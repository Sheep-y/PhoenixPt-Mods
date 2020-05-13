({
   Id : "Zy.Scripting",
   Flags : "Library",
   Name : "Scripting Library",
   Description : 'Support "Eval" mod actions, "Eval" console command, "eval.cs" api.',
   LoadIndex : -200, // Load early to pre-load scripting engine
   Url : {
      "Nexus" : "https://nexusmods.com/phoenixpoint/mods/",
      "GitHub" : "https://github.com/Sheep-y/PhoenixPt-Mods/",
   },
   Requires : { Id: "Modnix", Min: 3 },
})