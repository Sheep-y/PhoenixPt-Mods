using System;
using System.Reflection;

namespace Sheepy.PhoenixPt_BetterDefence {
   using Harmony;
   using PhoenixPoint.Geoscape.Entities;
   using static System.Reflection.BindingFlags;
   using PhoenixPoint.Geoscape.Levels;

   public class Mod {
      //private static Logger Log;

      public static void Init () {
         //Log = new Logger( "Mods/SheepyMods.log" ){ Prefix = typeof( Mod ).Namespace + " " };
         HarmonyInstance harmony = HarmonyInstance.Create( typeof( Mod ).Namespace );
         Patch( harmony, typeof( GeoHavenDefenseMission ), "GetDefenseDeployment", null, "Postfix_GetDeployment" );
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

      [ HarmonyPriority( Priority.High ) ]
      public static void Postfix_GetDeployment ( GeoHavenDefenseMission __instance, ref int __result, GeoHaven haven ) {
         //Log.Info( "{0} deploy {1} vs {2}x{3}", haven.Site.Name, __result, __instance.AttackerDeployment, haven.Site.Owner.FactionStatModifiers.HavenAttackerStrengthModifier );
         float deploy = __result;
         switch ( haven?.AlertLevel ) {
            case GeoHaven.HavenAlertLevel.Alert : deploy *= 1.2f; break;
            case GeoHaven.HavenAlertLevel.HighAlert : deploy *= 1.3f; break;
         }

         GeoFaction attacker = __instance?.GetEnemyFaction();
         GeoLevelController geoLevel = haven?.Site?.GeoLevel;
         if ( attacker == geoLevel.AnuFaction || attacker == geoLevel.SynedrionFaction ) {
            deploy *= 1.5f;
         } else if ( attacker == geoLevel.NewJerichoFaction ) {
            deploy *= 2f;
         }

         __result = (int) Math.Round( deploy );
         //Log.Info( "Bumping defender to {0}", __result );
      }
   }
}