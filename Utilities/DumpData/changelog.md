Changelog of Dump Data, a Phoenix Point Mod by Sheepy

# Version 2.2, 2021-02-01

* Fix: Directory with same name as dump files will now be deleted for the dumps.
* Add research browser, an offline html page that can load and better present research data.
* Add AIActionDef, HumanCustomizationDef, and SharedGameTagsDataDef.
* Split EntitlementDef to a separate dump file by default.
* New guid dump column: ResourcePath.
* Dump_Others now ignore empty entries, and switched from eval.cs to eval.js.
* Better fill Guid dump's view element columns.

# Version 2.1, 2020-08-29

* Fix: Non-readble properties are now skipped.
* Fix: Type objects (System) and Reflection objects are no longer dumped recursively.
* Tested on Phoenix Point 1.7.61722.

# Version 2, 2020-07-01

* "dump.xml" API to dump any object to xml.
* Skip "name" and "Guid" of base defs because they are already included in open tag.
* Auto dump may be disabled.
* Recognise empty LocalizedTextBind.
* Ignore KeyValuePair element type for IDicitonary.
* Guid list now includes "Tags" field.
* Explain how to disable "missing scripting library" warning in Modnix 2.
* Tested on Phoenix Point 1.0.58929.

# Version 1, 2020-06-13

* Dump selected BaseDefs, TermData, a few game settings, plus guid list and console command list.
* Mod is designed for Modnix 3; other loaders may yield incomplete data.
* Tested on Phoenix Point 1.0.57630 and 1.0.58746 with Modnix 3.
