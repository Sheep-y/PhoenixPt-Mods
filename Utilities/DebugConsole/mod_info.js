({
   Id : "Sheepy.DebugConsole",
   LoadIndex : -500, // Using the earliest recommended index to try to capture all logs.

   Lang : "en",
   Duration : "temp",
   Name : {
      en : "Debug Console",
      zh : "除錯指令行" },
   Description :
"Enable in-game console and route game log, unity log, and modnix log to it, showing game messages and errors, recorded in Console.log.  Provide console and api extension that aids in mod development.

Errors and warnings caused from using Modinx API through console will be picked up by Modnix and reported. Modnix does not make an exception for this mod and the mod itself should not cause errors or warnings.",
   Disables :
      { Id: "ConsoleEnabler", Max: "1.0.0.0" },
   Url : {
      "Nexus"  : "https://nexusmods.com/phoenixpoint/mods/44/",
      "GitHub" : "https://github.com/Sheep-y/PhoenixPt-Mods/"
   },
   ConfigType : "Sheepy.PhoenixPt.DebugConsole.ModConfig",
})