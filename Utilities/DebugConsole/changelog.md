Changelog of Debug Console, a Phoenix Point Mod by Sheepy

# Version 7, in development

* New api "console.shell start" and "console.shell stop" to create a console shell with caller function.

# Version 6.1, 2020-07-10

* Fix: Handle TypeLoadException when scanning a class for command methods.
* Tested on Phoenix Point 1.5.3.59629.

# Version 6, 2020-07-01

* Non-error game messages are no longer forwarded by default, to bring performance to expectations.
* New: API command "console.write" to "stringify" an object and write to console.
* Fix: Catch stringify error when renderig API return value, and fallback to type name.
* Fix: Non-unity richtext tags are now preserved, e.g. C# code.
* Renamed API command "zy.ui.dump" to "ui.dump" to make it easier to remember.
* Return value is stringified to a max depth of 3.
* Faster command scanning of new assemblies.
* Skip console scanning of dynamic assemblies, scripting assemblies, and a few libraries.
* Tested on Phoenix Point 1.0.58929.

# Version 5.0.1, 2020-05-10

* Excludes more system and utility libraries from command scanning.
* Handles ReflectionTypeLoadException when scanning commands.
* Log negative time (before mod initialisation) as zero for consistency of display.

# Version 5, 2020-05-04

* Updated to Modnix 2.3.  Older Modnix would lost ui dump api.  No longer configurable on Modnix 1.
* Rewrite log capturer and processor. Faster, capture more, consistent format, and accurate timestamp.
* Long console messages now displayed trimmed and fully written to console log.
* "Optimise_Log_File" config removed, because the new log processor depends on it.
* [log_level] No longer report Modnix level when not using Modnix.
* [zy.ui.dump] Show component text of non-direct children.
* New mod architecture with less dead code.

# Version 4, 2020-04-24

* New: Replace game's Console.log writer to improve performance and make time sortable.
* New: API command "zy.gui.dump" to dump gui tree to Modnix log.
* New: Console command "dump_gui" to do the same to console.
* New: Console command "log_level" and "log_level_modnix" to get and set console and modnix log level.
* Fix: Log lines now have their line colours removed from console log.
* Fix: Log lines with curry brackets no longer crash the console log writer. This is (was) a vanilla bug.
* Fix: MethodInfo result from API are now displayed by ToString.

# Version 3, 2020-04-11

* New: Scan mods for console command.
* New: Console command to run modnix api.
* Fix: Brackets are now escaped to avoid string format error in the console.
* Fix: ConsoleEnabler mod now properly disabled.
* Modnix messages are now coloured in the console to distinguish from game messages.

# Version 2, 2020-03-25

* New: Modnix mod logs are now forwarded.  Not written to console log by default.
* New: Configurable log level.
* Fix: Avoid graphic rebulid loop error by caching logs and build on TimingScheduler.Update.
* Fix: Avoid 65000 vertices ArgumentException by discarding long entry and recurring entry.
* Add Duration declaration in mod_info.

# Version 1, 2020-03-18

* Enable console, toggled by `.
* Forward unity log to console, including game messages.