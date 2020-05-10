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

namespace Sheepy.PhoenixPt.DebugConsole {

   internal class ModConfig {
      public bool Mod_Count_In_Version = true;
      public bool Log_Game_Error = true;
      public bool Log_Game_Info = true;
      public bool Log_Modnix_Error = true;
      public bool Log_Modnix_Info = true;
      public bool Log_Modnix_Verbose = false;
      //public bool Optimise_Log_File = true;
      public bool Scan_Mods_For_Command = true;
      public bool Write_Modnix_To_Console_Logfile = false;
      public int  Config_Version = 20200503;

      internal bool Log_Game => Log_Game_Error || Log_Game_Info;
      internal bool Log_Modnix => Log_Modnix_Error || Log_Modnix_Info || Log_Modnix_Verbose;

      internal void Update () {
         if ( Config_Version < 20200422 ) { // Keep 0422 which has Optimise_Log_File
            Config_Version = 20200503;
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
         if ( Config.Log_Game ) try {
            TryPatch( typeof( LogFormatter ), "LogFormat", prefix: nameof( UnityToConsole ) );
            if ( Config.Log_Game_Error ) {
               TryPatch( typeof( LogFormatter ), "LogException", prefix: nameof( UnityExToConsole ) );
               TryPatch( AccessTools.Inner( typeof( TimingScheduler ), "Updateable" ), "RaiseException", prefix: nameof( CatchSchedulerException ) );
            }
            TryPatch( typeof( TimingScheduler ), "Update", postfix: nameof( BufferToConsole ) );
         } catch ( Exception ex ) { Error( ex ); }
         if ( Config.Scan_Mods_For_Command )
            new ExtModule().InitMod();
         if ( Config.Log_Game || Config.Log_Modnix )
            TryPatch( typeof( GameConsoleWindow ), "AppendToLogFile", nameof( OverrideAppendToLogFile_AddToQueue ) );
      }

      private void ModnixPatch () {
         if ( ! HasApi ) return;
         ExtModule.RegisterApi();
         if ( Config.Mod_Count_In_Version )
            TryPatch( typeof( UIStateMainMenu ), "EnterState", postfix: nameof( AfterMainMenu_AddModCount ) );
         if ( Config.Log_Modnix ) {
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

      private static void AfterMainMenu_AddModCount ( UIStateMainMenu __instance ) { try {
         var revision = typeof( UIStateMainMenu ).GetProperty( "_buildRevisionModule", BindingFlags.NonPublic | BindingFlags.Instance )?.GetValue( __instance ) as UIModuleBuildRevision;
         var list = Api( "mod_list", null ) as IEnumerable<string>;
         if ( revision == null || list == null ) return;
         Info( "Adding to main menu version string" );
         var loader_ver = new Regex( "(?:\\.0){1,2}$" ).Replace( Api( "version", "loader" ).ToString(), "" );
         revision.BuildRevisionNumber.text += $", Modnix {loader_ver}, {list.Count()} mods.";
      } catch ( Exception ex ) { Error( ex ); } }

      #region Log capturing
      private static FieldInfo ModnixLogEntryLevel;

      private static void ModnixToConsole ( object entry, string txt ) { try {
         txt = txt.Trim();
         if ( txt.Length == 0 || txt.Contains( ".ModnixToConsole" ) ) return;
         string colour = "lime", level = "MODX";
         if ( ModnixLogEntryLevel != null ) {
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
         WriteConsole( entryTime, $"<color={colour}>{level}", txt + "</color><b></b>" );
      } catch ( Exception ex ) { Error( ex ); } }

      private static bool UnityExToConsole ( Exception exception, object context ) => UnityToConsole( LogType.Exception, context, "{0}", exception );

      private static void CatchSchedulerException ( object __instance, Exception ex ) => UnityExToConsole( ex, __instance );

      private static bool UnityToConsole ( LogType logType, object context, string format, params object[] args ) { try {
         string level = "INFO";
         switch ( logType ) {
            case LogType.Exception: case LogType.Error: case LogType.Warning:
               if ( ! Config.Log_Game_Error ) return false;
               level = "EROR";
               break;
            default:
               if ( ! Config.Log_Game_Info ) return false;
               break;
         }
         var time = DateTime.Now;
         Task.Run( () => { try {
            string line = format;
            if ( level == "EROR" ) {
               level = "<color=red>EROR";
               format += "</color>";
            }
            try {
               line = string.Format( format, args );
            } catch ( FormatException ) { }
            if ( string.IsNullOrWhiteSpace( line ) ||
                 line.StartsWith( "[CONSOLE] " ) ||
                 line.Contains( "Called from a secondary Thread" ) ||
                 line.Contains( "UnityTools.LogFormatter" ) ||
                 line.Contains( ".UnityToConsole" ) )
               return;
            WriteConsole( time, level, line );
         } catch ( Exception ex ) { Error( ex ); } } );
         return false;
      } catch ( Exception ex ) { return Error( ex ); } }
      #endregion

      #region Forward logs to Console
      private readonly static List<string> ConsoleBuffer = new List<string>();
      //private static string LastLine;

      internal static void WriteConsole ( string line ) => WriteConsole( DateTime.Now, "", line );

      internal static void WriteConsole ( DateTime time, string level, string line ) {
         if ( string.IsNullOrWhiteSpace( line ) ) return;
         if ( time < StartTime ) time = StartTime;
         var fullline = string.Format( "{0} {1} {2}", FormatTime( time - StartTime ), level, line );
         if ( fullline.Length > 300 ) {
            // Trim long message from console to avoid 65000 vertices error, but write the full log.
            OverrideAppendToLogFile_AddToQueue( fullline, null );
            line = line.Split( new char[]{ '\r', '\n' }, 2 )[0].Trim();
            if ( line.Length > 250 ) line = line.Substring( 0, 250 );
            if ( level.StartsWith( "<color=" ) ) line += "</color>";
            WriteConsole( time, level, EscLine( line ) + $"... ({fullline.Length} chars)<b></b>" );
            return;
         }
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
         if ( Config.Log_Game || Config.Log_Modnix )
            lock ( ConsoleBuffer ) ConsoleBuffer.Add( fullline );
         else // No log = no buffer patch
            GameConsoleWindow.Create().WriteLine( fullline );
      }

      private static string FormatTime ( TimeSpan logTime ) {
         return logTime.TotalSeconds.ToString( "F2" );
      }

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
         lock ( WriteBuffer ) {
            if ( LogFileQueue == null ) {
               LogFileQueue = ____logFileQueue;
               if ( LogFileQueue == null ) return true;
            }
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

      private static Regex RegexStyle = new Regex( "</?\\w+(=#?\\w+| )?>", RegexOptions.Compiled );
      private static Regex RegexModnixLine = new Regex( "</color><b></b>$", RegexOptions.Compiled );

      private static string FormatConsoleLog ( KeyValuePair<DateTime,string> entry ) {
         var line = entry.Value;
         if ( string.IsNullOrWhiteSpace( line ) ) return null;
         if ( line.EndsWith( " chars)<b></b>" ) ) return null; // Trimmed log
         if ( HasApi && RegexModnixLine.IsMatch( line ) && ! Config.Write_Modnix_To_Console_Logfile ) return null;
         line = EscLine( RegexStyle.Replace( line, "" ) ); // Prevent log writer thread from throwing error on string.Format.
         return entry.Key.ToString( "u" ).Substring( 11, 8 ) + " | " + line;
      }
      #endregion

      private static string EscLine ( string line ) => line.Replace( "{", "{{" ).Replace( "}", "}}" );
   }
}