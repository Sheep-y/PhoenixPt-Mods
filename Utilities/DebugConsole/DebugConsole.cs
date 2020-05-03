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

      public void SplashMod ( Func< string, object, object > api = null ) {
         SetApi( api, out Config ).Update();
         GeneralPatch();
         ModnixPatch();
      }

      private void GeneralPatch () {
         GameConsoleWindow.DisableConsoleAccess = false; // Enable console first no matter what happens
         Verbo( "Console Enabled" );
         if ( Config.Log_Game_Error || Config.Log_Game_Info ) try {
            var a = TryPatch( typeof( LogFormatter ), "LogFormat", prefix: nameof( UnityToConsole ) );
            var b = TryPatch( typeof( LogFormatter ), "LogException", prefix: nameof( ExToConsole ) );
            if ( a != null || b != null ) {
               TryPatch( typeof( TimingScheduler ), "Update", postfix: nameof( BufferToConsole ) );
               foreach ( var t in typeof( TimeComponent ).GetNestedTypes( BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static ) )
                  Info ( t );
               //TryPatch( typeof( TimeComponent ), "Awake", transpiler: nameof( CaptureCoroutineException ) );
            }
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

      internal static void AddToConsoleBuffer ( string line ) {
         if ( string.IsNullOrWhiteSpace( line ) ) return;
         lock ( ConsoleBuffer ) ConsoleBuffer.Add( line );
      }

      private static FieldInfo ModnixLogEntryLevel;

      private static void ModnixToConsole ( object entry, string txt ) { try {
         txt = txt.Trim();
         if ( txt.Length == 0 || txt.Length > 1000 || txt.Contains( ".ModnixToConsole" ) || txt.Contains( "BeforeAppendToLogFile_ProcessModnixLine" ) ) return;
         var prefix = "<color=lime>[MODX] ";
         if ( ModnixLogEntryLevel != null ) {
            var level = (TraceEventType) ModnixLogEntryLevel.GetValue( entry );
            lock ( _SLock ) ; // Sync config
            switch ( level ) {
               case TraceEventType.Critical: case TraceEventType.Error:
                  if ( ! Config.Log_Modnix_Error ) return;
                  prefix = "<color=fuchsia>[ERROR] ";
                  break;
                case TraceEventType.Warning:
                  if ( ! Config.Log_Modnix_Error ) return;
                  prefix = "<color=orange>[WARN] ";
                  break;
               case TraceEventType.Information :
                  if ( ! Config.Log_Modnix_Info ) return;
                  prefix = "<color=lime>[INFO] ";
                  break;
               default :
                  if ( ! Config.Log_Modnix_Verbose ) return;
                  prefix = "<color=aqua>[VEBO] ";
                  break;
            }
         }
         txt = $"{prefix} ({Time.time.ToString("0.000")}) {txt}</color>";
         AddToConsoleBuffer( txt );
      } catch ( Exception ex ) { Error( ex ); } }

      private static bool UnityToConsole ( LogType logType, UnityEngine.Object context, string format, params object[] args ) { try {
         bool runOriginal = ! Config.Optimise_Log_File;
         switch ( logType ) {
            case LogType.Exception: case LogType.Error: case LogType.Warning:
               if ( ! Config.Log_Game_Error ) return runOriginal; break;
            default:
               if ( ! Config.Log_Game_Info ) return runOriginal; break;
         }
         string line = string.Format( format, args );
         if ( string.IsNullOrWhiteSpace( line ) ||
              line.StartsWith( "[CONSOLE] " ) ||
              line.Contains( "Called from a secondary Thread" ) ||
              line.Contains( "UnityTools.LogFormatter" ) ||
              line.Contains( "DebugConsole.Mod.UnityToConsole" ) )
            return runOriginal;
         if ( format.Length > 1000 ) {
            // Hide long message from console to avoid 65000 vertices error.
            OverrideAppendToLogFile_AddToQueue( line, null );
         } else
            AddToConsoleBuffer( line );
         return runOriginal;
      } catch ( Exception ex ) { return Error( ex ); } }

      private static bool ExToConsole ( Exception exception, UnityEngine.Object context )
         => UnityToConsole( LogType.Exception, context, exception.ToString() );

      private static IEnumerable<CodeInstruction> CaptureCoroutineException ( IEnumerable<CodeInstruction> instr ) {
         foreach ( var code in instr )
            Info( code );
         return instr;
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

      #region Write console to console log
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

      private static Regex RegexModnixColour = new Regex( "^<color=#?\\w+>", RegexOptions.Compiled );
      private static Regex RegexModnixLine = new Regex( "^(\\[\\w+\\] )?\\d+ \\(\\d+\\.\\d{3}\\) ", RegexOptions.Compiled );

      private static string FormatConsoleLog ( KeyValuePair<DateTime,string> entry ) {
         var line = entry.Value;
         if ( string.IsNullOrWhiteSpace( line ) ) return null;
         line = EscLine( line ); // Prevent log writer thread from throwing error on string.Format.
         if ( line.StartsWith( "<color=", StringComparison.Ordinal ) ) {
            line = RegexModnixColour.Replace( line, "", 1 );
            if ( line.EndsWith( "</color>", StringComparison.Ordinal ) )
               line = line.Substring( 0, line.Length - 8 );
         }
         if ( HasApi && RegexModnixLine.IsMatch( line ) && ! Config.Write_Modnix_To_Console_Logfile ) return null;
         return entry.Key.ToString( "u" ).Substring( 11, 8 ) + " | " + line;
      }
      #endregion

      private static string EscLine ( string line ) => line.Replace( "{", "{{" ).Replace( "}", "}}" );
   }
}