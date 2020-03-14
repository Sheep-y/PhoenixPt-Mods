({
   Id : "Sheepy.GlobeTweaks",
   Lang : "-",
   Taint : false,
   Name : {
      en : "Globe UI Tweaks",
      zh : "地球介面微調" },
   Description : {
      en : "Tweaks Geoscape globe - do not unpause on move, pause on heal, centre to new base etc.",
      zh : "微調地球介面 - 移動時不解除暫停，隊伍恢復時暫停，聚焦於新基地等。" },
   Url : {
      "Nexus" : "https://nexusmods.com/phoenixpoint/mods/17",
      "GitHub" : "https://github.com/Sheep-y/PhoenixPt-Mods",
   },
   Conflicts : [
      { Id: "PhoenixPt_CentreOnNewBase", Max: "1.0.0.0" },
      { Id: "PhoenixPt_NoAutoUnpause", Max: "1.0.0.0" },
      { Id: "PhoenixPt_PauseOnHeal", Max: "1.0.1.0" },
   ],
   DefaultSettings : {
      "Center_On_New_Base" : true,
      "Centre_On_Heal" : true,
      "No_Auto_Unpause" : true,
      "Pause_On_Heal" : true,
   },
})