({
   Id : "Sheepy.GlobeTweaks",
   Lang : "*",
   Duration : "temp",
   Name : {
      en : "Globe Tweaks",
      zh : "地球介面微調" },
   Description : {
      en : "Tweaks Geoscape globe - do not unpause on move, pause on rest and heal, centre to new base etc.",
      zh : "微調地球介面 - 移動時不解除暫停，隊伍休息好及康復時暫停，聚焦新基地等。" },
   Url : {
      "Nexus" : "https://nexusmods.com/phoenixpoint/mods/17",
      "GitHub" : "https://github.com/Sheep-y/PhoenixPt-Mods",
   },
   Disables : [
      { Id: "PhoenixPt_CentreOnNewBase", Max: "1.0.0.0" },
      { Id: "PhoenixPt_NoAutoUnpause", Max: "1.0.0.0" },
      { Id: "PhoenixPt_PauseOnHeal", Max: "1.0.1.0" },
   ],
   ConfigType : "Sheepy.PhoenixPt.GlobeTweaks.ModConfig",

   /* Legacy config for Modnix 1.0 */
   DefaultConfig : {
      "Base_Centre_On_Heal" : true,
      "Base_Pause_On_Heal" : true,
      "Center_On_New_Base" : true,
      "No_Auto_Unpause" : true,
      "Notice_On_HP_Only_Heal" : false,
      "Notice_On_Stamina_Only_Heal" : true,
      "Vehicle_Centre_On_Heal" : true,
      "Vehicle_Pause_On_Heal" : true,
      "Config_Version" : 20200324,
   },
})