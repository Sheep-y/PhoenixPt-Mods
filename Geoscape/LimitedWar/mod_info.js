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
   Disables : [
      { Id: "PhoenixPt_BetterDefence", Max: "1.0.0.0" },
      { Id: "PhoenixPt_LessWar", Max: "1.0.0.0" },
      { Id: "PhoenixPt_LimitWarToZone", Max: "1.0.0.0" },
      { Id: "PhoenixPt_NoWar", Max: "1.0.0.0" },
   ],
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
	  }
   }
})