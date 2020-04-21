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
using System.Threading.Tasks;
using UnityEngine;

namespace Sheepy.PhoenixPt.DebugConsole {

   internal class ModConfig {
      public bool Mod_Count_In_Version = true;
      public bool Log_Game_Error = true;
      public bool Log_Game_Info = true;
      public bool Log_Modnix_Error = true;
      public bool Log_Modnix_Info = true;
      public bool Log_Modnix_Verbose = false;
      public bool Scan_Mods_For_Command = true;
      public bool Write_Modnix_To_Console_Logfile = false;
      public int  Config_Version = 20200405;

      internal void Update () {
         if ( Config_Version < 20200405 ) {
            Config_Version = 20200405;
            ZyMod.Api( "config_save", this );
         }
      }
   }

   public class Mod : ZyMod {
      internal static ModConfig Config;

      public static void Init () => new Mod().SplashMod();

      public void SplashMod ( Func< string, object, object > api = null ) {
         SetApi( api, out Config ).Update();
         GeneralPatch();
         ModnixPatch();
      }

      private void GeneralPatch () {
         GameConsoleWindow.DisableConsoleAccess = false; // Enable console first no matter what happens
         Verbo( "Console Enabled" );
         if ( Config.Log_Game_Error || Config.Log_Game_Info ) {
            Application.logMessageReceived += UnityToConsole; // Non-main thread will cause inconsistent formats.
            TryPatch( typeof( TimingScheduler ), "Update", postfix: nameof( BufferToConsole ) );
         }
         if ( Config.Scan_Mods_For_Command && InitScanner() ) {
            TryPatch( typeof( UIStateMainMenu ), "EnterState", prefix: nameof( ScanCommands ) );
            TryPatch( typeof( GameConsoleWindow ), "ToggleVisibility", postfix: nameof( ScanCommands ) );
            // Scan before lists are accessed. Messages are buffered and will appear after the commands.
            TryPatch( typeof( ConsoleCommandAttribute ), "GetCommands", prefix: nameof( ScanCommands ) );
            TryPatch( typeof( ConsoleCommandAttribute ), "GetInfo", prefix: nameof( ScanCommands ) );
            TryPatch( typeof( ConsoleCommandAttribute ), "HasCommand", prefix: nameof( ScanCommands ) );
         }
      }

      private void ModnixPatch () {
         if ( ! HasApi ) return;
         Api( "api_add zy.ui.dump", (Func<object,object>) Extensions.GuiTree );
         if ( Config.Mod_Count_In_Version )
            TryPatch( typeof( UIStateMainMenu ), "EnterState", postfix: nameof( AfterMainMenu_AddModCount ) );
         if ( Config.Log_Modnix_Error || Config.Log_Modnix_Info || Config.Log_Modnix_Verbose ) {
            var loader = Api( "assembly", "modnix" ) as Assembly
               ?? AppDomain.CurrentDomain.GetAssemblies().First( e => e.FullName.StartsWith( "ModnixLoader", StringComparison.Ordinal ) );
            var type = loader?.GetType( "Sheepy.Logging.FileLogger" );
            if ( type == null ) {
               Info( "Modnix assembly not found, log not forwarded." );
               return;
            }
            ModnixLogEntryLevel = loader.GetType( "Sheepy.Logging.LogEntry" ).GetField( "Level" );
            if ( ModnixLogEntryLevel == null ) Warn( "Modnix log level not found. All log forwarded." );
            TryPatch( type, "ProcessEntry", postfix: nameof( ModnixToConsole ) );
            TryPatch( typeof( GameConsoleWindow ), "AppendToLogFile", nameof( BeforeAppendToLogFile_ProcessModnixLine ) );
         }
      }

      private static HashSet< Assembly > ScannedMods;
      private static FieldInfo CmdMethod;
      private static FieldInfo CmdVarArg;
      private static SortedList<string, ConsoleCommandAttribute> Commands;

      private static bool InitScanner () {
         CmdMethod = typeof( ConsoleCommandAttribute ).GetField( "_methodInfo", BindingFlags.NonPublic | BindingFlags.Instance );
         CmdVarArg = typeof( ConsoleCommandAttribute ).GetField( "_variableArguments", BindingFlags.NonPublic | BindingFlags.Instance );
         Commands =  typeof( ConsoleCommandAttribute ).GetField( "CommandToInfo", BindingFlags.NonPublic | BindingFlags.Static ).GetValue( null ) as SortedList<string, ConsoleCommandAttribute> ;
         if ( CmdMethod == null || CmdVarArg == null || Commands == null ) return false;
         ScannedMods = new HashSet< Assembly >();
         return true;
      }

      private static void ScanCommands () { try {
         Assembly[] asmAry = AppDomain.CurrentDomain.GetAssemblies();
         if ( asmAry.Length == ScannedMods.Count ) return;
         foreach ( var asm in asmAry ) {
            if ( ScannedMods.Contains( asm ) ) continue;
            if ( asm.FullName.StartsWith( "UnityEngine.", StringComparison.Ordinal ) ||
                 asm.FullName.StartsWith( "Unity.", StringComparison.Ordinal ) ||
                 asm.FullName.StartsWith( "Assembly-CSharp,", StringComparison.Ordinal ) ||
                 asm.FullName.StartsWith( "System.", StringComparison.Ordinal ) ) {
               ScannedMods.Add( asm );
               continue;
            }
            Verbo( "Scanning {0} for console commands.", asm.FullName );
            foreach ( var type in asm.GetTypes() )
               foreach ( var func in type.GetMethods( BindingFlags.Static | BindingFlags.Public ) ) {
                  var tag = func.GetCustomAttribute( typeof(ConsoleCommandAttribute) ) as ConsoleCommandAttribute;
                  if ( tag == null ) continue;
                  if ( tag.Command == null ) tag.Command = func.Name;
                  if ( Commands.ContainsKey( tag.Command ) ) {
                     Info( "Command exists, cannot register {0} of {1} in {2}.", tag.Command, type.FullName, asm.FullName );
                     continue;
                  }
                  CmdMethod.SetValue( tag, func );
                  var param = func.GetParameters();
                  if ( param.Length > 0 && param[ param.Length - 1 ].ParameterType.FullName.Equals( "System.String[]" ) )
                     CmdVarArg.SetValue( tag, true );
                  Commands.Add( tag.Command, tag );
                  Info( "Command registered: {0} of {1} in {2}.", tag.Command, type.FullName, asm.FullName );
               }
            ScannedMods.Add( asm );
         }
      } catch ( Exception ex ) { Error( ex ); } }

      private static void AfterMainMenu_AddModCount ( UIStateMainMenu __instance ) { try {
         var revision = typeof( UIStateMainMenu ).GetProperty( "_buildRevisionModule", BindingFlags.NonPublic | BindingFlags.Instance )?.GetValue( __instance ) as UIModuleBuildRevision;
         var list = Api( "mod_list", null ) as IEnumerable<string>;
         if ( revision == null || list == null ) return;
         Info( "Adding to main menu version string" );
         var loader_ver = new Regex( "(?:\\.0){1,2}$" ).Replace( Api( "version", "loader" ).ToString(), "" );
         revision.BuildRevisionNumber.text += $", Modnix {loader_ver}, {list.Count()} mods.";
      } catch ( Exception ex ) { Error( ex ); } }

      private readonly static List<string> Buffer = new List<string>();

      internal static void ConsoleWrite ( string line ) {
         if ( string.IsNullOrWhiteSpace( line ) ) return;
         lock ( Buffer ) Buffer.Add( line );
      }

      private static FieldInfo ModnixLogEntryLevel;

      private static void ModnixToConsole ( object entry, string txt ) { try {
         txt = txt.Trim();
         if ( txt.Length == 0 || txt.Length > 1000 || txt.Contains( ".ModnixToConsole" ) || txt.Contains( "BeforeAppendToLogFile_ProcessModnixLine" ) ) return;
         if ( ModnixLogEntryLevel != null ) {
            var level = (TraceEventType) ModnixLogEntryLevel.GetValue( entry );
            string prefix;
            lock ( _Lock ) switch ( level ) {
               case TraceEventType.Critical: case TraceEventType.Error:
                  if ( ! Config.Log_Modnix_Error ) return;
                  prefix = "<color=fuchsia>Error";
                  break;
                case TraceEventType.Warning:
                  if ( ! Config.Log_Modnix_Error ) return;
                  prefix = "<color=orange>Warning";
                  break;
               case TraceEventType.Information :
                  if ( ! Config.Log_Modnix_Info ) return;
                  prefix = "<color=lime>Log [INFO]";
                  break;
               default :
                  if ( ! Config.Log_Modnix_Verbose ) return;
                  prefix = "<color=aqua>Log [VEBO]";
                  break;
            }
            txt = $"{prefix} {Time.frameCount} ({Time.time.ToString("0.000")}) {txt}</color>";
         }
         ConsoleWrite( txt );
      } catch ( Exception ex ) { Error( ex ); } }

      private static Regex RegexModnixLine = new Regex( "\\] \\d+ \\(\\d+\\.\\d{3}\\) ", RegexOptions.Compiled );
      private static Regex RegexModnixColour = new Regex( "^<color=#?\\w+>", RegexOptions.Compiled );

      [ HarmonyPriority( Priority.VeryLow ) ]
      private static bool BeforeAppendToLogFile_ProcessModnixLine ( ref string line ) { try {
         if ( string.IsNullOrWhiteSpace( line ) ) return true;
         line = EscLine( line ); // Prevent log writer thread from throwing error on string.format.
         if ( ! RegexModnixLine.IsMatch( line ) ) return true;
         if ( ! Config.Write_Modnix_To_Console_Logfile ) return false;
         if ( line.StartsWith( "<color=", StringComparison.Ordinal ) ) {
            line = RegexModnixColour.Replace( line, "", 1 );
            if ( line.EndsWith( "</color>", StringComparison.Ordinal ) )
               line = line.Substring( 0, line.Length - 8 );
         }
         return true;
      } catch ( Exception ex ) { return Error( ex ); } }

      private static void UnityToConsole ( string condition, string stackTrace, LogType type ) { try {
         switch ( type ) {
            case LogType.Exception: case LogType.Error: case LogType.Warning:
               if ( ! Config.Log_Game_Error ) return; break;
            default:
               if ( ! Config.Log_Game_Info ) return; break;
         }
         // Skip long message which can cause the 65000 vertices error.
         if ( condition.Length > 1000 || stackTrace.Length > 1000 ||
              condition.Contains( "Called from a secondary Thread" ) ||
              stackTrace?.Contains( "UnityTools.LogFormatter" ) == true )
           return;
         var line = $"{type} {condition}   {stackTrace}".Trim();
         if ( line.Length == 0 ) return;
         ConsoleWrite( line );
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
            console.WriteLine( EscLine( line ) );
      } catch ( Exception ex ) { Error( ex ); } }

      private static string EscLine ( string line ) => line.Replace( "{", "{{" ).Replace( "}", "}}" );
   }
}