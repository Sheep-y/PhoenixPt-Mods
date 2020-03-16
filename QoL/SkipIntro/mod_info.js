({
   /* Please make sure id is unique! Same id = only one survives. */
   Id : "Sheepy.SkipIntro",
   // Version is read from Assembly Information.  But you can override it here if you want to.

   /* Mod Declarations and Descriptions */
   Lang : "-", // Language-independent
   Duration : "temp", // Effects are temporary and does not affect saves
   Name : {
      en : "Skip Intro",
      zh : "略過片頭" },
   Description : {
      en : "Skip game intro, loading curtains, and combat landing .",
      zh : "略過遊戲片頭、載入布幕、及戰鬥進場影片。" },
   Url : {
      "Nexus" : "https://nexusmods.com/phoenixpoint/mods/17",
      "GitHub" : "https://github.com/Sheep-y/PhoenixPt-Mods",
   },

   /* Disable pre-modnix version, which has filename-based id. */
   Conflicts :
      { Id: "PhoenixPt_SkipIntro", Max: "1.1.0.0" },

   /* Config files will be created when users click "Reset". */
   DefaultConfig : {
      "Skip_Logos" : true,
      "Skip_HottestYear" : true,
      "Skip_Landings" : true,
      "Skip_CurtainDrop" : true,
      "Skip_CurtainLift" : true,
   },
})