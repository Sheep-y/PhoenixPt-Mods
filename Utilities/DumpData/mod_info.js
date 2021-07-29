({
   Id : "Sheepy.DumpData",
   Lang : "*",
   Duration : "temp",
   Name : "Dump Data",
   requires : { Id: "Phoenix Point", Min: "1.11.0.0" },
   Description : "
Export various data to xml/csv when you start or load a geoscape game.
   
Note that Dump_Others config requires JavaScript Runtime, but the other data can be exported standalone.

Data are saved in this mod's folder by default.

Requires Phoenix Point 1.11 and above due to change in the game's audio subsystem.
Older game versions please use Dump Data 2.2.
",
   Url : {
      "GitHub" : "https://github.com/Sheep-y/PhoenixPt-Mods/",
      "Nexus"  : "https://nexusmods.com/phoenixpoint/mods/50/",
      "JavaScript Runtime" : "https://www.nexusmods.com/phoenixpoint/mods/49",
   },
   ConfigType : "Sheepy.PhoenixPt.DumpData.ModConfig",
})