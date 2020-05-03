using Base.Core;
using Base.Levels;
using Base.Utils.GameConsole;
using Newtonsoft.Json;
using PhoenixPoint.Common.Levels;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Home.View;
using PhoenixPoint.Home.View.ViewStates;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.View;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Sheepy.PhoenixPt.DebugConsole {
   internal class ExtModule : ZyMod {

      #region Scanner
      private static HashSet< Assembly > ScannedMods;
      private static FieldInfo CmdMethod;
      private static FieldInfo CmdVarArg;
      private static SortedList<string, ConsoleCommandAttribute> Commands;

      internal void InitMod () {
         if ( InitScanner() ) {
            TryPatch( typeof( UIStateMainMenu ), "EnterState", prefix: nameof( ScanCommands ) );
            TryPatch( typeof( GameConsoleWindow ), "ToggleVisibility", postfix: nameof( ScanCommands ) );
            // Scan before lists are accessed. Messages are buffered and will appear after the commands.
            TryPatch( typeof( ConsoleCommandAttribute ), "GetCommands", prefix: nameof( ScanCommands ) );
            TryPatch( typeof( ConsoleCommandAttribute ), "GetInfo", prefix: nameof( ScanCommands ) );
            TryPatch( typeof( ConsoleCommandAttribute ), "HasCommand", prefix: nameof( ScanCommands ) );
         }
      }

      internal static void RegisterApi () {
         Api( "api_add zy.ui.dump", (Func<object,bool>) GuiTreeApi );
      }

      internal static bool InitScanner () {
         CmdMethod = typeof( ConsoleCommandAttribute ).GetField( "_methodInfo", BindingFlags.NonPublic | BindingFlags.Instance );
         CmdVarArg = typeof( ConsoleCommandAttribute ).GetField( "_variableArguments", BindingFlags.NonPublic | BindingFlags.Instance );
         Commands =  typeof( ConsoleCommandAttribute ).GetField( "CommandToInfo", BindingFlags.NonPublic | BindingFlags.Static ).GetValue( null ) as SortedList<string, ConsoleCommandAttribute> ;
         if ( CmdMethod == null || CmdVarArg == null || Commands == null ) return false;
         ScannedMods = new HashSet< Assembly >();
         return true;
      }

      internal static void ScanCommands () { try {
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
            foreach ( var type in asm.GetTypes() ) {
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
            }
            ScannedMods.Add( asm );
         }
      } catch ( Exception ex ) { Error( ex ); } }
      #endregion

      #region Console commands
      [ConsoleCommand( Command = "log_level", Description = "[level] - Get or set console log level: g(et), e(rror), i(nfo), v(erbose), or n(one)" ) ]
      public static void ConsoleCommandDebugLevel ( IConsole console, string level ) { try {
         if ( level != null && level.Length >= 1 ) switch ( Char.ToLower( level[0] ) ) {
            case 'g' :
               List<string> levels = new List<string>();
               var config = Mod.Config;
               if ( config.Log_Game_Error ) levels.Add( "Game Error" );
               if ( config.Log_Game_Info ) levels.Add( "Game Info" );
               if ( config.Log_Modnix_Error ) levels.Add( "Modnix Error" );
               if ( config.Log_Modnix_Info ) levels.Add( "Modnix Info" );
               if ( config.Log_Modnix_Verbose ) levels.Add( "Modnix Verbose" );
               if ( level.Length == 0 ) levels.Add( "(None)" );
               console.WriteLine( "Console log level: " + String.Join( ", ", levels ) );
               return;

            case 'e' :
               var conf = SetAllLogging( false );
               conf.Log_Game_Error = conf.Log_Modnix_Error = true;
               console.WriteLine( "Console log level set to Error." );
               return;

            case 'i' :
               SetAllLogging( true ).Log_Modnix_Verbose = false;
               console.WriteLine( "Console log level set to Information." );
               return;

            case 'v' :
               SetAllLogging( true );
               console.WriteLine( "Console log level set to Verbose." );
               //ConsoleCommandModnixDebugLevel( console, "v" );
               return;

            case 'n' :
               SetAllLogging( false );
               console.WriteLine( "Console log level set to None." );
               return;
         }
         WriteError( $"Unknown level '{level}'. Level must be g, e, i, or n." );
      } catch ( Exception ex ) {
         Error( ex );
      } finally {
         _ = Mod.Config; // Sync config
      } }

      private static ModConfig SetAllLogging ( bool enable ) {
         var config = Mod.Config;
         config.Log_Game_Error = config.Log_Modnix_Error = config.Log_Game_Info = config.Log_Modnix_Info = config.Log_Modnix_Verbose = enable;
         return config;
      }

      [ ConsoleCommand( Command = "log_level_modnix", Description = "[level] - Get or set modnix log level: g(et), e(rror), i(nfo), v(erbose), or n(one)" ) ]
      public static void ConsoleCommandModnixDebugLevel ( IConsole console, string level ) { try {
         if ( ! HasApi ) { WriteError( "Modnix API not found." ); return; }
         // This should be called rarely, so the reflections are not cached.
         var asm = Api( "assembly", "modnix" ) as Assembly;
         var loggerField = asm?.GetType( "Sheepy.Modnix.ModLoader" )?.GetField( "Log", BindingFlags.NonPublic | BindingFlags.Static );
         var levelField = asm?.GetType( "Sheepy.Logging.Logger" )?.GetProperty( "Level" );
         var logger = loggerField?.GetValue( null );
         string levelText = logger != null ? levelField.GetValue( logger )?.ToString() : null;
         if ( levelText == null ) { WriteError( "Modnix logger not found or not recognised." ); return; }
         
         if ( level != null && level.Length >= 1 ) switch ( Char.ToLower( level[0] ) ) {
            case 'g' :
               console.WriteLine( "Modnix log level: " + levelText );
               return;

            case 'e' :
               levelField.SetValue( logger, SourceLevels.Warning );
               console.WriteLine( "Modnix log level set to Error." );
               return;

            case 'i' :
               levelField.SetValue( logger, SourceLevels.Information );
               console.WriteLine( "Modnix log level set to Information." );
               return;

            case 'v' :
               levelField.SetValue( logger, SourceLevels.Verbose );
               console.WriteLine( "Modnix log level set to Verbose." );
               return;

            case 'n' :
               levelField.SetValue( logger, SourceLevels.Off );
               console.WriteLine( "Modnix log level set to None." );
               return;
         }
         WriteError( $"Unknown level '{level}'. Level must be g, e, i, v, or n." );
      } catch ( Exception ex ) { Error( ex ); } }

      [ ConsoleCommand( Command = "modnix", Description = "[spec] [param] - Call Modnix or compatible api." ) ]
      public static void ConsoleCommandModnix ( IConsole console, string[] param ) { try {
         if ( ! HasApi ) {
            WriteError( "Modnix API not found." );
            return;
         }
         if ( param == null || param.Length == 0 ) throw new ApplicationException( "Api action required." );
         Task.Run( () => {
            object arg = null;
            if ( param.Length > 2 ) {
               arg = new string[ param.Length - 1 ];
               Array.Copy( param, 1, arg as string[], 0, param.Length - 1 );
            } else if ( param.Length == 2 )
               arg = param[1];
            WriteResult( Api( param[0], arg ) );
         } );
      } catch ( Exception ex ) { Error( ex ); } }

      private static void WriteError ( string line ) => GameConsoleWindow.Create().WriteLine( $"<color=red>{line}</color>" );
      #endregion

      #region Gui Tree
      internal static bool GuiTreeApi ( object root ) => DumpGui( null, root ?? DefaultDumpSubject() );

      [ ConsoleCommand( Command = "dump_gui", Description = "[name] - Find a named component and dump its tree, or dump current view's tree." ) ]
      public static void ConsoleCommandGuiDump ( IConsole console, string[] names ) { try {
         if ( names?.Length > 0 ) {
            foreach ( var name in names )
               DumpGui( console, name );
         } else
            DumpGui( console, DefaultDumpSubject() );
      } catch ( Exception ex ) { Error( ex ); } }

      private static Component DefaultDumpSubject () {
         var level = GameUtl.CurrentLevel();
         if ( level == null || level.CurrentState != Level.State.Playing )
            return level.gameObject.transform;
         if ( level.GetComponent<MenuLevelController>() != null )
            return level.GetComponent<HomeScreenView>()?.HomeScreenModules;
         if ( level.GetComponent<GeoLevelController>() != null )
            return level.GetComponent<GeoscapeView>()?.GeoscapeModules;
         if ( level.GetComponent<TacticalLevelController>() != null )
            return level.GetComponent<TacticalView>()?.TacticalModules;
         return null;
      }

      internal static bool DumpGui ( IConsole output, object root ) {
         if ( root is string txt ) {
            root = GameObject.Find( txt );
            if ( root == null ) {
               Output( output, "GameObject '{0}' does not exist or is inactive", txt );
               return false;
            }
         }
         if ( root is Component c ) root = c.gameObject;
         if ( ! ( root is GameObject obj ) ) {
            Output( output, new ArgumentException( "Not a Unity GameObject: " + root?.GetType().FullName ?? "null" ) );
            return false;
         }
         DumpComponents( output, "", new HashSet<object>(), obj );
         return true;
      }

      internal static void DumpComponents ( IConsole output, string prefix, HashSet<object> logged, GameObject e ) {
         if ( prefix.Length > 20 ) return;
         if ( logged.Contains( e ) ) return;
         logged.Add( e );
         var text = output != null && prefix.Length > 0 ? FindText( e ) : () => "";
         Output( output, "{0}- '{1}'{2} {3}{4}{5}{6} :{7}", prefix, e.name, ToTag( e.tag ), text, TypeName( e ),
            e.activeSelf ? "" : " (Inactive)", ToLayer( e.layer ), ToString( e.GetComponent<Transform>() ) );
         if ( output == null || prefix.Length == 0 )
            foreach ( var c in e.GetComponents<Component>() ) {
               var typeName = TypeName( c );
               if ( c is Transform cRect ) ;
               else if ( c is UnityEngine.UI.Text txt )
                  Output( output, "{0}...{1} {2} {3}", prefix, typeName, txt.color, txt.text );
               else if ( c is I2.Loc.Localize loc )
                  Output( output, "{0}...{1} {2}", prefix, typeName, loc.mTerm );
               else if ( c is UnityEngine.UI.LayoutGroup layout )
                  Output( output, "{0}...{1} Padding {2}", prefix, typeName, layout.padding );
               else if ( c is FovControllableBehavior fov )
                  Output( output, "{0}...{1} Invis {2} Billboard {3}-{4}", prefix, typeName, fov.ControllingDef.InvisibleOverFov, fov.ControllingDef.BillboardStartFov, fov.ControllingDef.BillboardFullFov );
               else
                  Output( output, "{0}...{1}", prefix, typeName );
            }
         for ( int i = 0 ; i < e.transform.childCount ; i++ )
            DumpComponents( output, prefix + "  ", logged, e.transform.GetChild( i ).gameObject );
      }

      private static Func<string> ToTag ( string tag ) => () => "Untagged".Equals( tag ) ? "" : $":{tag}";

      private static Func<string> FindText ( GameObject obj ) => () => {
         var text = obj.GetComponent< UnityEngine.UI.Text >()?.text;
         if ( text == null ) return "";
         return $"\"{text}\" ";
      };

      private static Func<string> ToLayer ( int layer ) => () => layer == 0 ? "" : $" Layer {layer}";

      private static Func<string> ToString ( Transform t ) { return () => {
         if ( t == null ) return "";
         var result = string.Format( "Pos {0} Scale {1} Rotate {2}", t.localPosition, t.localScale, t.localRotation );
         return " " + result.Replace( ".0,", "," ).Replace( ".0)", ")" )
            .Replace( "Pos (0, 0, 0)", "" )
            .Replace( "Scale (1, 1, 1)", "" )
            .Replace( "Rotate (0, 0, 0, 1)", "" )
            .Trim();
      }; }

      private static void Output ( IConsole output, object param, params object[] args ) {
         if ( output == null ) { Info( param, args ); return; }
         for ( int i = 0, len = args.Length ; i < len ; i++ )
            if ( args[i] is Func<string> eval ) args[i] = eval();
         Mod.WriteConsole( string.Format( param?.ToString(), args ) );
      }

      private static string TypeName ( object e ) => e?.GetType().FullName.Replace( "UnityEngine.", "UE." );

      private static void WriteResult ( object result ) { try {
         if ( result is string line ) ;
         else if ( result == null ) line = "null";
         else if ( result is Exception || result is StackTrace || result is MethodInfo || result is Task ) line = result.ToString();
         else line = JsonConvert.SerializeObject( result, Formatting.None );
         Mod.WriteConsole( line );
         if ( result is Task<object> task )
            task.ContinueWith( e => {
               if ( e.IsCanceled ) WriteResult( "Cancelled" );
               else if ( e.IsFaulted ) WriteResult( e.Exception );
               else WriteResult( e.Result );
            } );
      } catch ( Exception ex ) { Error( ex ); } }
      #endregion
   }
}