using Base.Utils.GameConsole;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static System.Reflection.BindingFlags;

namespace Sheepy.PhoenixPt.DebugConsole {
   using ModnixAPI = Func< string, object, object >;
   using IShell = IDictionary< string, object >;
   using ConsoleHandler = Action< IConsole, string >;
   using CommandHandler = Func< string, object >;

   public class ConsoleShell : ZyMod {
      private static IPatch ReadPatch, EvalPatch, ShowLinePatch;
      private static readonly Stack< IShell > Shells = new Stack<IShell>();
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
               if ( Shells.Count == 0 ) return false;
               ExitShell();
               return true;
            case "start" : case null :
               var cmdShell = shell as IShell;
               if ( cmdShell == null ) return new ArgumentException( "shell start param must be IDictionary<string,object>" );
               Shells.Push( cmdShell );
               if ( string.IsNullOrWhiteSpace( Abbr ) || ! ( Handler is ConsoleHandler || Handler is CommandHandler ) ) {
                  Shells.Pop();
                  return new ArgumentException( "shell must have 'abbr' as string and 'handler' as Func<string,object> or Action<IConsole,string>" );
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
         SetConsoleInputPlaceholder( PlaceholderText, PlaceholderColour );
         console.Write( EnterMessage );
      }

      private static void SetConsoleInputPlaceholder ( string hint, Color color ) { try {
         var input = typeof( GameConsoleWindow ).GetField( "_inputField", NonPublic | Instance )?.GetValue( GameConsoleWindow.Create() ) as InputField;
         var text = input.placeholder.GetComponent<Text>();
         text.text = hint;
         text.color = color;
      } catch ( NullReferenceException ) { } }

      private static string Command;

      private static void BeforeReadCommandLine_GetCommand ( ref string commandLine ) {
         if ( commandLine == null ) return;
         Command = commandLine;
         commandLine = ""; // Bypass default execution
         if ( Command.Trim().Equals( ExitCommand, StringComparison.OrdinalIgnoreCase ) )
            ExitShell();
      }

      private static void BeforeExecuteCommandLine_ShowLine ( GameConsoleWindow __instance, string line, ref bool showLine ) {
         var fmt = FeedbackFormat;
         if ( ! showLine || string.IsNullOrWhiteSpace( fmt ) ) return;
         __instance.Write( fmt, line );
         showLine = false;
      }

      private static void AfterExecute_RunCommand ( IConsole context ) {
         var code = Command;
         Command = null;
         var handler = Handler;
         if ( code == null || handler == null ) return;
         object result = null;
         try {
            if ( handler is ConsoleHandler conFunc ) {
               conFunc( context, code );
               return;
            }
            if ( handler is CommandHandler objFunc ) {
               result = objFunc( code );
               if ( result is Exception ex ) throw ex;
               if ( Api( "console.write", result ) is bool write && write ) return;
               else try { // Catch ToString() error
                  context.Write( result + " (" + result.GetType().FullName + ")" );
               } catch ( Exception ) { context.Write( result.GetType().FullName ); }
            }
         } catch ( Exception ex ) {
            context.Write( $"<color=red>{ex.GetType()} {ex.Message}</color>" + ex.StackTrace );
         }
      }

      private static void ExitShell () {
         if ( Shells.Count == 1 ) {
            Command = null;
            Unpatch( ref ReadPatch );
            Unpatch( ref EvalPatch );
            Unpatch( ref ShowLinePatch );
            SetConsoleInputPlaceholder( "Enter command...", Color.grey );
            GameConsoleWindow.Create().Write( ExitMessage );
         }
         if ( Shells.Count > 0 ) Shells.Pop();
      }

      private static IShell CurrentShell => Shells.Count > 0 ? Shells.Peek() : null;
      private static string Abbr => CurrentShell?.GetText( "abbr" ) ?? "???";
      private static string EnterMessage => CurrentShell?.GetText( "enter_message" ) ?? $"Entering {Abbr} shell. Type '{ExitCommand}' to return.";
      private static string PlaceholderText => CurrentShell?.GetText( "placeholder_text" ) ?? $"Enter {Abbr}...";
      private static Color PlaceholderColour => CurrentShell?.TryGet( "placeholder_colour" ) is Color c ? c : Color.grey;
      private static string FeedbackFormat => CurrentShell?.GetText( "feedback_format" ) ?? $"{Abbr}> {{0}}";
      private static object Handler => CurrentShell?.TryGet( "handler" );
      private static string ExitCommand => CurrentShell?.GetText( "exit_command" ) ?? "Exit";
      private static string ExitMessage => CurrentShell?.GetText( "exit_message" ) ?? $"Exiting {Abbr} shell.";
   }

   internal static class Tools {
      internal static object TryGet  ( this IShell shell, string name ) =>
         shell.TryGetValue( name, out object val ) ? val : null;

      internal static string GetText ( this IShell shell, string name ) => shell.TryGet( name )?.ToString();
   }
}