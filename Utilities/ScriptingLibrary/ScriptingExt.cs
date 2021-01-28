using Base.Utils.GameConsole;
using System;
using System.Linq;

namespace Sheepy.PhoenixPt.ScriptingLibrary {
   using ModnixAPI = Func<string,object,object>;

   internal class ScriptingExt : ZyMod {

      internal static void RegisterAPI () {
         Api( "api_add eval.js", (ModnixAPI) API_Eval_CS );
         //Api( "api_add zy.eval.cs", (ModnixAPI) API_Eval_CS );
      }

      private static object API_Eval_CS ( string spec, object param ) {
         var code = param?.ToString();
         if ( string.IsNullOrWhiteSpace( code ) ) return new ArgumentNullException( nameof( param ) );
         if ( ScriptingLibrary.LoadLibraries() is Exception err ) return err;
         Info( "API> {0}", code );
         var mod = ( Api( "api_stack mod" ) as string[] )?.FirstOrDefault( e => e != "Zy.cSharp" ) ?? "API";
         return ScriptingEngine.Eval( mod, code );
      }

      [ ConsoleCommand( Command = "Eval", Description = "(code) - Evaluate JavaScript code, or enter JavaScript shell if no code" ) ]
      public static void Console_Eval_CS ( IConsole console, string[] param ) {
         var code = string.Join( " ", param ?? Array.Empty<string>() ).Trim();
         if ( string.IsNullOrEmpty( code ) ) {
            ConsoleShell.OnCommand = EvalToConsole;
            ConsoleShell.EnterEvalMode( console );
         } else
            EvalToConsole( console, code );
      }

      private static void EvalToConsole ( IConsole console, string code ) {
         if ( ScriptingLibrary.LoadLibraries() is Exception err ) {
            console.Write( FormatException( err ) );
            return;
         }
         var result = ScriptingEngine.Eval( "Console", code );
         if ( result == null && code.IndexOf( '-' ) > 0 ) return; // Ignore "null" result from assignment statement.
         if ( result is Exception ex ) result = FormatException( ex ) + ex.StackTrace;
         if ( Api( "console.write", result ) is bool write && write ) return;
         else try { // Catch ToString() error
            console.Write( result + " (" + result.GetType().FullName + ")" );
         } catch ( Exception ) { console.Write( result.GetType().FullName ); }
      }

      private static string FormatException ( Exception ex ) => $"<color=red>{ex.GetType()} {ex.Message}</color>";
   }
}