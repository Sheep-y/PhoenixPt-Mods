Changelog of Debug Console, a Phoenix Point Mod by Sheepy

# Version 3.1

* New: Api command "zy.gui.dump" to dump gui tree of given GameObject / Transform to modnix loader log.
* Fix: MethodInfo result from API will now be displayed by ToString.

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