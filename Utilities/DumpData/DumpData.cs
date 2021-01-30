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
            Warn( "'Dump_Others' requires Scripting Library or other mod that provides the 'eval.js' api.  Delete the lines or change it to 'null' to remove this warning." );
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

      /*
      #region Manual dump code
      private static void DumpWeapons ( GeoLevelController __instance ) { try {
         // Build keyword list and weapon list - heavy code, do once
         var keywords = GameUtl.GameComponent<DefRepository>().GetAllDefs<DamageKeywordDef>().ToDictionary( e => e.name );
         var weapons = GameUtl.GameComponent<DefRepository>().GetAllDefs<WeaponDef>().ToDictionary( e => e.name );

         // Add paralysing damage to Hailstorm GT
         //weapons[ "SY_Aspida_Arms_WeaponDef" ]?.DamagePayload.DamageKeywords.Add(
         //   new DamageKeywordPair(){ DamageKeywordDef = keywords[ "Paralysing_DamageKeywordDataDef" ], Value = 30 });
         foreach ( var weapon in GameUtl.GameComponent<DefRepository>().GetAllDefs<WeaponDef>() ) {
            DamagePayload dmg = weapon.DamagePayload;
            Info( weapon.GetDisplayName().Localize() );
            Info( "{0} [{1} {2}]", weapon.name, weapon.Guid, weapon.ResourcePath );
            Info( "Traits: {1} / Tags: {0}", weapon.Tags?.Join( e => e.name ), weapon.Traits?.Join() );
            Info( "AP {0}, Weight {1}, Accuracy {2}, Ammo {3}, Free Ammo {4}, HP {5}, Armour {6}",
               weapon.APToUsePerc, weapon.CombinedWeight(), weapon.EffectiveRange, weapon.ChargesMax, weapon.FreeReloadOnMissionEnd, weapon.HitPoints, weapon.Armor );
            Info( "Slot {0}, Hands {1}, Fumble {2}, Non-Prof accuracy {3}, Manufacturable {4}, NoDropChance {5}",
               weapon.HolsterSlotDef?.name, weapon.HandsToUse, weapon.FumblePerc, weapon.IncompetenceAccuracyMultiplier, weapon.CanManufacture( __instance.PhoenixFaction ), weapon.DestroyOnActorDeathPerc );
            Info( "Damage: {0}", dmg.DamageKeywords.Select( e => e.DamageKeywordDef.name + " " + e.Value ).Join() );
            Info( "{0} bullets x{1} shots, each bullet cost {2} ammo, Range {3} Max {4}", dmg.ProjectilesPerShot, dmg.AutoFireShotCount, weapon.AmmoChargesPerProjectile, dmg.Range, weapon.MaximumRange );
            Info( "AoE {0}, Cone {1}, Spread {2}:{3}", dmg.AoeRadius, dmg.ConeRadius, weapon.SpreadRadius, weapon.SpreadDegrees, weapon.AmmoChargesPerProjectile );
            if ( weapon.UnlockRequirements != null ) Info( "Requires {0}", ReqToString( weapon.UnlockRequirements ) );
            Info( "{0}", weapon.Abilities?.Join( e => e.GetType().ToString() ) );
            Info( " " );
            //weapon.DamagePayload.DamageKeywords.Add( new DamageKeywordPair(){ DamageKeywordDef = keywords[ "Paralysing_DamageKeywordDataDef" ], Value = 8 });
         }
      } catch ( Exception ex ) { Error( ex ); } }

      private static void LogAbilities ( GeoLevelController __instance ) {
         try {
            foreach ( var ability in GameUtl.GameComponent<DefRepository>().GetAllDefs<TacticalAbilityDef>() ) {
               Info( "{0} {1} {2}", ability.ViewElementDef?.DisplayName1.Localize(), ability.name, ability.Guid );
            }
         } catch ( Exception ex ) { Error( ex ); }
      }


      private static void DumpResearches ( GeoPhoenixFaction __instance ) { try {
         //foreach ( var item in GameUtl.GameComponent<DefRepository>().GetAllDefs<ItemDef>() ) {
         //   if ( item.UnlockRequirements == null ) continue;
         //   Info( item.GetDisplayName().Localize() );
         //   Info( "Id {0} [{1} {2}]", item.name, item.Guid, item.ResourcePath );
         //   Info( "Unlocked By: {0}", ReqToString( item.UnlockRequirements ) );
         //   Info( " " );
         //}

         foreach ( var tech in __instance.Research.AllResearchesArray ) {
            ResearchDef def = tech.ResearchDef;
            Info( def.ViewElementDef.DisplayName1.Localize() );
            Info( "Id {0} [{1} {2}]", def.Id, def.Guid, def.ResourcePath );
            Info( "Point {0}, Priority {1}, Tags {2}", tech.ResearchCost, def.Priority, def.Tags?.Join( e => e.ResourcePath ) ?? "(null)" );
            if ( ! string.IsNullOrWhiteSpace( def.ViewElementDef.BenefitsText?.LocalizationKey ) )
               Info( "Benefit: {0}", def.ViewElementDef.BenefitsText.Localize() );
            if ( def.Resources != null && def.Resources.Count() > 0 )
               Info( "{0}", def.Resources.Join( e => e.Type + " " + e.Value ) );
            if ( def.Unlocks != null && def.Unlocks.Count() > 0 )
               Info( "Unlocks: {0}", def.Unlocks.Join( ReqToString ) );
            if ( tech.ManufactureRewards != null && tech.ManufactureRewards.Count() > 0 )
               Info( "Allows: {0}", tech.ManufactureRewards.Join( ReqToString ) );
            if ( tech.Rewards != null && tech.Rewards.Count() > 0 )
               Info( "Awards: {0}", tech.Rewards.Join( ReqToString ) );
            if ( tech.GetUnlockRequirements() != null && tech.GetUnlockRequirements().Count() > 1 )
               Info( "UnlockBy: {0}", tech.GetUnlockRequirements().Join( ReqToString ) );
            Info( "RevealBy: {0}", tech.GetRevealRequirements()?.Join( ReqToString ) ?? "(null)" );
            Info( "Requires: {0}", tech.GetResearchRequirements()?.Join( ReqToString ) ?? "(null)" );
            Info( " " );
         }
      } catch ( Exception ex ) { Error( ex ); } }

      private static string ReqToString ( SimpleRequirementDef req ) {
         if ( req is ItemResearchedRequirementDef res ) {
            return $"Item Researched";
         } else if ( req is AndRequirementDef and ) {
            return "(" + and.SubRequirements.Join( ReqToString, " and " ) + ")";
         } else if ( req is OrRequirementDef or ) {
            return "(" + or.SubRequirements.Join( ReqToString, " or " ) + ")";
         } else if ( req is ClassRequirementDef cls ) {
            return cls.RequiredClass.className;
         } else
            return req.GetType().ToString();
      }

      private static string ReqToString ( ResearchRequirement req ) {
         if ( req is ExistingResearchRequirement && req.RequirementDef is ExistingResearchRequirementDef resDef ) {
            return (resDef.Faction?.GetPPName() ?? "") + " " + (resDef.Research?.name ?? "") + " " + (resDef.Tag?.name ?? "");
         } else if ( req is FactionDiplomacyRequirement && req.RequirementDef is FactionDiplomacyRequirementDef dipDef ) {
            return (dipDef.WithFaction?.GetPPName() ?? " Faction?") + " >= " + dipDef.DiplomacyState;
         } else if ( req is IsFactionResearchRequirement && req.RequirementDef is IsFactionResearchRequirementDef freDef ) {
            return $"Is {freDef.Faction?.GetPPName() ?? "Unknown Faction"}";
         } else if ( req is EncounterVariableResearchRequirement && req.RequirementDef is EncounterVariableResearchRequirementDef encDef ) {
            return $"Variable {encDef.VariableName}";
         } else if ( req is ActorResearchRequirement && req.RequirementDef is ActorResearchRequirementDef killDef ) {
            return killDef.Actor != null
               ? $"Kill a {killDef.Actor.name}"
               : $"Kill something with {killDef.Tag?.name ?? "(null)"} tag";
         } else if ( req is CaptureActorResearchRequirement && req.RequirementDef is CaptureActorResearchRequirementDef capDef ) {
            return capDef.Actor != null
               ? $"Capture a {capDef.Actor.name}"
               : $"Capture something with {capDef.Tag?.name ?? "(null)"} tag";
         } else if ( req is DestroySiteResearchRequirement && req.RequirementDef is DestroySiteResearchRequirementDef clearDef ) {
            return clearDef.AlienBaseDef != null
               ? $"Destroy a {clearDef.AlienBaseDef.name}"
               : $"Clear a {clearDef.FactionDef?.GetPPName()} {clearDef.Type}";
         } else if ( req is VisitSiteRequirement && req.RequirementDef is VisitSiteRequirementDef visitDef ) {
            return visitDef.AlienBaseDef != null
               ? $"Visit a {visitDef.AlienBaseDef.name}"
               : $"Visit a {visitDef.FactionDef?.GetPPName()} {visitDef.Type}";
         } else if ( req is RevealSiteResearchRequirement && req.RequirementDef is RevealSiteResearchRequirementDef exploreDef ) {
            return exploreDef.AlienBaseDef != null
               ? $"Explore a {exploreDef.AlienBaseDef}"
               : $"Explore a {exploreDef.FactionDef?.GetPPName()} {exploreDef.Type}";
         } else if ( req is ReceiveItemResearchRequirement && req.RequirementDef is ReceiveItemResearchRequirementDef itemDef ) {
            return itemDef.ItemDef != null
               ? $"Acquired a {itemDef.ItemDef.GetDisplayName().Localize()}"
               : $"Acquired an item with {itemDef.Tag.name} tag";
         } else if ( req is EncounteredVoxelResearchRequirement && req.RequirementDef is EncounteredVoxelResearchRequirementDef voxelDef ) {
            return $"Experienced {voxelDef.Type}";
         } else
            return $"??? {req.GetType().Name}.{req.RequirementDef?.GetType().Name ?? "(null)"} ???";
      }

      private static string ReqToString ( ResearchReward req ) {
         return ReqToString( req.Def );
      }

      private static string ReqToString ( ResearchRewardDef def ) {
         if ( def is ManufactureResearchRewardDef man ) {
            return "Items [" + man.Items.Join( e => e.GetDisplayName()?.Localize() ?? e.name ) + "]";
         } else if ( def is ClassResearchRewardDef cls ) {
            return "Class " + cls.SpecializationDef?.name;
         } else if ( def is FacilityResearchRewardDef fac ) {
            return "Facility [" + fac.Facilities.Join( e => e.name ) + "]";
         } else if ( def is HavenZoneResearchRewardDef zone ) {
            return "Zone " + zone.Zone?.name;
         } else if ( def is CharacterStatResearchRewardDef stat ) {
            return "Stat " + stat.PassiveModifierAbilityDef?.name;
         } else if ( def is FacilityBuffResearchRewardDef bb ) {
            return $"Building {bb.Facility?.ViewElementDef?.DisplayName1?.Localize()} {bb.ModificationType} {bb.Increase}";
         } else if ( def is HavenZoneBuffResearchRewardDef zb ) {
            return $"Output +[Global {zb.GlobalModifier}, Res {zb.ResearchModifier}, Prod {zb.ProductionModifier}, Deploy {zb.DeploymentModifier}, Food {zb.FoodModifier}]";
         } else if ( def is FactionModifierResearchRewardDef fb ) {
            return $"Faction +[Atk {fb.HavenAttackerStrengthModifier}, Def {fb.HavenDefenseModifier}, DefTime {fb.HavenDefensePrepTimeHours}, "+
                   $"Exp {fb.ExperienceAfterMissionModifier}, Recruit {fb.RecruitmentModifier}, Breach {fb.BreachPointModifier}, Scan {fb.ScannerRangeModifier}, ScanBase {fb.ScannerCanRevealAlienBases}]";
         } else if ( def is AircraftBuffResearchRewardDef air ) {
            return $"Aircraft {air.VehicleDef?.ViewElement?.DisplayName1?.Localize()} [HP {air.ModData.MaxMaintenancePointsMultiplier}, Speed {air.ModData.SpeedMultiplier}, Range {air.ModData.RangeMultiplier}, Space {air.ModData.SpaceForUnits}]";
         } else if ( def is ResearchBonusResearchRewardDef rb ) {
            return $"Research {rb.Tag?.ResourcePath} +{rb.Amount}";
         } else if ( def is StatusBuffResearchRewardDef sb ) {
            return $"Status {sb.StatusBuffEffectDef.name}";
         } else
            return def.GetType().Name;
      }
      #endregion
      */
   }
}