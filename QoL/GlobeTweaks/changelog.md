Changelog of Globe Tweaks, a Phoenix Point Mod by Sheepy

# Version 7, 2020-10-28

* Fix: Setting both Auto_Unpause_Single and Auto_Unpause_Multiple to false is now working.  Previously one of them need to be true.
* Update recruit info to support Phoenix Point 1.7.  Not backward compatible with older game versions.
* Config Haven_Icons.Always_Show_Soldier merged with Always_Show_Action, fine control is lost due to fov architecture change.
* Remove code and config related to scanner, because active scanning is removed since Phoenix Point 1.6.
* Tested on Phoenix Point 1.7.3.62880.

# Version 6.1, 2020-07-10

* Update vehicle movement pause for Danforth (1.5.2).  Should still work on Pre-Danforth installations.
* Setting Hours_Format / Days_Format / Haven_Icons to null no longer throw NRE.
* Tested on Phoenix Point 1.5.3.59629.

# Version 6, 2020-06-03

* New: Same_Site_Scan_Cooldown_Min, prevents duplicate scan on same site.
* New: Show_Airplane_Action_Time now show remaining time of active scanner, if any.
* Fix: Vehicle recruits are properly handled for haven mouseovers.
* Change "No_Auto_Unpause" config to "Auto_Unpause_Multiple".
* Add "Auto_Unpause_Single" config.
* Explore menu now shows remaining time for ongoing exploration.
* Support Modnix 3 lazy loading.
* Tested on Phoenix Point 1.0.57630.

# Version 5, 2020-05-02

* Updated to game version 1.0.57335.  Haven popup will not show trade offers in earlier game versions.
* New: Hide_Recruit_Stickman, default true.
* New: Show_Airplane_Action_Time, default true, and related format strings Hours_Format and Days_Format.
* Trade text in haven popup is no longer italic.  Motto text unaffected.
* Mod no longer configurable with Modnix 1.
* Fixed NexusMods link.

# Version 4, 2020-04-19

* New: Always show haven recruit icons and resources icons regardless of zoom.
* New: Always show base soldier count tag regardless of zoom.
* New: Show recruit class and trade offers in haven mouseover hint.
* Updated to Modnix 2.

# Version 3.1.1, 2020-03-25

* Fix: Heal message and rest message are no longer swapped.

# Version 3.1, 2020-03-24

* Renamed Pause_On_HP_Only_Heal to Notice_On_HP_Only_Heal.
* Renamed Pause_On_Stamina_Only_Heal to Notice_On_Stamina_Only_Heal.

# Version 3, 2020-03-24

* Updated to Modnix 1.0.
* New: Pause on full stamina (default on), pause on full health (default off)
* New: Separate vehicle heal and base heal options.
* Fix: Pause on heal now properly updates time controller state.
* Isolate each features; failure of one would not affect the others.

# Version 2, 2020-03-16

* Merged into one mod as a configurable Modnix mod.
* Now also pause on base heal.

# Version 1

Delivered in form of three mods:

* No Auto Unpause, 2019-12-16
* Center On New Base, 2019-12-28
* Pause On Heal, 2020-01-09