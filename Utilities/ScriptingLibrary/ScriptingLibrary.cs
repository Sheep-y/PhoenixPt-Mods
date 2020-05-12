using Base.Utils.GameConsole;
using Harmony;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static System.Reflection.BindingFlags;

namespace Sheepy.PhoenixPt.ScriptingLibrary {
   using ModnixAPI = Func<string,object,object>;

   public class ScriptingLibrary : ZyMod {
      internal static ScriptingLibrary Mod;

      public void SplashMod ( ModnixAPI api ) {
         Mod = this;
         SetApi( api );
         Api( "api_add eval.cs", (ModnixAPI) API_Eval_CS );
         Api( "api_add zy.eval.cs", (ModnixAPI) API_Eval_CS );
         Task.Run( () => API_Eval_CS( null, "\"Scripting Warmup\"" ) );
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

      public static object API_Eval_CS ( string spec, object param ) {
         var code = param?.ToString();
         if ( string.IsNullOrWhiteSpace( code ) ) return new ArgumentNullException( nameof( param ) );
         if ( LoadLibraries() is Exception err ) return err;
         Info( "API> {0}", code );
         return Shell.Eval( "API", code ); // TODO: Change to use caller mod's session
      }

      [ ConsoleCommand( Command = "Eval", Description = "(code) - Evaluate C# code, or enter C# shell if no code" ) ]
      public static void Console_Eval_CS ( IConsole console, string[] param ) {
         var code = string.Join( " ", param ?? new string[0] ).Trim();
         if ( string.IsNullOrEmpty( code ) )
            EnterEvalMode( console );
         else
            EvalToConsole( console, code );
      }

      private static void EvalToConsole ( IConsole console, string code ) {
         if ( LoadLibraries() is Exception err ) {
            console.Write( "<color=red>" + err.GetType() + " " + err.Message + "</color>" );
            return;
         }
         var result = Shell.Eval( "Console", code );
         if ( result == null ) return;
         if ( result is Exception ex ) result = "<color=red>" + ex.GetType() + " " + ex.Message + "</color>" + ex.StackTrace;
         if ( Api( "console.write", result ) is bool write && write ) return;
         else try { // Catch ToString() error
            console.Write( code + " (" + code.GetType().FullName + ")" );
         } catch ( Exception ) { console.Write( code.GetType().FullName ); }
      }

      private static IPatch ReadPatch, EvalPatch, ShowLinePatch;

      private static void EnterEvalMode ( IConsole console ) {
         if ( EvalPatch != null || Mod == null ) return;
         ReadPatch = Mod.Patch( GameAssembly.GetType( "Base.Utils.GameConsole.CommandLineParser" ), "ReadCommandLine", nameof( BeforeReadCommandLine_Eval ) );
         EvalPatch = Mod.Patch( GameAssembly.GetType( "Base.Utils.GameConsole.EmptyCommand" ), "Execute", postfix: nameof( AfterExecute_Eval ) );
         ShowLinePatch = Mod.TryPatch( typeof( GameConsoleWindow ).GetMethod( "ExecuteCommandLine", new Type[]{ typeof( string ), typeof( bool ), typeof( bool ) } ),
            prefix: nameof( BeforeExecuteCommandLine_ShowLine ) );
         SetConsoleInputPlaceholder( "Enter C# expression...", Color.blue );
         console.Write( "Entering C# Shell.  Type 'Exit' to return." );
      }

      private static void SetConsoleInputPlaceholder ( string hint, Color color ) { try {
         var input = typeof( GameConsoleWindow ).GetField( "_inputField", NonPublic | Instance )?.GetValue( GameConsoleWindow.Create() ) as InputField;
         var text = input.placeholder.GetComponent<Text>();
         text.text = hint;
         text.color = color;
      } catch ( NullReferenceException ) { } }

      private static string CommandLine;

      private static void BeforeReadCommandLine_Eval ( ref string commandLine ) {
         commandLine = commandLine?.Trim();
         if ( string.IsNullOrEmpty( commandLine ) ) return;
         CommandLine = commandLine;
         commandLine = "";
         if ( "exit".Equals( CommandLine.ToLowerInvariant() ) )
            ExitEvalMode();
      }

      private static void BeforeExecuteCommandLine_ShowLine ( GameConsoleWindow __instance, string line, ref bool showLine ) {
         if ( ! showLine ) return;
         __instance.WriteLine( "C#> {0}", line );
         showLine = false;
      }

      private static void AfterExecute_Eval ( IConsole context ) {
         var code = CommandLine;
         CommandLine = null;
         EvalToConsole( context, code );
      }

      private static void ExitEvalMode () {
         Unpatch( ref ReadPatch );
         Unpatch( ref EvalPatch );
         Unpatch( ref ShowLinePatch );
         SetConsoleInputPlaceholder( "Enter command...", Color.grey );
         GameConsoleWindow.Create().Write( "Exiting C# Shell." );
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