using Base;
using Base.Core;
using Base.Defs;
using Base.Utils.GameConsole;
using I2.Loc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Sheepy.PhoenixPt.DumpData {

   internal class ModConfig {
      public bool   Auto_Dump = true;
      public bool   Multithread = true;
      public bool   GZip = true;
      public ushort Depth = 50;
      public bool   Skip_Dumped_Objects = true;
      public bool   Skip_Dumped_Defs = true;
      public bool   Skip_Empty_Objects = true;
      public bool   Skip_Empty_Lists = true;
      public bool   Skip_Zeros = true;
      public string Dump_Path = "";
      public bool   Dump_Command_Csv = true;
      public bool   Dump_Guid_Csv = true;
      public bool   Dump_Lang_Csv = true;
      public string[] Dump_Defs = new string[]{
         "VehicleItemDef",
         "TacticalItemDef",
         "TacticalActorDef",
         "TacUnitClassDef",
         "TacMissionTypeDef",
         "SpecializationDef",
         "SharedGameTagsDataDef",
         "ResearchDef",
         "PhoenixFacilityDef",
         "HumanCustomizationDef",
         "GroundVehicleItemDef",
         "GeoscapeEventDef",
         "GeoSiteSceneDef",
         "GeoMistGeneratorDef",
         "GeoHavenZoneDef",
         "GeoFactionDef",
         "GeoAlienBaseDef",
         "GeoActorDef",
         "GameTagDef",
         "EntitlementDef",
         "DamageKeywordDef",
         "ComponentSetDef",
         "BodyPartAspectDef",
         "AIActionDef",
         "AchievementDef",
         "AbilityTrackDef",
         "AbilityDef",
      };
      public string[] Dump_Others = new string[] {
         "GameSettings", "SharedData.GetSharedDataFromGame().AISettingsDef",
         "GameSettings", "SharedData.GetSharedDataFromGame().ContributionSettings",
         "GameSettings", "SharedData.GetSharedDataFromGame().DifficultyLevels",
         "GameSettings", "SharedData.GetSharedDataFromGame().DiplomacySettings",
         "GameSettings", "SharedData.GetSharedDataFromGame().DynamicDifficultySettings",
      };
      public uint   Config_Version = 20210129;

      internal void Upgrade () {
         if ( Config_Version >= 20201128 ) return;
         //if ( Config_Version < 20200630 ) ; // Do nothing; Added Auto_Dump field
         //if ( Config_Version < 20201128 ) ; // Never released and merged with 20210129. Added EntitlementDef.
         if ( Config_Version < 20210129 && Dump_Defs?.Length > 0 ) {
            AddDef( "AIActionDef" );
            AddDef( "EntitlementDef" );
            AddDef( "HumanCustomizationDef" );
            AddDef( "SharedGameTagsDataDef" );
         }
         Config_Version = 20210129;
         ZyMod.Api( "config save", this ); // Add Auto_Dump
      }

      private void AddDef ( string def ) {
         if ( Dump_Defs.Contains( def ) ) return;
         var list = new List< string >( Dump_Defs );
         list.Add( def );
         Dump_Defs = list.ToArray();
      }
   }

   public class Mod : ZyMod {
      internal static ModConfig Config;

      public static void Init () => MainMod();

      internal static string DumpDir;
      internal static string GameVersion;
      private  static bool DidAutoDump;

      public static void MainMod ( Func< string, object, object > api = null ) {
         GameMod( api );
         GeoscapeOnShow();
      }

      public static void GameMod ( Func< string, object, object > api ) {
         SetApi( api, out Config );
         Config.Upgrade();
         if ( string.IsNullOrWhiteSpace( Config.Dump_Path ) ) {
            DumpDir = Api( "path" )?.ToString();
            DumpDir = DumpDir == null ? "." : Path.GetDirectoryName( DumpDir );
         } else
            DumpDir = Config.Dump_Path;
         GameVersion = Api( "version", "game" )?.ToString();
         BuildTypeMap();
         Api( "api_add dump.xml", (Func<string,object,object>) ApiDumpXml );
      }

      public static void GeoscapeOnShow () {
         if ( DidAutoDump || ! Config.Auto_Dump ) return; // Run only once
         DumpData();
         DidAutoDump = true;
         //Patch( typeof( GeoLevelController ), "OnLevelStart", postfix: nameof( LogWeapons ) );
         //Patch( typeof( GeoLevelController ), "OnLevelStart", postfix: nameof( LogAbilities ) );
         //Patch( typeof( GeoPhoenixFaction ), "OnAfterFactionsLevelStart", postfix: nameof( DumpResearches ) );
         //Patch( typeof( ItemManufacturing ), "AddAvailableItem", nameof( LogItem ) );
      }

      private  static List<BaseDef> AllDefs => GameUtl.GameComponent<DefRepository>().DefRepositoryDef.AllDefs;
      private  static readonly Dictionary< string, List<object> > ExportData = new Dictionary< string, List<object> >();
      private  static readonly Dictionary< string, Type >         ExportType = new Dictionary< string, Type >();
      internal static readonly List<Type> DumpedTypes = new List<Type>();

      private static void BuildTypeMap () {
         Info( "Buiding type map" );
         if ( Config.Dump_Lang_Csv )
            ExportType.Add( nameof( TermData ), typeof( TermData ) );
         ExportType.Add( nameof( BaseDef ), typeof( BaseDef ) );
         foreach ( var def in AllDefs ) {
            var type = def.GetType();
            while ( type != typeof( BaseDef ) ) {
               var name = type.Name;
               if ( ! ExportType.ContainsKey( name ) ) ExportType.Add( name, type );
               type = type.BaseType;
            }
         }
         DumpedTypes.AddRange( StringToTypes( Config.Dump_Defs ?? new string[ 0 ] ) );
         Verbo( "Mapped {0} types", ExportType.Count );
      }

      private static IEnumerable<Type> StringToTypes ( string[] types ) =>
         types?.Select( name => ExportType.TryGetValue( name, out Type type ) ? type : null ).Where( e => e != null ) ?? Enumerable.Empty<Type>();

      private static void DumpData () { try {
         Info( "Dumping data to {0}", DumpDir );
         Info( "Scanning data" );
         Type[] wanted = DumpedTypes.ToArray();
         foreach ( var e in AllDefs ) {
            foreach ( var type in wanted )
               if ( type.IsInstanceOfType( e ) )
                  AddDataToExport( type.Name, e );
         }
         if ( Config.Dump_Command_Csv ) ConsoleCommandAttribute.GetCommands().ForEach( e => AddDataToExport( "ConsoleCommand", e ) );
         if ( Config.Dump_Guid_Csv ) AllDefs.ForEach( e => AddDataToExport( "Guid", e ) );
         if ( Config.Dump_Lang_Csv ) AddLangToDump();
         AddOthersToDump();
         var sum = ExportData.Values.Sum( e => e.Count );
         Info( "{0} entries to dump", sum );
         var multithread = Config.Multithread;
         var tasks = multithread ? new List<Task>() : null;
         foreach ( var entry in ExportData ) { lock( entry.Value ) {
            Action dump;
            var name = entry.Key;
            if ( name == "Guid" ) dump = new GuidDumper( name, entry.Value ).DumpData;
            else if ( name == "ConsoleCommand" ) dump = new CommandDumper( name, entry.Value ).DumpData;
            else if ( name.StartsWith( "Text-" ) ) dump = new LangDumper( name, entry.Value ).DumpData;
            else dump = new BaseDefDumper( "Data-" + name, ExportType[ name ], entry.Value ).DumpData;
            if ( multithread ) {
               var task = Task.Run( dump );
               tasks.Add( task );
            } else
               dump();
         } }
         if ( multithread )
            Task.WaitAll( tasks.ToArray() );
         Info( "{0} entries dumped", sum );
         ExportData.Clear();
      } catch ( Exception ex ) { Error( ex ); } }

      private static object ApiDumpXml ( string file, object target ) {
         if ( target == null ) return null;
         if ( string.IsNullOrEmpty( file ) ) file = "API-" + target.GetType().Name + "." + DateTime.Now.ToString( "u" ).Replace( ':', '-' ).Replace( ' ', 'T' );
         String.Join( "_", file.Split( Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries ) ).TrimEnd( '.' );
         Info( "Dump data API called.  Dumping to {0}", file );
         new XmlDumper( file, target.GetType(), new object[]{ target }.ToList() ).DumpData();
         return true;
      }

      private static void AddLangToDump () {
         Info( "Scanning text" );
         foreach ( var src in LocalizationManager.Sources ) {
            if ( string.IsNullOrEmpty( src.Google_SpreadsheetKey ) ) continue;
            var name = "Text-" + ( string.IsNullOrEmpty( src.Google_SpreadsheetName ) ? src.Google_SpreadsheetKey : src.Google_SpreadsheetName );
            if ( name == null ) continue;
            if ( src.HasUnloadedLanguages() ) {
               Verbo( "Loading {0}", name );
               src.LoadAllLanguages( true );
            }
            AddDataToExport( name, src.GetLanguages( false ) );
            if ( src.mDictionary != null )
               foreach ( var term in src.mDictionary )
                  AddDataToExport( name, term.Value );
            if ( src.mDictionaryNoCategories != null )
               foreach ( var term in src.mDictionaryNoCategories )
                  AddDataToExport( name, term.Value );
         }
      }

      private static void AddOthersToDump () {
         var list = Config.Dump_Others.Where( e => ! string.IsNullOrWhiteSpace( e ) ).ToArray();
         if ( list == null || list.Length == 0 ) return;
         if ( Api( "api_info", "eval.js" ) == null ) {
            Warn( "'Dump_Others' requires JavaScript Runtime or other mod that provides the 'eval.js' api.  Delete the lines or change it to 'null' to remove this warning." );
            return;
         }
         for ( var i = 0 ; i < list.Length-1 ; i += 2 ) {
            string name = list[ i ] ?? "Others", cmd = list[ i + 1 ];
            var data = Api( "eval.js", cmd );
            if ( data == null ) {
               Info( "{0} is null", cmd );
               continue;
            } else if ( data is Exception ex ) {
               Error( "Error evaluating {0} - {1}", cmd, ex );
               continue;
            }
            AddDataToExport( name, data );
            if ( ! ExportType.ContainsKey( name ) )
               ExportType.Add( name, typeof( BaseDef ) );
         }
      }

      private static void AddDataToExport ( string name, object obj ) {
         if ( ! ExportData.TryGetValue( name, out var list ) )
            ExportData.Add( name, list = new List<object>() );
         list.Add( obj );
      }
   }
}