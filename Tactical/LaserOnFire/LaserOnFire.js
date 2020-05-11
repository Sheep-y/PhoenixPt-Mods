({
   Id : "Sheepy.LaserOnFire",
   Name : "Laser on Fire",
   Author : "Sheepy",
   Flags : "Library",
   Version : "0.1",
   Description : "Add a small fire damage to all laser weapons, plus pierce or shred on the heavier lasers.\n\nThis is done to demo Modnix data mod.",
   Url : {
      "Nexus" : "https://nexusmods.com/phoenixpoint/mods/47/",
      "GitHub" : "https://github.com/Sheep-y/PhoenixPt-Mods/",
   },
   Actions : [{
      "Action"  : "Default",
      "OnError" : "log, stop"
   },{
      "Eval" : 'DateTime.Now',
   },{
      "Eval" : 'GameUtl.GameComponent<DefRepository>().GetAllDefs<PiercingDamageKeywordDataDef>().FirstOrDefault()',
   }],

/* Envision migrated from Weapon Overhaul
      //"eval" : 'FirstDef<WeaponDef>( "NJ_PRCR_AssaultRifle_WeaponDef" ).AddDamage( ShreddingDamageKeywordDataDef, 5 )',
   {
   "set" : "ItemDef[name=NJ_Gauss_AssaultRifle_AmmoClip_ItemDef].ChargesMax",
   "val" : 20,
}, {
   "set" : "$w",
   "val" : "WeaponDef[name=PX_AssaultRifle_WeaponDef]",
}, {
   "set" : "$w.DamagePayload.AutoFireShotCount",
   "val" : 5,
}, {
   "set" : "$w.ChargesMax",
   "val" : 40,
}, {
   "set" : "$w.DamagePayload.DamageType.HasArmourShred",
   "val" : true,
}, {
   "add" : "$w.DamagePayload.DamageKeywords",
   "val" : "new DamageKeywordPair{ DamageKeywordDef = ShreddingDamageKeywordDataDef, Value = 2.0 }"
}, {
   "set" : "$w.DamagePayload.DamageKeywords[0]",
   "val" : 35,
   "onerror" : "verbose"
}
*/
})