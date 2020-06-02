({
   Id : "Sheepy.LaserOnFire",
   Name : "Laser on Fire",
   Author : "Sheepy",
   Version : "1.0",
   Description : "Add a small fire damage to all laser weapons, plus pierce or shred on a few lasers.\nAlso modify Destiny III to fire in first person.\n\nThis mod requires Modnix 3+ and the Scripting Library mod. Tested on Phoenix Point Ver 1.0.57630.",
   Url : {
      "Nexus" : "https://nexusmods.com/phoenixpoint/mods/33/",
      "GitHub" : "https://github.com/Sheep-y/PhoenixPt-Mods/",
   },
   Requires: "Zy.Scripting",
   Actions : [{
      "Action"  : "Default",
      "OnError" : "log, stop"
   },{ // Add fire to Hephaestus II (Synedrion Laser Pistol)
      "Eval" : 'GetDef<WeaponDef>( "a30138e8-a29b-1f24-8b80-7dafe1b9aada" ).Fire( 1 )',
   },{ // Add fire to Deimos AR-L (Synedrion Laser Assault Rifle)
      "Eval" : 'GetDef<WeaponDef>( "309474c9-3034-bb04-49d8-a129812789a4" ).Fire( 1 )',
   },{ // Add fire and pierce to Pythagoras VII (Synedrion Laser Sniper Rifle)
      "Eval" : 'GetDef<WeaponDef>( "eec3f504-2336-5b84-2963-70d85e593611" ).Fire( 2 ).Pierce( 5 )',
   },{ // Add fire to Gorgon Eye-A (Phoenix Laser PDF)
      "Eval" : 'GetDef<WeaponDef>( "1c053f71-38a0-9674-7821-8dffcdca49aa" ).Fire( 1 )',
   },{ // Add fire and shred to Scorcher AT (Phoenix Laser Auto Turret)
      "Eval" : 'GetDef<WeaponDef>( "07b631ea-690a-2854-896c-4947f9773c7c" ).Fire( 1 ).Shred( 2 )',
   },{ // Add fire to Destiny III (Phoenix Laser Array), and assign to pla.  ';' is required for assignment.
      "Eval" : 'var pla = GetDef<WeaponDef>( "fd84633b-79a3-82e4-fb53-07ab8c778545" ).Fire( 2 );',
   },{ // Enable Destiny III first-person aim by replacing launch rocket ability with fire weapon ability,
      "Eval" : 'pla.Abilities = pla.Abilities.Where( e => e.Guid != "0ea4735b-dde8-e904-180e-3e1faef16a56" )
           .Concat( new AbilityDef[] { Repo.GetDef( "d3e8b389-069f-04c4-8aca-fb204c74fd37" ) as AbilityDef } ).ToArray()',
   },{ // and disable first person camera shake
      "Eval" : 'pla.DamagePayload.CameraShake = null',
   }],
})