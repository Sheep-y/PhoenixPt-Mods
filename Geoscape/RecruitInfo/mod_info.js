({
   Id : "Sheepy.RecruitInfo",
   Lang : "-",
   Duration : "temp",
   Name : "Recruit Info",
   Description :
      "Tune down the global faction war by reducing damage, number of attacks, and chance of successful attack.",
   Url : {
      "Nexus" : "https://nexusmods.com/phoenixpoint/mods/24/",
      "GitHub" : "https://github.com/Sheep-y/PhoenixPt-Mods/",
   },
   Disables : [
      { Id: "PhoenixPt_BetterDefence", Max: "1.0.0.0" },
      { Id: "PhoenixPt_LessWar", Max: "1.0.0.0" },
      { Id: "PhoenixPt_LimitWarToZone", Max: "1.0.0.0" },
      { Id: "PhoenixPt_NoWar", Max: "1.0.0.0" },
   ],
   ConfigType : "Sheepy.PhoenixPt.RecruitInfo.ModConfig",
})