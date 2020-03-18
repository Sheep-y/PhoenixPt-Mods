using System;
using System.Linq;
using System.Reflection;

namespace Sheepy.PhoenixPt_LimitWarToZone {
   using Harmony;
   using PhoenixPoint.Geoscape.Entities;
   using static System.Reflection.BindingFlags;
   using PhoenixPoint.Geoscape.Entities.Sites;
   using PhoenixPoint.Geoscape.Levels;
   using System.Collections.Generic;
   using Base.UI;

   public class Mod {
      //private static Logger Log;

      public static void Init () {
         //Log = new Logger( "Mods/SheepyMods.log" ){ Prefix = typeof( Mod ).Namespace + " " };
         HarmonyInstance harmony = HarmonyInstance.Create( typeof( Mod ).Namespace );

         Patch( harmony, typeof( GeoHavenDefenseMission ), "UpdateGeoscapeMissionState", "Prefix_UpdateSite", "Postfix_UpdateSite" );
         Patch( harmony, typeof( GeoSite ), "DestroySite", "Override_DestroySite" );
         Patch( harmony, typeof( GeoscapeLog ), "Map_SiteMissionStarted", null, "Postfix_SiteMission" );
         Patch( harmony, typeof( GeoscapeLog ), "Map_SiteMissionEnded", null, "Postfix_SiteMission" );
         //Patch( harmony, typeof( GeoFaction ), "AttackHavenFromVehicle", "Override_Attack" );
      }

      #region Harmony helpers
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
      #endregion

      private static GeoHavenDefenseMission DefenseMission = null;

      [ HarmonyPriority( Priority.VeryHigh ) ]
      public static void Prefix_UpdateSite  ( GeoHavenDefenseMission __instance ) { DefenseMission = __instance; }
      public static void Postfix_UpdateSite () { DefenseMission = null; }

      [ HarmonyPriority( Priority.High ) ]
      public static bool Override_DestroySite ( GeoSite __instance ) { try {
         if ( DefenseMission == null ) return true;
         if ( DefenseMission.GetEnemyFaction() == __instance.GeoLevel.AlienFaction ) return true;
         //if ( DefenseMission.Status != GeoscapeMissionStatus.AttackersWon ) return true; // Always true here
         GeoHavenZone zone = DefenseMission.AttackedZone;
         //Log.Info( "Convert to {0} destruction ({1})", zone.Def.ResourcePath, zone.Health.IntValue );
         zone.AddDamage( zone.Health.IntValue );
         typeof( GeoHaven ).GetMethod( "IncreaseAlertness", NonPublic | Instance ).Invoke( DefenseMission.Haven, null );
         //Log.Info( "Zone is now {0} and on {1}", zone.State, DefenseMission.Haven.AlertLevel );
         return false;
      } catch ( Exception ex ) { /* Log.Error( ex ); */ return true; } }

      public static void Postfix_SiteMission ( GeoSite site, GeoMission mission, GeoscapeLogMessagesDef ____messagesDef, List<GeoscapeLogEntry> ____entries ) {
         if ( ! ( mission is GeoHavenDefenseMission defense ) ) return;
         if ( defense.GetEnemyFaction() == site.GeoLevel.AlienFaction ) return;

         LocalizedTextBind zoneName = defense.AttackedZone?.Def?.ViewElementDef?.DisplayName1;
         if ( zoneName == null || ____entries == null || ____entries.Count < 1 ) return;

         GeoscapeLogEntry entry = ____entries[ ____entries.Count - 1 ];
         entry.Parameters[0] = new LocalizedTextBind( site.SiteName.Localize() + " " + TitleCase( zoneName.Localize() ), true );
      }

      private static string TitleCase ( string txt ) {
         return txt.Split( new char[]{ ' ' } ).Join( e => e.Substring(0,1).ToUpper() + e.Substring(1).ToLower(), " " );
      }
   }
}