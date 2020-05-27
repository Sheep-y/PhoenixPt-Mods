({
   Id : "Sheepy.RecruitInfo",
   Lang : "*",
   Duration : "temp",
   Name : "Recruit Info",
   Description : "Show information on recruits such as personal skills, mutations, and equipments.",
   Url : {
      "Nexus" : "https://nexusmods.com/phoenixpoint/mods/28/",
      "GitHub" : "https://github.com/Sheep-y/PhoenixPt-Mods/",
   },
   Disables : { Id: "PhoenixPt_RecruitInfo", Max: "1.0.0.0" },
   ConfigType : "Sheepy.PhoenixPt.RecruitInfo.ModConfig",
   /* Legacy config for Modnix 1.0 */
   DefaultConfig : {
      "Skills": {
         "Enabled": true,
         "Mouse_Over_Popup": true,
         "Name": "<size=42>...</size>",
         "Popup_Title": "<size=36><b>...</b></size>",
         "Popup_Body": "<size=30>...</size>"
      },
      "Grafts": {
         "Enabled": true,
         "Mouse_Over_Popup": true,
         "Name": "<size=42>...</size>",
         "Popup_Title": "<size=36><b>...</b></size>",
         "Popup_Body": "<size=30>...</size>"
      },
      "Equipments": {
         "Enabled": true,
         "Mouse_Over_Popup": true,
         "Name": "<size=36>...</size>",
         "Popup_Title": "<size=36><b>...</b></size>",
         "Popup_Body": "<size=30>...</size>"
      },
      "Config_Version": 20200417
   }
})