{  /* This file should be bundled with the dll, either zipped together or set to "Resource" or "Embedded Resource" in build action.
      Not bundling this file means a number of features will fail to register with Modnix, such as config and dependencies. */

   /* Mod Id.  Keep this unique and consistent. */
   Id : "Sheepy.VersionText",

   /* Yes comment is allowed.  This is not strict json. */
   Name : "Version Text",

   /* Plain text only please. */
   Description : "A hello world mod.\r\t* Change home screen version text.\n\t* Configurable.",

   /* temp = mod has no lasting impact on saves.  perm = saves may be severely impacted when mod is disabled. */
   Duration : "temp",

   /* Will be displayed in Modnix's info panel */
   Url : {
      "GitHub" : "https://github.com/Sheep-y/PhoenixPt-Mods/",
      "Nexus" : "https://nexusmods.com/phoenixpoint/mods/"
   },

   /* Default zero.  Lower index = load earlier.  Higher index = load later. */
   LoadIndex : -10,

   /* If any mod listed here is not found, this mod will be disabled. 
      Depending on a mod does NOT mean your mod will be loaded after it.
      Dependency does not affect load order (beyond disabling mods). */
   Requires : { Id: "Sheepy.DebugConsole", Min : "3.0" },

   /* Mods here will be disabled, if exists. */
   Disables : [ { Id : "DeafTheDead", Max : "0.9" }, {}, {} ],

   /* The dlls will be scanned for this type. If found, it will be used to manage config. */
   ConfigType : "VersionText.ModConfig",

   // Can also use single-line comment
}