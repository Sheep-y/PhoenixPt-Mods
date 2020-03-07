using System;
using System.Linq;
using System.Reflection;

namespace Sheepy.PhoenixPt_DumpInfo {
   using Base.Core;
   using Base.Defs;
   using Harmony;
   using PhoenixPoint.Common.Entities.Items;
   using PhoenixPoint.Geoscape.Entities.Requirement;
   using PhoenixPoint.Geoscape.Entities.Research;
   using PhoenixPoint.Geoscape.Entities.Research.Requirement;
   using PhoenixPoint.Geoscape.Entities.Research.Reward;
   using PhoenixPoint.Geoscape.Levels;
   using PhoenixPoint.Geoscape.Levels.Factions;
   using PhoenixPoint.Tactical.Entities;
   using PhoenixPoint.Tactical.Entities.Abilities;
   using PhoenixPoint.Tactical.Entities.DamageKeywords;
   using PhoenixPoint.Tactical.Entities.Weapons;
   using System.Collections.Generic;
   using static System.Reflection.BindingFlags;

   public class Mod {
      private static Logging.Logger Log = new Logging.Logger( "Mods/SheepyMods.log" );

      public static void Init () {
         Log.Delete();
         HarmonyInstance harmony = HarmonyInstance.Create( typeof( Mod ).Namespace );

         //Patch( harmony, typeof( GeoLevelController ), "OnLevelStart", null, "LogWeapons" );
         //Patch( harmony, typeof( GeoLevelController ), "OnLevelStart", null, "LogAbilities" );
         Patch( harmony, typeof( GeoPhoenixFaction ), "OnAfterFactionsLevelStart", null, "DumpResearches" );
         //Patch( harmony, typeof( TacticalLevelController ), "OnLevelStart", null, "AfterLevelStart_PatchWeapons" );
         //Patch( harmony, typeof( GeoFaction ), "RebuildBonusesFromResearchState", "LogList" );
         //Patch( harmony, typeof( ItemManufacturing ), "AddAvailableItem", "LogItem" );
      }

      #region Modding helpers
      private static void Patch ( HarmonyInstance harmony, Type target, string toPatch, string prefix, string postfix = null ) {
         Patch( harmony, target.GetMethod( toPatch, Public | NonPublic | Instance | Static ), prefix, postfix );
      }

      private static void Patch ( HarmonyInstance harmony, MethodInfo toPatch, string prefix, string postfix = null ) {
         harmony.Patch( toPatch, ToHarmonyMethod( prefix ), ToHarmonyMethod( postfix ) );
      }

      private static HarmonyMethod ToHarmonyMethod ( string name ) {
         if ( name == null ) return null;
         MethodInfo func = typeof( Mod ).GetMethod( name );
         if ( func == null ) throw new NullReferenceException( name + " is null" );
         return new HarmonyMethod( func );
      }

      private static bool LogError ( Exception ex ) {
         Log.Error( ex );
         return true;
      }
      #endregion

      public static bool LogList ( GeoFaction __instance ) { try {
         foreach ( var research in __instance.Research.Completed ) {
            Log.Info( "Research {0}", research.GetLocalizedName() );
            foreach ( var reward in research.Rewards ) {
               Log.Info( reward.GetType() );
               if ( reward.RewardOnce ) continue;
               reward.GiveReward( __instance );
            }
         }
         return false;
      } catch ( Exception ex ) { return LogError( ex ); } }

      public static void LogItem ( ItemManufacturing __instance, IManufacturable item ) { try {
         //if ( item.RelatedItemDef.name == "PX_GrenadeLauncher_WeaponDef" || item.RelatedItemDef.name == "PX_ShotgunRifle_WeaponDef" )
         //Log.Info( Logging.Logger.Stacktrace );
         Log.Info( "IManufacturable {0}", item.RelatedItemDef.name );
      } catch ( Exception ex ) { LogError( ex ); } }


      public static void LogWeapons ( GeoLevelController __instance ) { try {
         // Build keyword list and weapon list - heavy code, do once
         var keywords = GameUtl.GameComponent<DefRepository>().GetAllDefs<DamageKeywordDef>().ToDictionary( e => e.name );
         var weapons = GameUtl.GameComponent<DefRepository>().GetAllDefs<WeaponDef>().ToDictionary( e => e.name );

         // Add paralysing damage to Hailstorm GT
         //weapons[ "SY_Aspida_Arms_WeaponDef" ]?.DamagePayload.DamageKeywords.Add(
         //   new DamageKeywordPair(){ DamageKeywordDef = keywords[ "Paralysing_DamageKeywordDataDef" ], Value = 30 });
         foreach ( var weapon in GameUtl.GameComponent<DefRepository>().GetAllDefs<WeaponDef>() ) {
            DamagePayload dmg = weapon.DamagePayload;
            Log.Info( weapon.GetDisplayName().Localize() );
            Log.Info( "{0} [{1} {2}]", weapon.name, weapon.Guid, weapon.ResourcePath );
            Log.Info( "Tags: {0}", weapon.Tags.Join( e => e.ResourcePath ) );
            Log.Info( "AP {0}, Weight {1}, Accuracy {2}, Ammo {3}, Manufacturable {4}", weapon.APToUsePerc, weapon.CombinedWeight(), weapon.EffectiveRange, weapon.ChargesMax, weapon.CanManufacture( __instance.PhoenixFaction ) );
            Log.Info( "Damage: {0}", dmg.DamageKeywords.Select( e => e.DamageKeywordDef.name + " " + e.Value ).Join() );
            Log.Info( "{0} bullets x{1} shots, each bullet cost {7} ammo, AoE {2}, Cone {3}, Spread {4}:{5}, Range {6}",
               dmg.ProjectilesPerShot, dmg.AutoFireShotCount, dmg.AoeRadius, dmg.ConeRadius, weapon.SpreadRadius, weapon.SpreadDegrees, dmg.Range, weapon.AmmoChargesPerProjectile );
            if ( weapon.UnlockRequirements != null ) Log.Info( "Requires {0}", ReqToString( weapon.UnlockRequirements ) );
            Log.Info( " " );
            //weapon.DamagePayload.DamageKeywords.Add( new DamageKeywordPair(){ DamageKeywordDef = keywords[ "Paralysing_DamageKeywordDataDef" ], Value = 8 });
         }
         Log.Flush();
      } catch ( Exception ex ) { LogError( ex ); } }

      public static void LogAbilities ( GeoLevelController __instance ) {
         try {
            foreach ( var ability in GameUtl.GameComponent<DefRepository>().GetAllDefs<TacticalAbilityDef>() ) {
               Log.Info( "{0} {1} {2}", ability.ViewElementDef?.DisplayName1.Localize(), ability.name, ability.Guid );
            }
         } catch ( Exception ex ) { LogError( ex ); }
      }


      public static void DumpResearches ( GeoPhoenixFaction __instance ) { try {
         //foreach ( var item in GameUtl.GameComponent<DefRepository>().GetAllDefs<ItemDef>() ) {
         //   if ( item.UnlockRequirements == null ) continue;
         //   Log.Info( item.GetDisplayName().Localize() );
         //   Log.Info( "Id {0} [{1} {2}]", item.name, item.Guid, item.ResourcePath );
         //   Log.Info( "Unlocked By: {0}", ReqToString( item.UnlockRequirements ) );
         //   Log.Info( " " );
         //}

         foreach ( var tech in __instance.Research.AllResearchesArray ) {
            ResearchDef def = tech.ResearchDef;
            Log.Info( def.ViewElementDef.DisplayName1.Localize() );
            Log.Info( "Id {0} [{1} {2}]", def.Id, def.Guid, def.ResourcePath );
            Log.Info( "Point {0}, Priority {1}, Tags {2}", tech.ResearchCost, def.Priority, def.Tags?.Join( e => e.ResourcePath ) ?? "(null)" );
            if ( ! string.IsNullOrWhiteSpace( def.ViewElementDef.BenefitsText?.LocalizationKey ) )
               Log.Info( "Benefit: {0}", def.ViewElementDef.BenefitsText.Localize() );
            if ( def.Resources != null && def.Resources.Count() > 0 )
               Log.Info( "{0}", def.Resources.Join( e => e.Type + " " + e.Value ) );
            if ( def.Unlocks != null && def.Unlocks.Count() > 0 )
               Log.Info( "Unlocks: {0}", def.Unlocks.Join( ReqToString ) );
            if ( tech.ManufactureRewards != null && tech.ManufactureRewards.Count() > 0 )
               Log.Info( "Allows: {0}", tech.ManufactureRewards.Join( ReqToString ) );
            if ( tech.Rewards != null && tech.Rewards.Count() > 0 )
               Log.Info( "Awards: {0}", tech.Rewards.Join( ReqToString ) );
            if ( tech.GetUnlockRequirements() != null && tech.GetUnlockRequirements().Count() > 1 )
               Log.Info( "UnlockBy: {0}", tech.GetUnlockRequirements().Join( ReqToString ) );
            Log.Info( "RevealBy: {0}", tech.GetRevealRequirements()?.Join( ReqToString ) ?? "(null)" );
            Log.Info( "Requires: {0}", tech.GetResearchRequirements()?.Join( ReqToString ) ?? "(null)" );
            Log.Info( " " );
         }
         Log.Flush();
      } catch ( Exception ex ) { LogError( ex ); } }

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