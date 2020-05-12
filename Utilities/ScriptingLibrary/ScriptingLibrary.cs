using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Sheepy.PhoenixPt.ScriptingLibrary {
   using ModnixAPI = Func<string,object,object>;

   public class ScriptingLibrary : ZyMod {

      public void SplashMod ( ModnixAPI api ) {
         SetApi( api );
         ScriptingExt.RegisterAPI();
         Api( "zy.eval.cs", "\"Scripting Warmup\"" );
      }

      public static object ActionMod ( string modId, Dictionary<string,object> action ) { try {
         object value = null;
         if ( action?.TryGetValue( "eval", out value ) != true || ! ( value is string code ) ) return false;
         if ( LoadLibraries() is Exception lib_err ) return lib_err;
         Info( "Action> {0}", code );
         var result = Shell.Eval( modId, code );
         if ( result is Exception ev_err ) return ev_err;
         if ( result != null ) Info( "Action< {0}", result );
         return true;
      } catch ( Exception ex ) { return ex; } }

      private static readonly string[] Eval_Libraries = new string[]{
         "netstandard.dll",
         "Microsoft.Win32.Primitives.dll",
         "System.Buffers.dll",
         "System.Collections.Immutable.dll",
         "System.Memory.dll",
         "System.Numerics.Vectors.dll",
         "System.Reflection.Metadata.dll",
         "System.Runtime.CompilerServices.VisualC.dll",
         "System.Security.Principal.dll",
         "System.Threading.Tasks.Extensions.dll",
         "System.ValueTuple.dll",
         "Microsoft.CodeAnalysis.CSharp.dll",
         "Microsoft.CodeAnalysis.CSharp.Scripting.dll",
         "Microsoft.CodeAnalysis.dll",
         "Microsoft.CodeAnalysis.Scripting.dll" };

      private static object Eval_Lib_Result;

      internal static object LoadLibraries () { lock ( Eval_Libraries ) { try {
         // When a library fails to load, we won't want to retry it on every single action!
         if ( Eval_Lib_Result != null ) return Eval_Lib_Result;
         if ( ! HasApi ) throw new ApplicationException( "API not initiated" );
         Info( "Loading C# scripting libraries" );

         var lib = Path.Combine( Api( "dir" )?.ToString(), "lib" );
         foreach ( var f in Eval_Libraries ) {
            var path = Path.Combine( lib, f );
            Verbo( "Loading {0}", path );
            Assembly.LoadFrom( path );
         }
         return Eval_Lib_Result = true;
      } catch ( Exception ex ) { return Eval_Lib_Result = ex; } } }
   }
}