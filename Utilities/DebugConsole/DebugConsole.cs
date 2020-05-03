using Base.Core;
using Base.Utils.GameConsole;
using Harmony;
using PhoenixPoint.Home.View.ViewModules;
using PhoenixPoint.Home.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityTools;
using static Harmony.AccessTools;

namespace Sheepy.PhoenixPt.DebugConsole {

   internal class ModConfig {
      public bool Mod_Count_In_Version = true;
      public bool Log_Game_Error = true;
      public bool Log_Game_Info = true;
      public bool Log_Modnix_Error = true;
      public bool Log_Modnix_Info = true;
      public bool Log_Modnix_Verbose = false;
      public bool Optimise_Log_File = true;
      public bool Scan_Mods_For_Command = true;
      public bool Write_Modnix_To_Console_Logfile = false;
      public int  Config_Version = 20200422;

      internal void Update () {
         if ( Config_Version < 20200422 ) {
            Config_Version = 20200422;
            ZyMod.Api( "config_save", this );
         }
      }
   }

   public class Mod : ZyMod {
      internal static volatile ModConfig Config;

      public static void Init () => new Mod().SplashMod();

      private static readonly DateTime StartTime = DateTime.Now;

      public void SplashMod ( Func< string, object, object > api = null ) {
         SetApi( api, out Config ).Update();
         GeneralPatch();
         ModnixPatch();
      }

      private void GeneralPatch () {
         GameConsoleWindow.DisableConsoleAccess = false; // Enable console first no matter what happens
         Verbo( "Console Enabled" );
         if ( Config.Log_Game_Error || Config.Log_Game_Info ) try {
            TryPatch( typeof( LogFormatter ), "LogFormat", prefix: nameof( UnityToConsole ) );
            if ( Config.Log_Game_Error ) {
               TryPatch( typeof( LogFormatter ), "LogException", prefix: nameof( UnityExToConsole ) );
               TryPatch( Inner( typeof( TimingScheduler ), "Updateable" ), "RaiseException", prefix: nameof( CatchSchedulerException ) );
            }
            TryPatch( typeof( TimingScheduler ), "Update", postfix: nameof( BufferToConsole ) );
         } catch ( Exception ex ) { Error( ex ); }
         if ( Config.Scan_Mods_For_Command && Extensions.InitScanner() ) {
            TryPatch( typeof( UIStateMainMenu ), "EnterState", prefix: nameof( ScanCommands ) );
            TryPatch( typeof( GameConsoleWindow ), "ToggleVisibility", postfix: nameof( ScanCommands ) );
            // Scan before lists are accessed. Messages are buffered and will appear after the commands.
            TryPatch( typeof( ConsoleCommandAttribute ), "GetCommands", prefix: nameof( ScanCommands ) );
            TryPatch( typeof( ConsoleCommandAttribute ), "GetInfo", prefix: nameof( ScanCommands ) );
            TryPatch( typeof( ConsoleCommandAttribute ), "HasCommand", prefix: nameof( ScanCommands ) );
         }
         if ( Config.Optimise_Log_File )
            TryPatch( typeof( GameConsoleWindow ), "AppendToLogFile", nameof( OverrideAppendToLogFile_AddToQueue ) );
      }

      private void ModnixPatch () {
         if ( ! HasApi ) return;
         Api( "api_add zy.ui.dump", (Func<object,bool>) Extensions.GuiTreeApi );
         if ( Config.Mod_Count_In_Version )
            TryPatch( typeof( UIStateMainMenu ), "EnterState", postfix: nameof( AfterMainMenu_AddModCount ) );
         if ( Config.Log_Modnix_Error || Config.Log_Modnix_Info || Config.Log_Modnix_Verbose ) {
            var loader = Api( "assembly", "modnix" ) as Assembly;
            var type = loader?.GetType( "Sheepy.Logging.FileLogger" );
            if ( type == null ) {
               Info( "Modnix assembly not found, log not forwarded." );
               return;
            }
            ModnixLogEntryLevel = loader.GetType( "Sheepy.Logging.LogEntry" ).GetField( "Level" );
            if ( ModnixLogEntryLevel == null ) Warn( "Modnix log level not found. All log forwarded." );
            TryPatch( type, "ProcessEntry", postfix: nameof( ModnixToConsole ) );
         }
      }

      private static void ScanCommands () => Extensions.ScanCommands();

      private static void AfterMainMenu_AddModCount ( UIStateMainMenu __instance ) { try {
         var revision = typeof( UIStateMainMenu ).GetProperty( "_buildRevisionModule", BindingFlags.NonPublic | BindingFlags.Instance )?.GetValue( __instance ) as UIModuleBuildRevision;
         var list = Api( "mod_list", null ) as IEnumerable<string>;
         if ( revision == null || list == null ) return;
         Info( "Adding to main menu version string" );
         var loader_ver = new Regex( "(?:\\.0){1,2}$" ).Replace( Api( "version", "loader" ).ToString(), "" );
         revision.BuildRevisionNumber.text += $", Modnix {loader_ver}, {list.Count()} mods.";
      } catch ( Exception ex ) { Error( ex ); } }

      #region Forward logs to Console
      private readonly static List<string> ConsoleBuffer = new List<string>();
      private static string LastLine;

      internal static void AddToConsoleBuffer ( string line ) {
         if ( string.IsNullOrWhiteSpace( line ) ) return;
         /*
         var pos = line.IndexOf( ' ' );
         if ( pos > 0 ) { // Skip duplicates
            var timeless = line.Substring( pos );
            if ( timeless.Equals( LastLine ) ) {
               OverrideAppendToLogFile_AddToQueue( line, null );
               return;
            }
            LastLine = timeless;
         }
         */
         lock ( ConsoleBuffer ) ConsoleBuffer.Add( line );
      }

      private static FieldInfo ModnixLogEntryLevel;

      private static void ModnixToConsole ( object entry, string txt ) { try {
         txt = txt.Trim();
         if ( txt.Length == 0 || txt.Length > 500 || txt.Contains( ".ModnixToConsole" ) ) return;
         string colour = "lime", level = "MODX";
         if ( ModnixLogEntryLevel != null ) {
            lock ( _SLock ) ; // Sync config
            switch ( (TraceEventType) ModnixLogEntryLevel.GetValue( entry ) ) {
               case TraceEventType.Critical: case TraceEventType.Error:
                  if ( ! Config.Log_Modnix_Error ) return;
                  colour = "fuchsia";
                  level = "EROR";
                  break;
                case TraceEventType.Warning:
                  if ( ! Config.Log_Modnix_Error ) return;
                  colour = "orange";
                  level = "WARN";
                  break;
               case TraceEventType.Information :
                  if ( ! Config.Log_Modnix_Info ) return;
                  level = "INFO";
                  break;
               default :
                  if ( ! Config.Log_Modnix_Verbose ) return;
                  colour = "aqua";
                  level = "VEBO";
                  break;
            }
         }
         var entryTime = ( entry.GetType().GetField( "Time" ).GetValue( entry ) as DateTime? ).Value;
         AddToConsoleBuffer( string.Format( "{0} <color={1}>{2} {3}</color><color=red></color>", FormatTime( entryTime - StartTime ), colour, level, txt ) );
      } catch ( Exception ex ) { Error( ex ); } }

      private static bool UnityToConsole ( LogType logType, object context, string format, params object[] args ) { try {
         string level = "INFO";
         lock ( _SLock ) ; // Sync config
         bool runOriginal = ! Config.Optimise_Log_File;
         switch ( logType ) {
            case LogType.Exception: case LogType.Error: case LogType.Warning:
               if ( ! Config.Log_Game_Error ) return runOriginal;
               level = "EROR";
               break;
            default:
               if ( ! Config.Log_Game_Info ) return runOriginal;
               break;
         }
         var time = DateTime.Now;
         Task.Run( () => { try {
            if ( level == "EROR" ) {
               level = "<color=red>EROR";
               format += "</color>";
            }
            string line = string.Format( format, args );
            if ( string.IsNullOrWhiteSpace( line ) ||
                 line.StartsWith( "[CONSOLE] " ) ||
                 line.Contains( "Called from a secondary Thread" ) ||
                 line.Contains( "UnityTools.LogFormatter" ) ||
                 line.Contains( ".UnityToConsole" ) )
               return;
            line = string.Format( "{0} {1} {2}", FormatTime( time - StartTime ), level, line );
            if ( line.Length > 500 ) {
               // Hide long message from console to avoid 65000 vertices error,
               // but show the type and message of exception
               if ( args?.Length == 1 && args[0] is Exception e )
                  UnityToConsole( LogType.Exception, context, "{0} {1}", e.GetType().FullName, e.Message );
               OverrideAppendToLogFile_AddToQueue( line, null );
            } else
               AddToConsoleBuffer( line );
         } catch ( Exception ex ) { Error( ex ); } } );
         return runOriginal;
      } catch ( Exception ex ) { return Error( ex ); } }

      private static string FormatTime ( TimeSpan logTime ) {
         return logTime.TotalSeconds.ToString( "F2" );
      }

      private static bool UnityExToConsole ( Exception exception, object context ) => UnityToConsole( LogType.Exception, context, "{0}", exception );

      private static void CatchSchedulerException ( object __instance, Exception ex ) => UnityExToConsole( ex, __instance );

      private static void BufferToConsole () { try {
         string[] lines;
         lock ( ConsoleBuffer ) {
            if ( ConsoleBuffer.Count == 0 ) return;
            lines = ConsoleBuffer.ToArray();
            ConsoleBuffer.Clear();
         }
         var console = GameConsoleWindow.Create();
         foreach ( var line in lines )
            console.WriteLine( EscLine( line ) );
      } catch ( Exception ex ) { Error( ex ); } }
      #endregion

      #region From console window to console log
      private readonly static List<KeyValuePair<DateTime,string>> WriteBuffer = new List<KeyValuePair<DateTime,string>>();
      private static Queue<string> LogFileQueue;
      private static Task LogWriteTask;

      [ HarmonyPriority( Priority.VeryLow ) ]
      private static bool OverrideAppendToLogFile_AddToQueue ( string line, Queue<string> ____logFileQueue ) {
         var entry = new KeyValuePair<DateTime,string>( DateTime.Now, line );
         if ( LogFileQueue == null ) {
            LogFileQueue = ____logFileQueue;
            if ( LogFileQueue == null ) return true;
         }
         lock ( WriteBuffer ) {
            WriteBuffer.Add( entry );
            if ( LogWriteTask == null )
               LogWriteTask = Task.Run( BufferToWriteQueue );
         }
         return false;
      }

      // Split file lock from queue lock, so that main thread will not be stuck on log IO
      private static async void BufferToWriteQueue () { try {
         await Task.Delay( 200 );
         KeyValuePair<DateTime,string>[] entries;
         lock ( WriteBuffer ) {
            entries = WriteBuffer.ToArray();
            WriteBuffer.Clear();
            LogWriteTask = null;
         }
         var lines = entries.Select( FormatConsoleLog ).Where( e => e != null ).ToArray();
         lock ( LogFileQueue ) {
            foreach ( var line in lines )
               LogFileQueue.Enqueue( line );
            Monitor.Pulse( LogFileQueue );
         }
      } catch ( Exception ex ) { Error( ex ); } }

      private static Regex RegexColour = new Regex( "</?color(=#?\\w+| )?>", RegexOptions.Compiled );
      private static Regex RegexModnixLine = new Regex( "</color><color=red></color>$", RegexOptions.Compiled );

      private static string FormatConsoleLog ( KeyValuePair<DateTime,string> entry ) {
         var line = entry.Value;
         if ( string.IsNullOrWhiteSpace( line ) ) return null;
         if ( HasApi && RegexModnixLine.IsMatch( line ) && ! Config.Write_Modnix_To_Console_Logfile ) return null;
         line = EscLine( RegexColour.Replace( line, "" ) ); // Prevent log writer thread from throwing error on string.Format.
         return entry.Key.ToString( "u" ).Substring( 11, 8 ) + " | " + line;
      }
      #endregion

      private static string EscLine ( string line ) => line.Replace( "{", "{{" ).Replace( "}", "}}" );
   }
}