({
   Id : "Sheepy.LessRecruit",
   Name : "Less Recruit",
   Duration: "newgame",
   Lang : "-",
   Author : "Sheepy",
   Version : "0.0.2020.0625",
   Description : "Remove 1 Assault from the starting squad of each difficulty, leaving 5 people for Rookie, 4 for Veteran, and 3 for Hero & Legend.\n\nRequires Scripting Library (C#).",
   Url : "https://github.com/Sheep-y/PhoenixPt-Mods/",
   Requires: "Zy.cSharp",
   Actions : [{
      "Action" : "Default",
      "Phase" : "GeoscapeMod",
   },{
      "Eval" : 'void LessRecruit ( string guid ) { ref var ary = ref GetDef<GameDifficultyLevelDef>( guid ).StartingSquadTemplate; ary = ary.Take( ary.Length - 1 ).ToArray(); }',
   },{
      "Eval" : 'LessRecruit( "Easy_GameDifficultyLevelDef" )', // Rookie
   },{
      "Eval" : 'LessRecruit( "Standard_GameDifficultyLevelDef" )', // Veteran
   },{
      "Eval" : 'LessRecruit( "Hard_GameDifficultyLevelDef"  )', // Hero
   },{
      "Eval" : 'LessRecruit( "VeryHard_GameDifficultyLevelDef"  )', // Legend
   }],
})