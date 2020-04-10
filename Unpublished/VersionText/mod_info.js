{
    Id : "Sheepy.VersionText",
    Name : "Version Text",
    Description : "A hello world mod.\r\t* Change home screen version text.\n\t* Configurable.",
    Duration : "temp",
    Url : { "GitHub" : "https://github.com/Sheep-y/PhoenixPt-Mods", "Nexus" : "https://nexusmods.com/phoenixpoint/mods/" },
    LoadIndex : -10,
    Requires : { Id: "Sheepy.DebugConsole", Min : "3.0" },
    Disables : [ { Id : "DeafTheDead", Max : "0.9" }, {}, {} ],
    ConfigType : "VersionText.ModConfig",
}