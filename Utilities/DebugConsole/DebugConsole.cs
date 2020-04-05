﻿using Base.Core;
using Base.Utils.GameConsole;
using Harmony;
using Newtonsoft.Json;
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
      public int  Config_Version = 20200405;
      public bool Mod_Count_In_Version = true;
      public bool Log_Game_Error = true;
      public bool Log_Game_Info = true;
      public bool Log_Modnix_Error = true;
      public bool Log_Modnix_Info = true;
      public bool Log_Modnix_Verbose = false;
      public bool Scan_Mods_For_Command = true;
      public bool Write_Modnix_To_Console_Logfile = false;
   }

   public class Mod : ZyMod {
      internal static ModConfig Config;

      public static void Init () => new Mod().SplashMod();

      public void SplashMod ( Func< string, object, object > api = null ) {
         GameConsoleWindow.DisableConsoleAccess = false; // Enable console first no matter what happens
         SetApi( api, out Config );
         CheckConfig();
         Verbo( "Console Enabled" );
         if ( Config.Log_Game_Error || Config.Log_Game_Info ) {
            Application.logMessageReceived += UnityToConsole; // Non-main thread will cause inconsistent formats.
            TryPatch( typeof( TimingScheduler ), "Update", postfix: nameof( BufferToConsole ) );
         }
         if ( Config.Scan_Mods_For_Command ) {
            TryPatch( typeof( ConsoleCommandAttribute ), "GetCommands", prefix: nameof( ScanCommands ) );
            TryPatch( typeof( ConsoleCommandAttribute ), "GetInfo", prefix: nameof( ScanCommands ) );
            TryPatch( typeof( ConsoleCommandAttribute ), "HasCommand", prefix: nameof( ScanCommands ) );
            TryPatch( typeof( GameConsoleWindow ), "ToggleVisibility", postfix: nameof( ScanCommands ) );
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
               if ( ! Config.Write_Modnix_To_Console_Logfile )
                  TryPatch( typeof( GameConsoleWindow ), "AppendToLogFile", nameof( SkipAppendLogFile_IfModnix ) );
            } else
               Info( "Modnix assembly not found, log not forwarded." );
         }
      }

      public static void MainMod () {
         if ( Config.Scan_Mods_For_Command )
            ScanCommands();
      }

      private static void CheckConfig () {
         if ( Config.Config_Version < 20200405 )
            ModnixApi?.Invoke( "config_save", Config );
      }

      private static HashSet< Assembly > ScannedMods;
      private static FieldInfo CmdMethod;
      private static FieldInfo CmdVarArg;
      private static SortedList<string, ConsoleCommandAttribute> Commands;

      private static void InitScanner () {
         if ( ScannedMods != null ) return;
         ScannedMods = new HashSet< Assembly >();
         CmdMethod = typeof( ConsoleCommandAttribute ).GetField( "_methodInfo", BindingFlags.NonPublic | BindingFlags.Instance );
         CmdVarArg = typeof( ConsoleCommandAttribute ).GetField( "_variableArguments", BindingFlags.NonPublic | BindingFlags.Instance );
         Commands =  typeof( ConsoleCommandAttribute ).GetField( "CommandToInfo", BindingFlags.NonPublic | BindingFlags.Static ).GetValue( null ) as SortedList<string, ConsoleCommandAttribute> ;
      }

      private static void ScanCommands () { try {
         InitScanner();
         if ( Commands == null ) return;
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
                  tag.Command = tag.Command ?? func.Name;
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

      [ ConsoleCommand( Command = "modnix", Description = "Call Modnix or compatible api." ) ]
      public static void ApiCommand ( IConsole console, string[] param ) { try {
         if ( param == null || param.Length == 0 ) throw new ApplicationException( "Api action required." );
         object arg = null;
         if ( param.Length > 2 ) {
            arg = new string[ param.Length - 1 ];
            Array.Copy( param, 1, arg as string[], 0, param.Length - 1 );
         } else if ( param.Length == 2 )
            arg = param[1];
         var result = ModnixApi( param[0], arg );
         console.WriteLine( EscLine( result is string txt ? txt : JsonConvert.SerializeObject( result, Formatting.None ) ) );
      } catch ( Exception ex ) { Error( ex ); } }

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
                  prefix = "<color=red>Error";
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
         lock ( Buffer ) Buffer.Add( txt );
      } catch ( Exception ex ) { Error( ex ); } }

      [ HarmonyPriority( Priority.VeryLow ) ]
      private static bool SkipAppendLogFile_IfModnix ( string line ) {
         return ! string.IsNullOrWhiteSpace( line ) && line.IndexOf( '┊' ) < 0;
      }

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
            console.WriteLine( EscLine( line ) );
      } catch ( Exception ex ) { Error( ex ); } }

      private static string EscLine ( string line ) => line.Replace( "{", "{{" ).Replace( "}", "}}" );
   }
}