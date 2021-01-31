using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Sheepy.PhoenixPt.ScriptingLibrary {
   using ModnixAPI = Func<string,object,object>;

   public class ScriptingLibrary : ZyMod {

      public static void Init () => SplashMod();

      public static void SplashMod ( ModnixAPI api = null ) {
         SetApi( api );
         LoadLibraries();
         ScriptingExt.RegisterAPI();
         DataCache.RegisterAPI();
         Task.Run( () => Api( "eval.js", "\"Scripting Warmup\"" ) );
      }

      private static readonly string[] ScriptNames = new string[] { "js", "javascript", "ecmascript" };

      public static object ActionMod ( string modId, Dictionary<string,object> action ) { try {
         object value = null, lang = null;
         if ( action?.TryGetValue( "eval", out value ) != true || ! ( value is string code ) ) return false;
         if ( action?.TryGetValue( "script", out lang ) != true || ! ( lang is string l ) || ! ScriptNames.Contains( l.Trim().ToLowerInvariant() ) ) return false;
         if ( Eval_Lib_Result is Exception lib_err ) return lib_err;
         Info( "Action> {0}", code );
         var result = ScriptingEngine.Eval( modId, code );
         if ( result is Exception ev_err ) return ev_err;
         Verbo( "Action< {0}", result );
         return true;
      } catch ( Exception ex ) { return ex; } }

      private static readonly string[] Eval_Libraries = new string[]{
         "ClearScript.Core.dll",
         "ClearScript.V8.dll",
         "ClearScript.Windows.dll",
         "ClearScript.Windows.Core.dll",
         //"ClearScriptV8.win-x64.dll", // Do not load. Not .Net.
      };

      private static object Eval_Lib_Result;

      internal static object LoadLibraries () { lock ( Eval_Libraries ) { try {
         // When a library fails to load, we won't want to retry it on every single action!
         if ( Eval_Lib_Result != null ) return Eval_Lib_Result;
         if ( ! HasApi ) throw new ApplicationException( "API not initiated" );
         Info( "Loading C# scripting libraries" );

         var lib = Path.Combine( Api( "dir" )?.ToString(), "lib" );
         foreach ( var f in Eval_Libraries ) {
            var path = Path.Combine( lib, f );
            if ( File.Exists( path ) ) {
               //Api( "log flush", "Loading " + path );
               //Verbo( "Loading {0}", path );
               var asm = Assembly.LoadFrom( path );
               Verbo( "Loaded {0}", asm?.FullName, asm.IsDynamic ? "(dynamic)" : asm.Location );
            } else
               Error( "Not found: {0}", path );
         }

         try { // Modnix 3 Beta 2 did not auto-load Microsoft libraries.
            Assembly.Load( "Microsoft.CSharp" );
         } catch ( Exception ) {
            var dn45dir = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.Windows ), "Microsoft.NET", "Framework", "v4.0.30319" );
            var dll = Path.Combine( dn45dir, "Microsoft.CSharp.dll" );
            Assembly.LoadFrom( dll ); // Yes, please throw on error
         }

         return Eval_Lib_Result = true;
      } catch ( Exception ex ) { return Eval_Lib_Result = ex; } } }
   }
}