({
   Id : "Sheepy.GlobeTweaks",
   Lang : "*",
   Duration : "temp",
   Name : {
      en : "Globe Tweaks",
      zh : "地球介面微調" },
   Description : "Tweaks Geoscape globe - always show haven action icons, more info in haven popup, pause and centreing tweaks, etc.",
   Url : {
      "Nexus" : "https://nexusmods.com/phoenixpoint/mods/13/",
      "GitHub" : "https://github.com/Sheep-y/PhoenixPt-Mods/",
   },
   Disables : [
      { Id: "PhoenixPt_CentreOnNewBase", Max: "1.0.0.0" },
      { Id: "PhoenixPt_NoAutoUnpause", Max: "1.0.0.0" },
      { Id: "PhoenixPt_PauseOnHeal", Max: "1.0.1.0" },
   ],
   ConfigType : "Sheepy.PhoenixPt.GlobeTweaks.ModConfig",
})