using Base.UI;
using Harmony;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Missions;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Reflection.BindingFlags;

namespace Sheepy.PhoenixPt.NoWar {

   public class ModSettings {
      public bool Faction_Attack_Zone = true;
      public bool Alien_Attack_Zone = false;
      public bool Attack_Raise_Alertness = true;
      public bool Attack_Raise_Faction_Alertness = true;

      public bool Stop_Continuous_Attack = true;
      public int No_Attack_When_Sieged_Difficulty = 2;
      public int One_Global_Attack_Difficulty = 0;
      public int One_Attack_Per_Faction_Difficulty = 1;

      internal bool Has_Less_Attack => Stop_Continuous_Attack ||
              No_Attack_When_Sieged_Difficulty >= 0 || One_Global_Attack_Difficulty >= 0 || One_Attack_Per_Faction_Difficulty >= 0;

      public string Defense_Boost = "{ Default: 0.5, Alert: 0.2, HighAlert: 0.1, AttackerNewJericho: 0.5 }";
   }

   public class Mod : ZyAdvMod {
      public static ModSettings Settings;

      public static void Init () => new Mod().MainMod();

      public void MainMod ( ModSettings settings = null, Action< SourceLevels, object, object[] > logger = null ) {
         SetLogger( logger );
         Settings = settings = ReadSettings( settings );

         if ( settings.Faction_Attack_Zone || settings.Alien_Attack_Zone ) {
            BatchPatch( "Zoned attacks", () => {
               Patch( typeof( GeoHavenDefenseMission ), "UpdateGeoscapeMissionState", nameof( BeforeGeoMission_StoreMission ), nameof( AfterGeoMission_Cleanup ) );
               Patch( typeof( GeoSite ), "DestroySite", nameof( Override_DestroySite ) );
               Patch( typeof( GeoscapeLog ), "Map_SiteMissionStarted", postfix: nameof( AfterSiteMission_AmendLog ) );
               Patch( typeof( GeoscapeLog ), "Map_SiteMissionEnded", postfix: nameof( AfterSiteMission_AmendLog ) );
               CommitPatch();
            } );
         }

         if ( settings.Attack_Raise_Alertness || settings.Attack_Raise_Faction_Alertness )
               Patch( typeof( GeoSite ), "DestroySite", postfix: nameof( AfterDestroySite_RaiseAlertness ) );

         if ( settings.Defense_Boost != null )
            Patch( typeof( GeoHavenDefenseMission ), "GetDefenseDeployment", postfix: nameof( AfterDefDeploy_BoostDef ) );

         if ( settings.Has_Less_Attack ) {
            BatchPatch( "Less attacks", () => {
               Patch( typeof( GeoLevelController ), "OnLevelStart", postfix: nameof( AfterLevelStart_StoreDiff ) );
               Patch( typeof( VehicleFactionController ), "UpdateNavigation", nameof( BeforeNav_Drop_Attack ), nameof( AfterNav_Restore ) );
               Patch( typeof( GeoFaction ), "AttackHavenFromVehicle", nameof( Override_Attack ) );
            } );
         }
      }

      private static bool IsAlien ( IGeoFactionMissionParticipant f ) => f is GeoAlienFaction;
      private static bool IsPhoenixPt ( IGeoFactionMissionParticipant f ) => f is GeoPhoenixFaction;
      private static bool IsAlienOrPP ( IGeoFactionMissionParticipant f ) => IsAlien( f ) || IsPhoenixPt( f );

      #region Parallel_Attack
      private static int difficulty = 1; // Default to Vet if anything goes wrong
      private static IGeoFactionMissionParticipant lastAttacker = null;

      public static void AfterLevelStart_StoreDiff ( GeoLevelController __instance ) { try {
         GameDifficultyLevelDef diff = __instance.CurrentDifficultyLevel;
         difficulty = __instance.DynamicDifficultySystem.DifficultyLevels.ToList().IndexOf( diff );
         lastAttacker = null;
         Info( "Attacker reset. Difficulty is {0}", difficulty ); // 0 = Rookie, 1 = Veteran, 2 = Hero, 3 = Legend
      } catch ( Exception ex ) { Error( ex ); } }

      private static bool AtMaxFight ( GeoLevelController geoLevel, IGeoFactionMissionParticipant attacker ) { try {
         if ( geoLevel?.Map == null || IsAlienOrPP( attacker ) ) return false;
         foreach ( GeoSite site in geoLevel.Map.AllSites ) {
            if ( ! ( site.ActiveMission is GeoHavenDefenseMission mission ) ) continue;
            if ( Settings.No_Attack_When_Sieged_Difficulty >= difficulty && IsAlien( mission.GetEnemyFaction() ) && site.Owner == attacker ) {
               Info( "Attack prevented; attacker is being sieged by alian", site.Name );
               return true;
            }
            if ( Settings.One_Global_Attack_Difficulty >= difficulty ) {
               Info( "Attack prevented; {0} already under siege", site.Name );
               return true; // Rokkie and Veteran limits to 1 faction war on globe
            }
            if ( Settings.One_Attack_Per_Faction_Difficulty >= difficulty && mission.GetEnemyFaction() == attacker ) {
               Info( "Attack prevented; {0} already sieging {1}", attacker.GetPPName(), site.Name );
               return true; // Hero and Legend limits to 1 attack per faction
            }
         }
         return false;
      } catch ( Exception ex ) { return ! Error( ex ); } }

      private static bool ShouldStopFight ( GeoLevelController geoLevel, IGeoFactionMissionParticipant from ) { try {
         if ( Settings.Stop_Continuous_Attack && lastAttacker != null && from == lastAttacker ) {
            Info( "Attack prevented; {0} has already attacked", from.GetPPName() );
            return true;
         }
         return AtMaxFight( geoLevel, from );
      } catch ( Exception ex ) { return ! Error( ex ); } }

      [ HarmonyPriority( Priority.Low ) ]
      public static void BeforeNav_Drop_Attack ( VehicleFactionController __instance, ref float? __state ) { try {
         if ( ShouldStopFight( __instance.Vehicle?.GeoLevel, __instance.Vehicle?.Owner ) ) {
            Info( "Discouraging {0} from faction war", __instance.Vehicle.Name );
            __state = __instance.ControllerDef.FactionInWarWeightMultiplier;
            __instance.ControllerDef.FactionInWarWeightMultiplier = -2f;
         } else {
            __state = null;
         }
      } catch ( Exception ex ) { Error( ex ); } }

      [ HarmonyPriority( Priority.High ) ]
      public static void AfterNav_Restore ( VehicleFactionController __instance, float? __state ) {
         if ( __state != null ) __instance.ControllerDef.FactionInWarWeightMultiplier = __state.Value;
      }

      [ HarmonyPriority( Priority.High ) ]
      public static bool Override_Attack ( GeoFaction __instance, GeoVehicle vehicle, GeoSite site, GeoLevelController ____level ) { try {
         Info( "{0} wants to attack {1}", vehicle.Name, site.Name );
         if ( ShouldStopFight( ____level, vehicle?.Owner ) ) {
            Info( "{0} attack prevented at {2}", __instance.Name.Localize(), vehicle.Name, site.Name );
            return false;
         }
         lastAttacker = vehicle.Owner;
         return true;
      } catch ( Exception ex ) { return Error( ex ); } }
      #endregion

      #region Alertness
      public static void AfterDestroySite_RaiseAlertness ( GeoSite __instance ) { try {
         var owner = __instance.Owner;
         if ( IsAlienOrPP( owner ) ) return;
         if ( Settings.Attack_Raise_Faction_Alertness ) {
            Info( "{0} has loss. Raising alertness of {0}", owner.GetPPName() );
            foreach ( var site in owner.Sites )
               typeof( GeoHaven ).GetMethod( "IncreaseAlertness", NonPublic | Instance ).Invoke( site, null );
         } else if ( __instance.IsActive ) {
            typeof( GeoHaven ).GetMethod( "IncreaseAlertness", NonPublic | Instance ).Invoke( __instance, null );
            Info( "{0} has loss. It is now on {1}", owner.Name, DefenseMission.Haven.AlertLevel );
         }
      } catch ( Exception ex ) { Error( ex ); } }
      #endregion

      #region Attack Zone
      private static GeoHavenDefenseMission DefenseMission = null;

      [ HarmonyPriority( Priority.VeryHigh ) ]
      public static void BeforeGeoMission_StoreMission  ( GeoHavenDefenseMission __instance ) { DefenseMission = __instance; }
      public static void AfterGeoMission_Cleanup () { DefenseMission = null; }

      private static bool SkipConvertToZone ( IGeoFactionMissionParticipant attacker ) {
         return IsPhoenixPt( attacker ) &&
              ( ! Settings.Alien_Attack_Zone && IsAlien( attacker ) ) ||
              ( ! Settings.Faction_Attack_Zone && ! IsAlien( attacker ) );
      }

      [ HarmonyPriority( Priority.High ) ]
      public static bool Override_DestroySite ( GeoSite __instance ) { try {
         if ( DefenseMission == null ) return true;
         Info( "{0} loss.", __instance.Name );
         if ( SkipConvertToZone( DefenseMission?.GetEnemyFaction() ) ) return true;
         GeoHavenZone zone = DefenseMission.AttackedZone;
         zone.AddDamage( zone.Health.IntValue );
         Info( "Fall of {0} converted to {1} destruction.", __instance.Name, zone.Def.ViewElementDef.DisplayName1 );
         return false;
      } catch ( Exception ex ) { return Error( ex ); } }

      public static void AfterSiteMission_AmendLog ( GeoSite site, GeoMission mission, List<GeoscapeLogEntry> ____entries ) { try {
         if ( ! ( mission is GeoHavenDefenseMission defense ) ) return;
         if ( SkipConvertToZone( DefenseMission?.GetEnemyFaction() ) ) return;
         LocalizedTextBind zoneName = defense.AttackedZone?.Def?.ViewElementDef?.DisplayName1;
         if ( zoneName == null || ____entries == null || ____entries.Count < 1 ) return;
         GeoscapeLogEntry entry = ____entries[ ____entries.Count - 1 ];
         Verbo( "Converting site invasion message to zone invasion." );
         entry.Parameters[0] = new LocalizedTextBind( site.SiteName.Localize() + " " + TitleCase( zoneName.Localize() ), true );
      } catch ( Exception ex ) { Error( ex ); } }
      #endregion

      #region Defense Boost
      [ HarmonyPriority( Priority.High ) ]
      public static void AfterDefDeploy_BoostDef ( GeoHavenDefenseMission __instance, ref int __result, GeoHaven haven ) { try {
         Info( "{0} deploy {1} vs {2}x{3}", haven.Site.Name, __result, __instance.AttackerDeployment, haven.Site.Owner.FactionStatModifiers.HavenAttackerStrengthModifier );
         float deploy = __result;
         switch ( haven?.AlertLevel ) {
            case GeoHaven.HavenAlertLevel.Alert : deploy *= 1.2f; break;
            case GeoHaven.HavenAlertLevel.HighAlert : deploy *= 1.3f; break;
         }
         GeoFaction attacker = __instance?.GetEnemyFaction() as GeoFaction;
         GeoLevelController geoLevel = haven?.Site?.GeoLevel;
         if ( attacker == geoLevel.AnuFaction || attacker == geoLevel.SynedrionFaction ) {
            deploy *= 1.5f;
         } else if ( attacker == geoLevel.NewJerichoFaction ) {
            deploy *= 2f;
         }
         __result = (int) Math.Round( deploy );
         Info( "Bumping defender to {0}", __result );
      } catch ( Exception ex ) { Error( ex ); } }
      #endregion
   }
}