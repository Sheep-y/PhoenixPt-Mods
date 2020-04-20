({
   Id : "Sheepy.LimitedWar",
   Lang : "-",
   Duration : "temp",
   Name : {
      en : "Limited War",
      zh : "有限戰爭" },
   Description : {
      en : "Tune down the global faction war by reducing damage, number of attacks, and chance of successful attack.",
      zh : "泠卻派系戰爭。減低傷害，限制攻擊的次數，並提升防守成功的機率。" },
   Url : {
      "Nexus" : "https://nexusmods.com/phoenixpoint/mods/24/",
      "GitHub" : "https://github.com/Sheep-y/PhoenixPt-Mods/",
   },
   // Disabled because Modnix Manager does not process game version correctly.
   // Not very useful when it shows as "disabled" in the manager but actually works in game.
   /*Requires : { Id: "Phoenix Point", Min: "1.0.56049" },*/
   Disables : [
      { Id: "PhoenixPt_BetterDefence", Max: "1.0.0.0" },
      { Id: "PhoenixPt_LessWar", Max: "1.0.0.0" },
      { Id: "PhoenixPt_LimitWarToZone", Max: "1.0.0.0" },
      { Id: "PhoenixPt_NoWar", Max: "1.0.0.0" },
   ],
   ConfigType : "Sheepy.PhoenixPt.LimitedWar.ModConfig",

   /* Legacy config for Modnix 1.0 */
   DefaultConfig : {
      Faction_Attack_Zone : true,
      Pandora_Attack_Zone : false,
      Attack_Raise_Alertness : true,
      Attack_Raise_Faction_Alertness : true,

      Stop_OneSided_War : true,
      No_Attack_When_Sieged_Difficulty : 2,
      One_Global_Attack_Difficulty : 0,
      One_Attack_Per_Faction_Difficulty : 1,

      Defense_Multiplier : {
          Default : 1,
          Alert    : 1.2,
          High_Alert : 1.1,
          Attacker_Pandora : 1.2,
          Attacker_Anu : 1,
          Attacker_NewJericho : 1,
          Attacker_Synedrion : 1,
          Defender_Pandora : 1,
          Defender_Anu : 1.2,
          Defender_NewJericho : 1,
          Defender_Synedrion : 1.2,
      },
      No_Alien_Attack_On_PhoenixPoint : false,

      Config_Version : 20200325,
   }
})