({
   Id : "Sheepy.LimitedWar",
   Lang : "-",
   Duration : "temp",
   Name : {
      en : "Limited War",
      zh : "有限戰爭" },
   Description : {
      en : "Tune down the global faction war by reducing damage done and number of attacks.",
      zh : "泠卻派系戰爭。將傷害減低，並限制攻擊的次數。" },
   Url : {
      "Nexus" : "https://nexusmods.com/phoenixpoint/mods/24",
      "GitHub" : "https://github.com/Sheep-y/PhoenixPt-Mods",
   },

   /* Disable pre-modnix version, which has filename-based id. */
   Conflicts :
      { Id: "PhoenixPt_SkipIntro", Max: "1.1.0.0" },

   DefaultConfig : {
      "Skip_Logos" : true,
      "Skip_HottestYear" : true,
      "Skip_Landings" : true,
      "Skip_CurtainDrop" : true,
      "Skip_CurtainLift" : true,
   },
})