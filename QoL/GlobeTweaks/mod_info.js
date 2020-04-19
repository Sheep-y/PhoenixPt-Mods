({
   Id : "Sheepy.GlobeTweaks",
   Lang : "*",
   Duration : "temp",
   Name : {
      en : "Globe Tweaks",
      zh : "地球介面微調" },
   Description : "Tweaks Geoscape globe - always show haven action icons, more info in haven popup, pause and centreing tweaks, etc.""
   Url : {
      "Nexus" : "https://nexusmods.com/phoenixpoint/mods/17/",
      "GitHub" : "https://github.com/Sheep-y/PhoenixPt-Mods/",
   },
   Disables : [
      { Id: "PhoenixPt_CentreOnNewBase", Max: "1.0.0.0" },
      { Id: "PhoenixPt_NoAutoUnpause", Max: "1.0.0.0" },
      { Id: "PhoenixPt_PauseOnHeal", Max: "1.0.1.0" },
   ],
   ConfigType : "Sheepy.PhoenixPt.GlobeTweaks.ModConfig",

   /* Legacy config for Modnix 1.0 */
   DefaultConfig : {
      "Base_Centre_On_Heal": true,
      "Base_Pause_On_Heal": true,
      "Center_On_New_Base": true,
      "No_Auto_Unpause": true,
      "Notice_On_HP_Only_Heal": false,
      "Notice_On_Stamina_Only_Heal": true,
      "Vehicle_Centre_On_Heal": true,
      "Vehicle_Pause_On_Heal": true,
      "Haven_Icons": {
         "Always_Show_Action": true,
         "Always_Show_Soldier": true,
         "Popup_Show_Recruit_Class": true,
         "Popup_Show_Trade": true,
         "In_Stock_Line": "<size=28>{0} {1} > {2} {3} ({4})</size>\n",
         "Out_Of_Stock_Line": "<color=red><size=28>{0} {1} > {2} {3} ({4})</size></color>\n"
      },
      "Config_Version": 20200419
   },
})