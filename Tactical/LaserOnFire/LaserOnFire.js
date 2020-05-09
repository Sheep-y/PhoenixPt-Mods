({
   Id : "Sheepy.LaserOnFire",
   Duration : "temp",
   Name : "Laser on Fire",
   Description : "Add a small fire damage to all laser weapons, plus pierce or shred on the heavier lasers.\n\nThis is done to demo Modnix data mod.",
   Url : {
      "Nexus" : "https://nexusmods.com/phoenixpoint/mods/47/",
      "GitHub" : "https://github.com/Sheep-y/PhoenixPt-Mods/",
   },
   Requires : { Id: "Modnix", Min: "3.0" },
   Actions : [{
      "name" : "Add shred to PRCR AR",
      "eval" : "FirstDef<WeaponDef>( \"NJ_PRCR_AssaultRifle_WeaponDef\" ).AddDamage( ShreddingDamageKeywordDataDef, 5 )",
      "onerror" : "stop"
   }],

/* Envision migrated from Weapon Overhaul
   {
   "set" : "ItemDef[name=NJ_Gauss_AssaultRifle_AmmoClip_ItemDef].ChargesMax",
   "to" : 20,
}, {
   "set" : "$w",
   "to" : "WeaponDef[name=PX_AssaultRifle_WeaponDef]",
}, {
   "set" : "$w.DamagePayload.AutoFireShotCount",
   "to" : 5,
}, {
   "set" : "$w.ChargesMax",
   "to" : 40,
}, {
   "set" : "$w.DamagePayload.DamageType.HasArmourShred",
   "to" : true,
}, {
   "add" : "$w.DamagePayload.DamageKeywords",
   "value" : "new DamageKeywordPair{ DamageKeywordDef = ShreddingDamageKeywordDataDef, Value = 2.0 }"
}, {
   "set" : "$w.DamagePayload.DamageKeywords[0]",
   "value" : 35,
   "onerror" : "verbose"
}
*/
})