using Base.Core;
using Base.UI;
using Base.Utils.GameConsole;
using PhoenixPoint.Home.View.ViewModules;
using PhoenixPoint.Home.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace Sheepy.PhoenixPt.DebugConsole {

   public class ModConfig {
      public int  Config_Version = 20200324;
      public bool Mod_Count_In_Version = true;
      public bool Log_Game_Error = true;
      public bool Log_Game_Info = true;
      public bool Log_Modnix_Error = true;
      public bool Log_Modnix_Info = true;
      public bool Log_Modnix_Verbose = false;
   }

   public class Mod : ZyMod {
      internal static ModConfig Config;

      public static void Init () => new Mod().SplashMod();

      public void SplashMod ( Func< string, object, object > api = null ) {
         GameConsoleWindow.DisableConsoleAccess = false; // Enable console first no matter what happens
         SetApi( api, out Config );
         if ( Config.Log_Game_Error || Config.Log_Game_Info ) {
            Application.logMessageReceived += UnityToConsole;
            TryPatch( typeof( TimingScheduler ), "Update", postfix: nameof( BufferToConsole ) );
         }
         if ( api == null ) return;

         // Modnix only features
         if ( Config.Mod_Count_In_Version )
            TryPatch( typeof( UIStateMainMenu ), "EnterState", postfix: nameof( AfterMainMenu_AddModCount ) );
         if ( Config.Log_Modnix_Error || Config.Log_Modnix_Info || Config.Log_Modnix_Verbose ) {
            var loader = api( "assembly", "modnix" ) as Assembly
               ?? AppDomain.CurrentDomain.GetAssemblies().First( e => e.FullName.StartsWith( "ModnixLoader", StringComparison.Ordinal ) );
            var type = loader?.GetType( "Sheepy.Logging.FileLogger" );
            if ( type != null ) {
               ModnixLogEntryLevel = loader.GetType( "Sheepy.Logging.LogEntry" ).GetField( "Level" );
               if ( ModnixLogEntryLevel == null ) Warn( "Modnix log level not found. All log forwarded." );
               TryPatch( type, "ProcessEntry", postfix: nameof( ModnixToConsole ) );
            } else
               Info( "Modnix assembly not found, log not forwarded." );
         }
      }

      private static void AfterMainMenu_AddModCount ( UIStateMainMenu __instance ) { try {
         var revision = typeof( UIStateMainMenu ).GetProperty( "_buildRevisionModule", BindingFlags.NonPublic | BindingFlags.Instance )?.GetValue( __instance ) as UIModuleBuildRevision;
         var list = ModnixApi( "mod_list", null ) as IEnumerable<string>;
         if ( revision == null || list == null ) return;
         Info( "Adding to main menu version string" );
         var loader_ver = new Regex( "(?:\\.0){1,2}$" ).Replace( ModnixApi( "version", "loader" ).ToString(), "" );
         revision.BuildRevisionNumber.text += $", Modnix {loader_ver}, {list.Count()} mods.";
      } catch ( Exception ex ) { Error( ex ); } }

      private readonly static List<string> Buffer = new List<string>();

      private static FieldInfo ModnixLogEntryLevel;

      private static void ModnixToConsole ( object entry, string txt ) { try {
         txt = txt.Trim();
         if ( txt.Length == 0 || txt.Length > 1000 || txt.Contains( ".ModnixToConsole" ) ) return;
         if ( ModnixLogEntryLevel != null ) {
            var level = (TraceEventType) ModnixLogEntryLevel.GetValue( entry );
            string prefix;
            lock ( _Lock ) switch ( level ) {
               case TraceEventType.Critical: case TraceEventType.Error:
                  if ( ! Config.Log_Modnix_Error ) return;
                  prefix = "Error";
                  break;
                case TraceEventType.Warning:
                  if ( ! Config.Log_Modnix_Error ) return;
                  prefix = "Warning";
                  break;
               case TraceEventType.Information :
                  if ( ! Config.Log_Modnix_Info ) return;
                  prefix = "Log [INFO]";
                  break;
               default :
                  if ( ! Config.Log_Modnix_Verbose ) return;
                  prefix = "Log [VEBO]";
                  break;
            }
            txt = $"{prefix} {Time.frameCount} ({Time.time.ToString("0.000")}) {txt}";
         }
         lock ( Buffer ) Buffer.Add( txt );
      } catch ( Exception ex ) { Error( ex ); } }

      private static void UnityToConsole ( string condition, string stackTrace, LogType type ) { try {
         switch ( type ) {
            case LogType.Exception: case LogType.Error: case LogType.Warning:
               if ( ! Config.Log_Game_Error ) return; break;
            default:
               if ( ! Config.Log_Game_Info ) return; break;
         }
         // Long message can cause the 
         if ( condition.Length > 1000 || stackTrace.Length > 1000 ||
              condition.Contains( "Called from a secondary Thread" ) ||
              stackTrace?.Contains( "UnityTools.LogFormatter" ) == true ) return;
         var line = $"{type} {condition}   {stackTrace}".Trim();
         if ( line.Length == 0 ) return;
         lock ( Buffer ) Buffer.Add( line );
      } catch ( Exception ex ) { Error( ex ); } }

      private static void BufferToConsole () { try {
         string[] lines;
         lock ( Buffer ) {
            if ( Buffer.Count == 0 ) return;
            lines = Buffer.ToArray();
            Buffer.Clear();
         }
         var console = GameConsoleWindow.Create();
         foreach ( var line in lines )
            console.WriteLine( line );
      } catch ( Exception ex ) { Error( ex ); } }
   }
}