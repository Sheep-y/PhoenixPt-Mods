using System;
using System.Reflection;

namespace Sheepy.PhoenixPt_CentreOnNewBase  {
   using Harmony;
   using PhoenixPoint.Geoscape.Entities;
   using PhoenixPoint.Geoscape.Levels;
   using PhoenixPoint.Geoscape.Levels.Factions;
   using static System.Reflection.BindingFlags;

   public class Mod {
      //private static Logging.Logger Log = new Logging.Logger( "Mods/SheepyMods.log" ){ Prefix = "CentreOnNewBase " };

      public static void Init () {
         HarmonyInstance harmony = HarmonyInstance.Create( typeof( Mod ).Namespace );

         Patch( harmony, typeof( GeoPhoenixFaction ), "ActivatePhoenixBase", null, "AfterActivatePhoenixBase_Center" );
         Patch( harmony, typeof( GeoFaction ), "OnVehicleSiteExplored", null, "AfterExplore_Center" );
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
         //Log.Error( ex );
         return true;
      }

      private static string TitleCase ( string txt ) {
         return txt.Split( new char[]{ ' ' } ).Join( e => e.Substring(0,1).ToUpper() + e.Substring(1).ToLower(), " " );
      }
      #endregion

      public static void AfterActivatePhoenixBase_Center ( GeoSite site, GeoLevelController ____level ) { try {
         ____level.View.ChaseTarget( site, false );
      } catch ( Exception ex ) { LogError( ex ); } }

      public static void AfterExplore_Center ( GeoFaction __instance, GeoVehicle vehicle, GeoLevelController ____level ) { try {
         if ( __instance != ____level.PhoenixFaction ) return;
         ____level.View.ChaseTarget( vehicle.CurrentSite, false );
      } catch ( Exception ex ) { LogError( ex ); } }
   }
}