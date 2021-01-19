({
   Id : "Zy.cSharp",
   Name : "Scripting Library (Beta)",
   Description : 'Supports runtime C# evaluation, such as "Eval" mod actions (Modnix 3), "Eval" console command, and "eval.cs" api.\r\rProvides data lookup by name and path through "pp.def" and "pp.defs" api.\r\rMods may also directly import helpers from ScriptHelpers.',
   LoadIndex : -200, // Load early to pre-load scripting engine.
   Requires: [{ Id: "Modnix", Min: "3.0.2021.0120" }],
   Url : {
      "Nexus" : "https://nexusmods.com/phoenixpoint/mods/",
      "GitHub" : "https://github.com/Sheep-y/PhoenixPt-Mods/",
   },
   Preloads: [
      /**
      "lib/netstandard.dll",
      "lib/System.Buffers.dll",
      "lib/System.Collections.Immutable.dll",
      "lib/System.Memory.dll",
      "lib/System.Numerics.Vectors.dll",
      "lib/System.Reflection.Metadata.dll",
      "lib/System.Runtime.CompilerServices.VisualC.dll",
      "lib/System.Security.Principal.dll",
      "lib/System.Threading.Tasks.Extensions.dll",
      "lib/System.ValueTuple.dll",
      "lib/Microsoft.CodeAnalysis.CSharp.dll",
      "lib/Microsoft.CodeAnalysis.CSharp.Scripting.dll",
      "lib/Microsoft.CodeAnalysis.dll",
      "lib/Microsoft.CodeAnalysis.Scripting.dll" 
      "lib/Microsoft.Win32.Primitives.dll",
      /**/
   ],
})