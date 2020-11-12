# Sheepy's Phoenix Point Mods

Source Codes of my mods for [Phoenix Point](https://phoenixpoint.info/), [Snapshot Games](http://www.snapshotgames.com/) 2019.
DLLs are latest build and are less well-tested than those on NexusMods.
You can find NexusMods links below.

Unless otherwise noted, my mods tries not to modify game data,
meaning they shouldn't taint your save.
In other words, savegames made with the mods active should be safe to load when the mods are disabled.

Mods will be added as they are updated and optimised for [Modnix](https://github.com/Sheep-y/Modnix).

Some [unpublished mods](https://github.com/Sheep-y/PhoenixPt-Mods/tree/master/Unpublished) not found on NexusMods,
usually custom jobs or for self use, can be found here.

## List of Mods

Mods published on NexusMods, and tested on Phoenix Point 1.8.64176.

* [**Block Telemetry**](https://www.nexusmods.com/phoenixpoint/mods/48/) - Opt-out of and blocks telemetry.
* [**Debug Console**](https://www.nexusmods.com/phoenixpoint/mods/44/) - Enable console and direct unity log and game log to it.
* [**Dump Data**](https://www.nexusmods.com/phoenixpoint/mods/50/) - Dump game text and data into xml and csv.
* [**Full Body Augmentations**](https://www.nexusmods.com/phoenixpoint/mods/33) - Removes mutation and bionics cap.
* [**Geoscape Tweaks**](https://www.nexusmods.com/phoenixpoint/mods/13) - Enhance Geoscape screens.
* [**Globe Tweaks**](https://www.nexusmods.com/phoenixpoint/mods/13) - Enhance Geoscape pausing and centring.
* [**Independent Gear**](https://www.nexusmods.com/phoenixpoint/mods/33) - Unlocks all independent gears.
* [**No Ambush & No Nothing Found**](https://www.nexusmods.com/phoenixpoint/mods/12/) - Removes ambushes and/or found nothing random event.
* [**Recruit Info**](https://www.nexusmods.com/phoenixpoint/mods/28) - See recruit's skills, grafts, and equipments on haven screen.
* [**Scripting Library**](https://www.nexusmods.com/phoenixpoint/mods/49) - Support C# evaluations and Eval mod actions.
* [**Skip Intro**](https://www.nexusmods.com/phoenixpoint/mods/17) - Skip logos, openings, and landings.
* [**Tech Progression**](https://www.nexusmods.com/phoenixpoint/mods/33) - Unlock more things as you research.
* [**Traditional Chinese**](https://www.nexusmods.com/phoenixpoint/mods/47) - Convert Simplified Chinese to Traditional Chinese.

Mods published on NexusMods, and tested up to 1.5.x.

* [**Laser on Fire**](https://www.nexusmods.com/phoenixpoint/mods/33) - Add fire to lasers and change Destiny III aiming to first person.
* [**Limited War**](https://www.nexusmods.com/phoenixpoint/mods/24) - Tone down the scale and damage of faction war.
* [**Loot Everything**](https://www.nexusmods.com/phoenixpoint/mods/33) - Loot all items, optionally loot all armours and mounts.
* [**Mitigate Shred**](https://www.nexusmods.com/phoenixpoint/mods/33) - Convert absorbed damage (by armour) to shred.
* [**Scrap Vehicle**](https://www.nexusmods.com/phoenixpoint/mods/26) - Make it possible to recycle vehicles and mutogs.
* [**Variable Rage Burst**](https://www.nexusmods.com/phoenixpoint/mods/33) - Dynamic Rage Burst shots depending on weapon AP.

Legacy mods that are deprecated due to Phoenix Point updates:

* [**Deaf the Dead**](https://www.nexusmods.com/phoenixpoint/mods/45/) - Prevent dead units from hearing enemy movements.
* [**ODI Factors**](https://www.nexusmods.com/phoenixpoint/mods/33) - Adjust ODI scores and multipliers through PPDefModifier.

Unpublished mods.  No warrenty at all.

* [**Flat Difficulty**](https://github.com/Sheep-y/PhoenixPt-Mods/tree/master/Unpublished/FlatDifficulty) - Combat result no longer affect dynamic difficulty system.
* [**Half Price Bionics**](https://github.com/Sheep-y/PhoenixPt-Mods/tree/master/Unpublished/HalfPriceBionics) - Half the price of bionic augments.
* [**Less Recruit**](https://github.com/Sheep-y/PhoenixPt-Mods/tree/master/Unpublished/LessRecruit) - Remove an Assault from starting squads, last tested on 1.8.
* [**Stealing Is Bad**](https://github.com/Sheep-y/PhoenixPt-Mods/tree/master/Unpublished/StealingIsBad) - Doubles the penalty for raiding.
* [**Strong Means Tough**](https://github.com/Sheep-y/PhoenixPt-Mods/tree/master/Unpublished/StrongMeansTough) - Increase HP per Strength for all human soldiers, last tested on 1.8.
* [**Version Text**](https://github.com/Sheep-y/PhoenixPt-Mods/tree/master/Unpublished/VersionText) - Hello World mod to demo modding.

Any other mods you find will be work-in-progress.

## Loader Compatibility

All published mods are tested with [Modnix](https://github.com/Sheep-y/Modnix),
and usually also with [PPML v0.1](https://github.com/RealityMachina/PhoenixPointModInjector/#readme).

All my mods are *incompatible* with [PPML v0.2](https://github.com/Ijwu/PhoenixPointModLoader/#readme) found on [NexusMods](https://www.nexusmods.com/phoenixpoint/mods/38).
Because of how PPML v0.2 is designed, mods must choose one to support.

* 0.1 mods will be ignored by 0.2.
* 0.2 and 0.3 mods will *crash* PPML 0.1.

Since 0.1 is still used by old mods, and many players are not experts in mod management,
I opt for having my mods not work rather than crashing the game, when they are used incorrectly.
Thanks for your understanding.

(PPML 0.3 can load 0.1 mods, but mod that base itself on 0.3 would still crash PPML 0.1.)

## Compiling

Steps to build your own dlls from the source code.

1. Make sure the game is installed in `C:\Program Files\Epic Games\PhoenixPoint`.  The mods need to reference its dlls.
2. Download Visual Studio 2019, select ".Net Desktop Development" in the Installer.
3. Download the [whole repo](https://github.com/Sheep-y/PhoenixPt-Mods/archive/master.zip) and extract to a folder.
4. Double click `PhoenixPt_Mods.sln`.  This should open all the mods in Visual Studio.
5. Switch to "Release" build.  Debug will not build.
6. In Build menu, click "Build Solution".  For the first build you need to be online, and it may take a while.
7. Repeat once or twice if build fail.  Clear NuGet cache, Restore NuGet packages, close and delete .vs etc.
8. If everything is fine, you can find the mods in the `distro` folder.

If things are not fine, well, you need programming skill to solve it.
We need modders. Come join us!

## Tech Tree

Yup, I made the first complete tech tree for Phoenix Point.
Visit [Fandom wiki](https://phoenixpoint.fandom.com/) for [the image](https://phoenixpoint.fandom.com/wiki/File:Sheepy_Tech_Tree.gif).

The source code of my tech tree is in the [Tech Tree folder](https://github.com/Sheep-y/PhoenixPt-Mods/tree/master/TechTree).
It is charted with [UMLet](https://www.umlet.com/). SVG available.
Yeah.
Was supposed to be a draft.
Except that I have no plan to properly redo it.
Feel free to pick it up and carry on.