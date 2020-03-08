using Base.Core;
using Base.Defs;
using Base.Entities.Abilities;
using Base.Platforms;
using Base.UI;
using Harmony;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.DifficultySystem;
using PhoenixPoint.Geoscape.Entities.Requirement;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using static System.Reflection.BindingFlags;

namespace Sheepy.PhoenixPt.DumpInfo {

   public class Mod : SheepyMod {

      private static string ModsRoot;

      public void MainMod ( string modsRoot, Action< SourceLevels, object, object[] > logger = null ) {
         ModsRoot = modsRoot;
         SetLogger( logger );

         Patch( typeof( GeoPhoenixFaction ), "OnAfterFactionsLevelStart", null, postfix: nameof( DumpJson ) );
         //Patch( typeof( GeoLevelController ), "OnLevelStart", null, "LogWeapons" );
         //Patch( typeof( GeoLevelController ), "OnLevelStart", null, "LogAbilities" );
         //Patch( typeof( GeoPhoenixFaction ), "OnAfterFactionsLevelStart", postfix: "DumpResearches" );
         //Patch( typeof( ItemManufacturing ), "AddAvailableItem", "LogItem" );
      }

      private static Dictionary< Type, List<BaseDef> > ExportData = new Dictionary< Type, List<BaseDef> >();

      public static void DumpJson ( GeoLevelController __instance) { try {
         Type[] wanted = new Type[] { typeof( ResearchDef ),
            typeof( TacticalItemDef ), typeof( GroundVehicleItemDef ), typeof( VehicleItemDef ),
            typeof( AbilityDef ), typeof( AbilityTrackDef ), typeof( TacUnitClassDef ), typeof( GeoActorDef ),
            typeof( AlienMonsterClassDef ), typeof( BodyPartAspectDef ), typeof( GeoAlienBaseDef ), typeof( GeoMistGeneratorDef ),
            typeof( GeoHavenZoneDef ), typeof( GeoFactionDef ), typeof( PhoenixFacilityDef ),
            typeof( AchievementDef ), typeof( GeoscapeEventDef ), typeof( TacMissionDef ),
            typeof( DynamicDifficultySettingsDef ), typeof( GameDifficultyLevelDef ) };
         foreach ( var e in GameUtl.GameComponent<DefRepository>().DefRepositoryDef.AllDefs ) {
            var type = wanted.FirstOrDefault( cls => cls.IsInstanceOfType( e ) );
            if ( type == null ) continue;
            if ( ! ExportData.ContainsKey( type ) ) ExportData.Add( type, new List<BaseDef>() );
            ExportData[ type ].Add( e );
         }
         var txt = new StringBuilder();
         foreach ( var entry in ExportData ) {
            entry.Value.Sort( CompareDef );
            var typeName = entry.Key.Name;
            var path = Path.Combine( ModsRoot, typeName + ".xml" );
            File.Delete( path );
            using ( var writer = new StreamWriter( new BufferedStream( new FileStream( path, FileMode.Create ) ) ) ) {
               writer.Write( $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r<{typeName}>" );
               foreach ( var def in entry.Value )
                  writer.Write( ToXml( def, txt ).ToString() );
               writer.Write( $"</{typeName}>" );
               writer.Flush();
            }
            entry.Value.Clear();
         }
      } catch ( Exception ex ) { Error( ex ); } }

      private static int CompareDef ( BaseDef a, BaseDef b ) {
         string aid = a.Guid, bid = b.Guid;
         if ( aid == null ) return bid == null ? 0 : -1;
         if ( bid == null ) return 1;
         return aid.CompareTo( bid );
      }

      private static StringBuilder ToXml ( object subject, StringBuilder txt ) {
         txt.Clear();
         Obj2Xml( subject, 0, txt );
         return txt.Append( '\r' );
      }

      private static void Obj2Xml ( object subject, int level, StringBuilder txt ) {
         if ( subject == null ) { txt.Append( "null" ); return; }
         if ( subject is string str ) { txt.Append( str ); return; }
         if ( subject is LocalizedTextBind l10n ) { txt.Append( l10n.ToString() ); return; }
         var type = subject.GetType();
         if ( type.IsValueType ) { txt.Append( EscXml( subject.ToString() ) ); return; }
         if ( level == 0 ) { Mem2Xml( type.Name, subject, 1, txt ); return; }
         if ( level > 10 ) { txt.Append( "..." ); return; }
         if ( type.FullName.StartsWith( "UnityEngine.", StringComparison.InvariantCulture ) ) { txt.Append( type.FullName ); return; }
         if ( subject is IEnumerable list ) {
            foreach ( var e in list ) {
               var typeName = EscXml( e?.GetType().ToString() ?? "null" );
               txt.Append( '<' ).Append( typeName ).Append( '>' );
               Obj2Xml( e, level + 1, txt );
               txt.Append( "</" ).Append( typeName ).Append( '>' );
            }
            return;
         }
         foreach ( var f in type.GetFields( Public | NonPublic | Instance ) ) try {
            Mem2Xml( f.Name, f.GetValue( subject ), level + 1, txt );
         } catch ( ApplicationException ex ) {
            Mem2Xml( f.Name, ex.GetType().Name, level + 1, txt );
         }
         foreach ( var f in type.GetProperties( Public | NonPublic | Instance ) ) try {
            Mem2Xml( f.Name, f.GetValue( subject ), level + 1, txt );
         } catch ( ApplicationException ex ) {
            Mem2Xml( f.Name, ex.GetType().Name, level + 1, txt );
         }
//         } catch ( ApplicationException ex ) { Info( txt ); throw new InvalidOperationException( $"{type.FullName}.{f.Name}", ex ); }
         return;
      }

      private static void Mem2Xml ( string name, object val, int level, StringBuilder txt ) {
         txt.Append( '<' ).Append( EscXml( name ) ).Append( '>' );
         Obj2Xml( val, level + 1, txt );
         txt.Append( "</" ).Append( EscXml( name ) ).Append( '>' );
      }

      private static string EscXml ( string txt ) =>
         txt.Replace( "<", "&lt;" ).Replace( ">", "&gt;" ).Replace( "\"", "&quot;" );

      public static void DumpWeapons ( GeoLevelController __instance ) { try {
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

      public static void LogAbilities ( GeoLevelController __instance ) {
         try {
            foreach ( var ability in GameUtl.GameComponent<DefRepository>().GetAllDefs<TacticalAbilityDef>() ) {
               Info( "{0} {1} {2}", ability.ViewElementDef?.DisplayName1.Localize(), ability.name, ability.Guid );
            }
         } catch ( Exception ex ) { Error( ex ); }
      }


      public static void DumpResearches ( GeoPhoenixFaction __instance ) { try {
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

      public static string ReqToString ( SimpleRequirementDef req ) {
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

      public static string ReqToString ( ResearchRequirement req ) {
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

      public static string ReqToString ( ResearchReward req ) {
         return ReqToString( req.Def );
      }

      public static string ReqToString ( ResearchRewardDef def ) {
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
   }
}