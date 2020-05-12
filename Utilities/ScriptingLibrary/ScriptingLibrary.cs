using Base.Utils.GameConsole;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Sheepy.PhoenixPt.ScriptingLibrary {
   using ModnixAPI = Func<string,object,object>;

   public class ScriptingLibrary : ZyMod {

      public static void SplashMod ( ModnixAPI api ) {
         SetApi( api );
         Api( "api_add eval.cs", (ModnixAPI) API_Eval_CS );
         Task.Run( () => API_Eval_CS( null, "\"Scripting Warmup\"" ) );
      }

      public static object ActionMod ( Dictionary<string,object> action ) { try {
         object value = null;
         if ( action?.TryGetValue( "eval", out value ) != true || ! ( value is string code ) ) return false;
         if ( LoadLibraries() is Exception lib_err ) return lib_err;
         Info( "Action> {0}", code );
         var result = Eval.EvalCode( code );
         if ( result is Exception ev_err ) return ev_err;
         if ( result != null ) Info( "Action< {0}", result );
         return true;
      } catch ( Exception ex ) { return ex; } }

      public static object API_Eval_CS ( string spec, object param ) {
         var code = param?.ToString();
         if ( string.IsNullOrWhiteSpace( code ) ) return new ArgumentNullException( nameof( param ) );
         if ( LoadLibraries() is Exception err ) return err;
         Info( "API> {0}", code );
         return Eval.EvalCode( code );
      } 

      [ ConsoleCommand( Command = "Eval", Description = "(code) - Evaluate C# code" ) ]
      public static void Console_Eval_CS ( IConsole console, string[] param ) {
         if ( param == null || param.Length == 0 ) return;
         var code = string.Join( " ", param );
         if ( string.IsNullOrWhiteSpace( code ) ) return;
         if ( LoadLibraries() is Exception err ) {
            console.Write( "<color=red>" + err.GetType() + " " + err.Message + "</color>" );
            return;
         }
         var result = Eval.EvalCode( code );
         if ( Api( "console.write", result ) is bool write && write ) return;
         if ( result == null ) console.Write( "null" );
         else try { // Catch ToString() error
            console.Write( code + " (" + code.GetType().FullName + ")" );
         } catch ( Exception ) { console.Write( code.GetType().FullName ); }
      }

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

      private static object LoadLibraries () { lock ( Eval_Libraries ) { try {
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