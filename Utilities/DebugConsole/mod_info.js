({
   Id: "Sheepy.DebugConsole",
   LoadIndex : -500,

   Lang: "en",
   Duration : "temp",
   Name: {
      en : "Debug Console",
      zh : "除錯指令行" },
   Description :
      "Enable in-game console and route game log, unity log, and modnix log to it, showing game messages and errors, recorded in Console.log.",
   Disables:
      { Id: "ConsoleEnabler", Max: "1.0.0.0" },
   Url: {
      "Nexus"  : "https://nexusmods.com/phoenixpoint/mods/44/",
      "GitHub" : "https://github.com/Sheep-y/PhoenixPt-Mods/"
   },
   ConfigType: "Sheepy.PhoenixPt.DebugConsole.ModConfig",

   /* Legacy config for Modnix 1.0 */
   DefaultConfig: {
      "Mod_Count_In_Version" : true,
      "Log_Game_Error" : true,
      "Log_Game_Info" : true,
      "Log_Modnix_Error" : true,
      "Log_Modnix_Info" : true,
      "Log_Modnix_Verbose" : false,
      "Scan_Mods_For_Command" : true,
      "Write_Modnix_To_Console_Logfile" : false,
      "Config_Version" : 20200405,
   },
})