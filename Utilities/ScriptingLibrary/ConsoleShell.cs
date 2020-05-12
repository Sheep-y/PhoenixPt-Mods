using Base.Utils.GameConsole;
using System;
using UnityEngine;
using UnityEngine.UI;
using static System.Reflection.BindingFlags;

namespace Sheepy.PhoenixPt.ScriptingLibrary {

   public class ConsoleShell : ZyMod {

      internal static Action<IConsole,string> OnCommand;

      private static IPatch ReadPatch, EvalPatch, ShowLinePatch;

      internal static void EnterEvalMode ( IConsole console ) {
         if ( EvalPatch != null ) return;
         var me = new ConsoleShell();
         ReadPatch = me.TryPatch( GameAssembly.GetType( "Base.Utils.GameConsole.CommandLineParser" ), "ReadCommandLine", nameof( BeforeReadCommandLine_Eval ) );
         EvalPatch = me.TryPatch( GameAssembly.GetType( "Base.Utils.GameConsole.EmptyCommand" ), "Execute", postfix: nameof( AfterExecute_Eval ) );
         if ( ReadPatch == null || EvalPatch == null ) {
            Unpatch( ref ReadPatch );
            Unpatch( ref EvalPatch );
            return;
         }
         ShowLinePatch = me.TryPatch( typeof( GameConsoleWindow ).GetMethod( "ExecuteCommandLine", new Type[]{ typeof( string ), typeof( bool ), typeof( bool ) } ),
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
         OnCommand( context, code );
      }

      private static void ExitEvalMode () {
         Unpatch( ref ReadPatch );
         Unpatch( ref EvalPatch );
         Unpatch( ref ShowLinePatch );
         SetConsoleInputPlaceholder( "Enter command...", Color.grey );
         GameConsoleWindow.Create().Write( "Exiting C# Shell." );
      }

   }
}