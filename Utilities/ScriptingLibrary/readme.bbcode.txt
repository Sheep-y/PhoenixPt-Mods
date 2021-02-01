Requires [url=https://github.com/Sheep-y/Modnix#readme]Modnix[/url]﻿.

This mod bundles and bridges a JavaScript runttime,
so that mods can be written in JavaScript, a quick and easy scripting language.
JavaScript can run from in-game console, through Modnix API, or in form of Modnix Action mods.


[h2]Console[/h2]

The easiest way to start scripting is by using this with [url=https://www.nexusmods.com/phoenixpoint/mods/44/]Debug Console[/url] (ver 7+).

With Debug Console, you can open the in-game console with '`' key, and enter the "JavaScript" command
to enter the javascript shell, where everything you type will be evaluated as JavaScript on the fly.
For example, "1+2" should get you "3", and "Api.Call('mod_list')" should get you a list of mod ids, or 

Repo.Get( GeoVehicleDef, 'PP_Manticore_Def' ).BaseStats.Speed.Value

which gives you the current speed of the Manticore.

Note that [url=https://www.nexusmods.com/phoenixpoint/mods/7]Console Enabler[/url] does not provide console shells.
You may still test one-lined script, without double quote, e.g. "JavaScript 1+2".


[h2]Making Your Mod[/h2]

Once you are happy with your scripts, you can put them into a plain-text mod.

See [url=https://github.com/Sheep-y/Modnix/tree/master/Demo%20Mods#readme]Laser On Fire[/url] [url=https://github.com/Sheep-y/Modnix/blob/master/Demo%20Mods/LaserOnFire/LaserOnFire.js](code)[/url] for a simple example.

You can also call the "eval.js" api to evaluate and get the result of javascript.
For example, [url=https://www.nexusmods.com/phoenixpoint/mods/50]Dump Data[/url] allow users to define extra data to extract through JavaScript.


[h2]Why JavaScript?[/h2?]

Because this is the cleanest solution I found.

This mod bundles the [url=https://nodejs.dev/learn/the-v8-javascript-engine]V8 engine[/url] used by Google Chrome browser ([url=https://www.baekdal.com/thoughts/apple-is-limiting-the-new-google-chrome-for-ios/]except on iOS[/url]),
called through [url=https://github.com/microsoft/ClearScript#readme]Microsoft ClearScript[/url].
And ClearScript is just a wrapper.  Nothing (too) fancy.

My previous attempt, C# Scripting Library, depended on [url=https://github.com/dotnet/roslyn#readme]Rosyln[/url], which depends on [url=https://docs.microsoft.com/en-us/dotnet/framework/reflection-and-codedom/dynamic-language-runtime-overview]Dynamic Language Runtime[/url] and [url=https://www.c-sharpcorner.com/article/net-framework-vs-net-core-vs-net-standard/].Net Standard[/url], which is not only heavy and creates lots of dynamic assemblies that stay in memory forever, but must also override system libraries, and eventually after a few game updates the conflicts can no longer be solved.

There are other alternatives such as [url=https://ironpython.net/]IronPython[/url] and [url=http://ironruby.net/]IronRuby[/url].
They seem to be lighter than Rosyln, so may be a modder can make them works.
Me?  I am happy with JavaScript.  My second highest scored tag on stackoverflow, after Java.


[h2]PPML Support?[/h2]

Not planned because I don't think it is important.
Lots of mods to update, and I haven't play Phoenix Point at all.

You can fork and contribute code, if you can and want to.  Like how I updated PPDefModifier.
We can't be more happy to have helps.


[h2]Scripting Helpers[/h2]

Repo
Api
Log


[h2]API[/h2]

eval.js
pp.def
pp.defs