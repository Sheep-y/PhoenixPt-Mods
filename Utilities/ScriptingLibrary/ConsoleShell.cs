using Base.Utils.GameConsole;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static System.Reflection.BindingFlags;

namespace Sheepy.PhoenixPt.ScriptingLibrary {

   public class ConsoleShell : ZyMod {

      internal static Action<IConsole,string> OnCommand;

      private static IPatch ReadPatch, EvalPatch, ShowLinePatch;

      private static IDictionary< string, object > CurrentShell;
      // string          abbr - Short forms used for various default values.
      // string enter_message - Welcome notice.  Default "Entering {abbr} Shell.  Type '{exit_command}' to return."
      // string  exit_command - Command to exit, plain text.  Case insensitive.  Default "Exit".
      // string  exit_message - Exit notice.     Default "Existing {abbr} Shell."
      // string input_placeholder - Placeholder text of the input box.  Default "Enter {abbr}."
      // string feedback_format - How to format inputted commands.  Default "> {0}".
      // Action<IConsole,string> handler - The function that process each command line.  Required.

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
         SetConsoleInputPlaceholder( "Enter JS expression...", Color.blue );
         console.Write( "Entering JS Shell.  Type 'Exit' to return." );
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
         __instance.WriteLine( "JS> {0}", line );
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
         GameConsoleWindow.Create().Write( "Exiting JS Shell." );
      }

   }
}