Changelog of Debug Console, a Phoenix Point Mod by Sheepy

# Version ?

* Fix: ConsoleEnabler mod now properly disabled.

# Version 2.0, 2020-03-25

* New: Modnix mod logs are now forwarded.  Not written to console log by default.
* New: Configurable log level.
* Fix: Avoid graphic rebulid loop error by caching logs and build on TimingScheduler.Update.
* Fix: Avoid 65000 vertices ArgumentException by discarding long entry and recurring entry.
* Add Duration declaration in mod_info.

# Version 1.0, 2020-03-18

* Enable console, toggled by `.
* Forward unity log to console, including game messages.