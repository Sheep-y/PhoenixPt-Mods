using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Sheepy.PhoenixPt.ScriptingLibrary {
   using ModnixAPI = Func<string,object,object>;

   public class ScriptingLibrary : ZyMod {

      public void SplashMod ( ModnixAPI api ) {
         SetApi( api );
         CopyLib();
         ScriptingExt.RegisterAPI();
         DataCache.RegisterAPI();
         Task.Run( () => Api( "eval.cs", "\"Scripting Warmup\"" ) );
      }

      public static object ActionMod ( string modId, Dictionary<string,object> action ) { try {
         object value = null;
         if ( action?.TryGetValue( "eval", out value ) != true || ! ( value is string code ) ) return false;
         if ( LoadLibraries() is Exception lib_err ) return lib_err;
         Info( "Action> {0}", code );
         var result = ScriptingEngine.Eval( modId, code );
         if ( result is Exception ev_err ) return ev_err;
         Verbo( "Action< {0}", result );
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

      // Some libraries, specifically System.Numerics.Vectors.dll, works only when copied to code folder.
      // Thus, to play it safe, we try to copy all critical dlls.
      public static void CopyLib () {
         foreach ( var lib in Eval_Libraries ) { try {
            var from = Path.Combine( Api( "dir" ).ToString(), "lib", lib );
            var to = Path.Combine( Api( "dir", "loader" ).ToString(), lib );
            if ( ! File.Exists( from ) ) return;
            if ( File.Exists( to ) && new FileInfo( to ).Length == new FileInfo( from ).Length ) return;
            Info( "Copy {0} to {1}", lib, to );
            File.Copy( from, to );
         } catch ( SystemException ex ) {
            Warn( ex );
         } }
      }

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
               //Verbo( "Loading {0}", path );
               var asm = Assembly.LoadFrom( path );
               Verbo( "Loaded {0}", asm?.FullName, asm.IsDynamic ? "(dynamic)" : asm.Location );
            } else
               Error( "Not found: {0}", path );
         }
         return Eval_Lib_Result = true;
      } catch ( Exception ex ) { return Eval_Lib_Result = ex; } } }
   }
}