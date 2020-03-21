using Base.UI;
using Harmony;
using Newtonsoft.Json;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Missions;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Reflection.BindingFlags;

namespace Sheepy.PhoenixPt.LimitedWar {

   public class ModSettings {
      public bool Faction_Attack_Zone = true;
      public bool Pandora_Attack_Zone = false;
      public bool Attack_Raise_Alertness = true;
      public bool Attack_Raise_Faction_Alertness = true;

      public bool Stop_OneSided_War = true;
      public int No_Attack_When_Sieged_Difficulty = 2;
      public int One_Global_Attack_Difficulty = 0;
      public int One_Attack_Per_Faction_Difficulty = 1;

      internal bool Has_Less_Attack => Stop_OneSided_War ||
              No_Attack_When_Sieged_Difficulty >= 0 || One_Global_Attack_Difficulty >= 0 || One_Attack_Per_Faction_Difficulty >= 0;

      public DefenseMultiplier Defense_Multiplier = new DefenseMultiplier();
   }

   public class DefenseMultiplier {
      public float Default = 1;
      public float Alert    = 1.2f;
      public float High_Alert = 1.1f;
      public float Attacker_Pandora = 1.2f;
      public float Attacker_Anu = 1;
      public float Attacker_NewJericho = 1f;
      public float Attacker_Synedrion = 1;
      public float Defender_Pandora = 1;
      public float Defender_Anu = 1.2f;
      public float Defender_NewJericho = 1;
      public float Defender_Synedrion = 1.2f;

      internal bool IsEmpty => Default == 1 && Alert == 1 && High_Alert == 1
         && Attacker_Pandora == 1 && Attacker_Anu == 1 && Attacker_NewJericho == 1 && Attacker_Synedrion == 1
         && Defender_Pandora == 1 && Defender_Anu == 1 && Defender_NewJericho == 1 && Defender_Synedrion == 1;
   }

   public class Mod : ZyAdvMod {
      private static ModSettings Settings;

      public static void Init () => new Mod().MainMod();

      public void MainMod ( ModSettings settings = null, Action< SourceLevels, object, object[] > logger = null ) {
         SetLogger( logger );
         Settings = settings = ReadSettings( settings );
         Info( "Setting = {0}", (Func<string>) JsonifySettings );

         if ( settings.Faction_Attack_Zone || settings.Pandora_Attack_Zone ) {
            if ( BatchPatch( "Zoned attacks", () => {
               Patch( typeof( GeoHavenDefenseMission ), "UpdateGeoscapeMissionState", nameof( BeforeGeoMission_StoreMission ), nameof( AfterGeoMission_Cleanup ) );
               Patch( typeof( GeoSite ), "DestroySite", nameof( Override_DestroySite ) );
            } ) != null ) {
               TryPatch( typeof( GeoscapeLog ), "Map_SiteMissionStarted", postfix: nameof( AfterSiteMission_AmendLog ) );
               TryPatch( typeof( GeoscapeLog ), "Map_SiteMissionEnded", postfix: nameof( AfterSiteMission_AmendLog ) );
            }
         }

         if ( settings.Attack_Raise_Alertness || settings.Attack_Raise_Faction_Alertness )
            TryPatch( typeof( GeoSite ), "DestroySite", postfix: nameof( AfterDestroySite_RaiseAlertness ) );

         if ( settings.Defense_Multiplier?.IsEmpty == false )
            TryPatch( typeof( GeoHavenDefenseMission ), "GetDefenseDeployment", postfix: nameof( AfterDefDeploy_BoostDef ) );

         if ( settings.Has_Less_Attack ) {
            if ( TryPatch( typeof( GeoFaction ), "AttackHavenFromVehicle", nameof( Override_Attack ) ) != null )
               BatchPatch( "Less attacks", () => {
                  Patch( typeof( GeoLevelController ), "OnLevelStart", postfix: nameof( AfterLevelStart_StoreDiff ) );
                  Patch( typeof( VehicleFactionController ), "UpdateNavigation", nameof( BeforeNav_Drop_Attack ), nameof( AfterNav_Restore ) );
               } );
         }
      }

      private static string JsonifySettings () => JsonConvert.SerializeObject( Settings );

      private static bool IsAlien ( IGeoFactionMissionParticipant f ) => f is GeoAlienFaction;
      private static bool IsPhoenixPt ( IGeoFactionMissionParticipant f ) => f is GeoPhoenixFaction;
      private static bool IsAlienOrPP ( IGeoFactionMissionParticipant f ) => IsAlien( f ) || IsPhoenixPt( f );

      #region Parallel_Attack
      private static int difficulty = 1; // Default to Vet if anything goes wrong
      private static IGeoFactionMissionParticipant lastAttacker = null;

      private static void AfterLevelStart_StoreDiff ( GeoLevelController __instance ) { try {
         GameDifficultyLevelDef diff = __instance.CurrentDifficultyLevel;
         difficulty = __instance.DynamicDifficultySystem.DifficultyLevels.ToList().IndexOf( diff );
         lastAttacker = null;
         Info( "Attacker reset. Difficulty level is {0}.", difficulty ); // 0 = Rookie, 1 = Veteran, 2 = Hero, 3 = Legend
      } catch ( Exception ex ) { Error( ex ); } }

      private static bool AtMaxFight ( GeoLevelController geoLevel, IGeoFactionMissionParticipant attacker ) { try {
         if ( geoLevel?.Map == null || IsAlienOrPP( attacker ) ) return false;
         foreach ( GeoSite site in geoLevel.Map.AllSites ) {
            if ( ! ( site.ActiveMission is GeoHavenDefenseMission mission ) ) continue;
            if ( Settings.No_Attack_When_Sieged_Difficulty >= difficulty && IsAlien( mission.GetEnemyFaction() ) && site.Owner == attacker ) {
               Verbo( "{0} is being sieged by alien at {0}.", site.Name );
               return true;
            }
            if ( Settings.One_Global_Attack_Difficulty >= difficulty ) {
               Verbo( "Global attack limit: {0} already under siege.", site.Name );
               return true; // Rokkie and Veteran limits to 1 faction war on globe
            }
            if ( Settings.One_Attack_Per_Faction_Difficulty >= difficulty && mission.GetEnemyFaction() == attacker ) {
               Verbo( "Faction attack limit: {0} already sieging {1}.", attacker.GetPPName(), site.Name );
               return true; // Hero and Legend limits to 1 attack per faction
            }
         }
         return false;
      } catch ( Exception ex ) { return ! Error( ex ); } }

      private static bool ShouldStopFight ( GeoLevelController geoLevel, IGeoFactionMissionParticipant from ) { try {
         if ( Settings.Stop_OneSided_War && lastAttacker != null && from == lastAttacker ) {
            Verbo( "One sided war; {0} has already attacked.", from.GetPPName() );
            return true;
         }
         return AtMaxFight( geoLevel, from );
      } catch ( Exception ex ) { return ! Error( ex ); } }

      [ HarmonyPriority( Priority.Low ) ]
      private static void BeforeNav_Drop_Attack ( VehicleFactionController __instance, ref float? __state ) { try {
         if ( ShouldStopFight( __instance.Vehicle?.GeoLevel, __instance.Vehicle?.Owner ) ) {
            Verbo( "Discouraging {0} from faction war.", __instance.Vehicle.Name );
            __state = __instance.ControllerDef.FactionInWarWeightMultiplier;
            __instance.ControllerDef.FactionInWarWeightMultiplier = -2f;
         } else {
            __state = null;
         }
      } catch ( Exception ex ) { Error( ex ); } }

      [ HarmonyPriority( Priority.High ) ]
      private static void AfterNav_Restore ( VehicleFactionController __instance, float? __state ) {
         if ( __state != null ) __instance.ControllerDef.FactionInWarWeightMultiplier = __state.Value;
      }

      [ HarmonyPriority( Priority.High ) ]
      private static bool Override_Attack ( GeoFaction __instance, GeoVehicle vehicle, GeoSite site, GeoLevelController ____level ) { try {
         Verbo( "{0} wants to attack {1}.", vehicle.Name, site.Name );
         if ( ShouldStopFight( ____level, vehicle?.Owner ) ) {
            Info( "{0} attack prevented at {2}.", __instance.Name.Localize(), vehicle.Name, site.Name );
            return false;
         }
         lastAttacker = vehicle.Owner;
         return true;
      } catch ( Exception ex ) { return Error( ex ); } }
      #endregion

      #region Alertness
      private static void AfterDestroySite_RaiseAlertness ( GeoSite __instance ) { try {
         var haven = DefenseMission?.Haven;
         var owner = haven?.Site?.Owner;
         if ( haven == null || IsAlienOrPP( owner ) ) return;
         if ( Settings.Attack_Raise_Faction_Alertness && owner != null ) {
            Info( "{0} has loss. Raising alertness of {1}", haven.Site.Name, owner.GetPPName() );
            foreach ( var e in owner.Havens )
               typeof( GeoHaven ).GetMethod( "IncreaseAlertness", NonPublic | Instance ).Invoke( e, null );
         } else if ( haven.Site?.State == GeoSiteState.Functioning ) {
            typeof( GeoHaven ).GetMethod( "IncreaseAlertness", NonPublic | Instance ).Invoke( haven, null );
            Info( "{0} has loss. It is now on {1}", haven.Site.Name, haven.AlertLevel );
         }
      } catch ( Exception ex ) { Error( ex ); } }
      #endregion

      #region Attack Zone
      private static GeoHavenDefenseMission DefenseMission = null;

      [ HarmonyPriority( Priority.VeryHigh ) ]
      private static void BeforeGeoMission_StoreMission  ( GeoHavenDefenseMission __instance ) { DefenseMission = __instance; }
      private static void AfterGeoMission_Cleanup () { DefenseMission = null; }

      private static bool CauseZoneDamage ( IGeoFactionMissionParticipant attacker ) {
         return ! IsPhoenixPt( attacker ) &&
              ( Settings.Pandora_Attack_Zone && IsAlien( attacker ) ) ||
              ( Settings.Faction_Attack_Zone && ! IsAlien( attacker ) );
      }

      [ HarmonyPriority( Priority.High ) ]
      private static bool Override_DestroySite ( GeoSite __instance ) { try {
         if ( DefenseMission == null ) return true;
         var attacker = DefenseMission.GetEnemyFaction();
         Verbo( "{0} loss to {1}.", __instance.Name, attacker.GetPPName() );
         if ( ! CauseZoneDamage( attacker ) ) return true;
         GeoHavenZone zone = DefenseMission.AttackedZone;
         zone.AddDamage( zone.Health.IntValue );
         zone.AddProduction( 0 );
         var haven = zone.Haven;
         Info( "Fall of {0} converted to {1} destruction.", __instance.Name, zone.Def.ViewElementDef.DisplayName1.LocalizeEnglish() );
         if ( haven != null ) {
            if ( ( zone.Def.ProvidesRecruitment || zone.Def.ProvidesEliteRecruitment ) && haven.AvailableRecruit != null )
               haven.RemoveRecruit();
         }
         haven.ZonesStats.UpdateZonesStats();
         __instance.RefreshVisuals();
         return false;
      } catch ( Exception ex ) { return Error( ex ); } }

      private static void AfterSiteMission_AmendLog ( GeoSite site, GeoMission mission, List<GeoscapeLogEntry> ____entries ) { try {
         if ( ! ( mission is GeoHavenDefenseMission defense ) || DefenseMission == null  ) return;
         var attacker = DefenseMission.GetEnemyFaction();
         if ( ! CauseZoneDamage( attacker ) ) return;
         LocalizedTextBind zoneName = defense.AttackedZone?.Def?.ViewElementDef?.DisplayName1;
         if ( zoneName == null || ____entries == null || ____entries.Count < 1 ) return;
         GeoscapeLogEntry entry = ____entries[ ____entries.Count - 1 ];
         Verbo( "Converting {0} invasion message to zone invasion.", attacker.GetPPName() );
         entry.Parameters[0] = new LocalizedTextBind( site.SiteName.Localize() + " " + TitleCase( zoneName.Localize() ), true );
      } catch ( Exception ex ) { Error( ex ); } }
      #endregion

      #region Defense Boost
      [ HarmonyPriority( Priority.High ) ]
      private static void AfterDefDeploy_BoostDef ( GeoHavenDefenseMission __instance, ref int __result, GeoHaven haven ) { try {
         if ( haven == null ) return;
         GeoFaction attacker = __instance.GetEnemyFaction() is GeoSubFaction sub
               ? attacker = sub.BaseFaction : __instance.GetEnemyFaction() as GeoFaction;
         var defender = haven.Site.Owner;
         Info( "{0} of {1} under attack by {2}, defense strength {3}. Alert level {4}.", haven.Site.Name, defender, attacker.GetPPName(), __result, haven.AlertLevel );

         var conf = Settings.Defense_Multiplier;
         float multiply = conf.Default;
         switch ( haven?.AlertLevel ) {
            case GeoHaven.HavenAlertLevel.Alert : multiply *= conf.Alert; break;
            case GeoHaven.HavenAlertLevel.HighAlert : multiply *= conf.High_Alert; break;
         }

         var geoLevel = haven.Site.GeoLevel;
         if ( IsAlien( attacker ) )
            multiply *= conf.Attacker_Pandora;
         else if ( attacker == geoLevel.AnuFaction )
            multiply *= conf.Attacker_Anu;
         else if ( attacker == geoLevel.SynedrionFaction )
            multiply *= conf.Attacker_Synedrion;
         else if ( attacker == geoLevel.NewJerichoFaction )
            multiply *= conf.Attacker_NewJericho;

         if ( IsAlien( defender ) )
            multiply *= conf.Defender_Pandora;
         else if ( defender == geoLevel.AnuFaction )
            multiply *= conf.Defender_Anu;
         else if ( defender == geoLevel.SynedrionFaction )
            multiply *= conf.Defender_Synedrion;
         else if ( defender == geoLevel.NewJerichoFaction )
            multiply *= conf.Defender_NewJericho;

         if ( multiply != 1 ) {
            __result = (int) Math.Round( __result * multiply );
            Info( "Multiplying defender strength by {0} to {1}", multiply, __result );
         }
      } catch ( Exception ex ) { Error( ex ); } }
      #endregion
   }
}