using System;
using System.Reflection;

namespace Sheepy.PhoenixPt_NoWar {
   using Harmony;
   using PhoenixPoint.Geoscape.Entities;
   using PhoenixPoint.Geoscape.Levels;
   using static System.Reflection.BindingFlags;

   public class Mod {
      //private static Logger Log;

      public static void Init () {
         //Log = new Logger( "Mods/SheepyMods.log" ){ Prefix = typeof( Mod ).Namespace + " " };
         HarmonyInstance harmony = HarmonyInstance.Create( typeof( Mod ).Namespace );

         Patch( harmony, typeof( VehicleFactionController ), "UpdateNavigation", "Prefix_Navigate" );
         Patch( harmony, typeof( GeoFaction ), "AttackHavenFromVehicle", "Override_Attack" );
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

      [ HarmonyPriority( Priority.Low ) ]
      public static void Prefix_Navigate ( VehicleFactionController __instance ) {
         __instance.ControllerDef.FactionInWarWeightMultiplier = -2f;
      }

      [ HarmonyPriority( Priority.High ) ]
      public static bool Override_Attack ( GeoVehicle vehicle ) {
         return vehicle?.Owner == vehicle.GeoLevel.AlienFaction || vehicle?.Owner == vehicle.GeoLevel.PhoenixFaction;
      }
   }
}