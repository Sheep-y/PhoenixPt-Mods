({
   Id : "Sheepy.SkipIntro",
   Lang : "-",
   Duration : "temp",
   Name : {
      en : "Skip Intro",
      zh : "略過片頭" },
   Description : {
      en : "Skip game intro, loading curtains, and combat landing .",
      zh : "略過遊戲片頭、載入布幕、及戰鬥進場影片。" },
   Url : {
      "Nexus" : "https://nexusmods.com/phoenixpoint/mods/17/",
      "GitHub" : "https://github.com/Sheep-y/PhoenixPt-Mods/",
   },

   /* Disable pre-modnix version, which has filename-based id. */
   Disables : { Id: "PhoenixPt_SkipIntro", Max: "1.1.0.0" },

   ConfigType : "Sheepy.PhoenixPt.SkipIntro.ModConfig",

   /* Legacy config for Modnix 1.0 */
   DefaultConfig : {
      "Skip_Logos" : true,
      "Skip_HottestYear" : true,
      "Skip_NewGameIntro" : false,
      "Skip_Landings" : true,
      "Skip_CurtainDrop" : true,
      "Skip_CurtainLift" : true,
      "Config_Version" : 20200322,
   },
})