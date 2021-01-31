using Base.Utils.GameConsole;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static System.Reflection.BindingFlags;

namespace Sheepy.PhoenixPt.ScriptingLibrary {
   using ModnixAPI = Func< string, object, object >;
   using IShell = IDictionary< string, object >;
   using CommandHandler = Action< IConsole, string >;

   public class ConsoleShell : ZyMod {
      private static IPatch ReadPatch, EvalPatch, ShowLinePatch;
      private static IShell CurrentShell;
      // string          abbr - Short forms used for various default values.  Required.
      //  ???   enter_command - Each mod should define their own enter command.
      // string enter_message - Welcome notice.  Default "Entering {abbr} shell.  Type '{exit_command}' to return."
      // string  exit_command - Command to exit, plain text.  Case insensitive.  Default "Exit".
      // string  exit_message - Exit notice.     Default "Existing {abbr} shell."
      // string placeholder_text - Placeholder text of the input box.  Default "Enter {abbr}..."
      // Color placeholder_colour - Placeholder text colour of the input box.  Default UnityEngine.Color.grey
      // string feedback_format - How to format inputted commands.  Default "{abbr}> {0}".
      // Action<IConsole,string> handler - The function that process each command line.  Required.

      public void SplashMod ( ModnixAPI api ) {
         SetApi( api );
         Api( "api_add console.shell", (ModnixAPI) StartShell );
      }

      internal object StartShell ( string command, object shell ) {
         switch ( command?.ToLowerInvariant() ) {
            case "stop" : 
               if ( CurrentShell == null ) return false;
               ExitShell();
               return true;
            case "start" : case null :
               if ( CurrentShell != null ) ExitShell();
               CurrentShell = shell as IShell;
               if ( CurrentShell == null ) return new ArgumentException( "shell start param must be IDictionary<string,object>" );
               if ( string.IsNullOrWhiteSpace( Abbr ) || Handler == null ) {
                  CurrentShell = null;
                  return new ArgumentException( "shell must have 'abbr' as string and 'handler' as Action<IConsole,string>" );
               }
               EnterShell( GameConsoleWindow.Create() );
               return true;
            default :
               return new ArgumentException( "Unknown console.shell spec: " + command );
         }
      }

      internal void EnterShell ( IConsole console ) {
         if ( EvalPatch != null ) return;
         ReadPatch = TryPatch( GameAssembly.GetType( "Base.Utils.GameConsole.CommandLineParser" ), "ReadCommandLine", nameof( BeforeReadCommandLine_GetCommand ) );
         EvalPatch = TryPatch( GameAssembly.GetType( "Base.Utils.GameConsole.EmptyCommand" ), "Execute", postfix: nameof( AfterExecute_RunCommand ) );
         if ( ReadPatch == null || EvalPatch == null ) {
            Api( "log w", "Cannot create console shell - patch failed" );
            Unpatch( ref ReadPatch );
            Unpatch( ref EvalPatch );
            return;
         }
         ShowLinePatch = TryPatch( typeof( GameConsoleWindow ).GetMethod( "ExecuteCommandLine", new Type[]{ typeof( string ), typeof( bool ), typeof( bool ) } ),
            prefix: nameof( BeforeExecuteCommandLine_ShowLine ) );
         SetConsoleInputPlaceholder( "Enter JS expression...", Color.blue );
         console.Write( EnterMessage );
      }

      private static void SetConsoleInputPlaceholder ( string hint, Color color ) { try {
         var input = typeof( GameConsoleWindow ).GetField( "_inputField", NonPublic | Instance )?.GetValue( GameConsoleWindow.Create() ) as InputField;
         var text = input.placeholder.GetComponent<Text>();
         text.text = hint;
         text.color = color;
      } catch ( NullReferenceException ) { } }

      private static string CommandLine;

      private static void BeforeReadCommandLine_GetCommand ( ref string commandLine ) {
         commandLine = commandLine?.Trim();
         if ( string.IsNullOrEmpty( commandLine ) ) return;
         CommandLine = commandLine;
         commandLine = ""; // Bypass default execution
         if ( commandLine.Equals( ExitCommand, StringComparison.OrdinalIgnoreCase ) )
            ExitShell();
      }

      private static void BeforeExecuteCommandLine_ShowLine ( GameConsoleWindow __instance, string line, ref bool showLine ) {
         var fmt = FeedbackFormat;
         if ( ! showLine || string.IsNullOrWhiteSpace( fmt ) ) return;
         __instance.WriteLine( fmt, line );
         showLine = false;
      }

      private static void AfterExecute_RunCommand ( IConsole context ) {
         var code = CommandLine;
         CommandLine = null;
         var handler = Handler;
         if ( code == null || handler == null ) return;
         handler( context, code );
      }

      private static void ExitShell () {
         CommandLine = null;
         Unpatch( ref ReadPatch );
         Unpatch( ref EvalPatch );
         Unpatch( ref ShowLinePatch );
         SetConsoleInputPlaceholder( "Enter command...", Color.grey );
         GameConsoleWindow.Create().Write( ExitMessage );
         CurrentShell = null;
      }

      private static string Abbr => CurrentShell?.GetText( "abbr" ) ?? "???";
      private static string EnterMessage => CurrentShell?.GetText( "enter_message" ) ?? $"Entering {Abbr} shell. Type '{ExitCommand}' to return.";
      private static string PlaceholderText => CurrentShell?.GetText( "placeholder_text" ) ?? $"Enter {Abbr}...";
      private static Color PlaceholderColour => CurrentShell?.TryGet( "placeholder_colour" ) is Color c ? c : Color.grey;
      private static string FeedbackFormat => CurrentShell?.GetText( "feedback_format" ) ?? "{Abbr}> {0}";
      private static CommandHandler Handler => CurrentShell?.TryGet( "handler" ) as CommandHandler;
      private static string ExitCommand => CurrentShell?.GetText( "exit_command" ) ?? "Exit";
      private static string ExitMessage => CurrentShell?.GetText( "exit_message" ) ?? $"Exiting {Abbr} shell.";
   }

   internal static class Tools {

      internal static object TryGet  ( this IShell shell, string name ) =>
         shell.TryGetValue( name, out object val ) ? val : null;

      internal static string GetText ( this IShell shell, string name ) => shell.TryGet( name )?.ToString();
   }
}