({
   Id : "Sheepy.LaserOnFire",
   Name : "Laser on Fire",
   Author : "Sheepy",
   Version : "1.0",
   Requires: "Zy.JavaScript",
   Description : "
Adds a small fire damage to laser weapons, plus pierce or shred on selected lasers.

This is done as a simple demo to 

This mod requires Modnix 3+ and Scripting Library 2+.
Tested on Phoenix Point 1.9.3.",
   Actions : [{
      "Action" : "Default",    // Required to set "Script" on all actions.
      "Script" : "JavaScript", // When combined with "Eval", cause the Scripting Library 2 to run the actions.
      "OnError" : "Warn, Continue", // Default is "Log, Stop" which logs the error and stops execution.
      "Phase" : "MainMod",
   },{
      "Eval" : 'Repo.Get( WeaponDef, "SY_LaserPistol_WeaponDef" ).Fire( 1 )',
   },{
      "Eval" : 'Repo.Get( WeaponDef, "SY_LaserAssaultRifle_WeaponDef" ).Fire( 1 )',
   },{
      "Eval" : 'Repo.Get( WeaponDef, "SY_LaserSniperRifle_WeaponDef" ).Fire( 3 ).Pierce( 5 )',
   },{
      "Eval" : 'Repo.Get( WeaponDef, "PX_LaserPDW_WeaponDef" ).Fire( 1 )',
   },{
      "Eval" : 'Repo.Get( WeaponDef, "PX_LaserTechTurretGun_WeaponDef" ).Fire( 1 ).Shred( 2 )',
   },{
      "Eval" : 'Repo.Get( WeaponDef, "PX_LaserArrayPack_WeaponDef" ).Fire( 2 )',
   }],
   Url : {
      "Nexus" : "https://nexusmods.com/phoenixpoint/mods/33/",
      "GitHub" : "https://github.com/Sheep-y/PhoenixPt-Mods/",
      "Modnix" : "https://www.nexusmods.com/phoenixpoint/mods/43",
      "Scripting Library" : "https://www.nexusmods.com/phoenixpoint/mods/49",
   },
   Copyright: "Public Domain",
})