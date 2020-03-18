using System;
using System.Reflection;

namespace Sheepy.PhoenixPt_LessWar {
   using Harmony;
   using PhoenixPoint.Common.Core;
   using PhoenixPoint.Geoscape.Entities;
   using PhoenixPoint.Geoscape.Levels;
   using System.Linq;
   using static System.Reflection.BindingFlags;

   public class Mod {
      //private static Logger Log;

      public static void Init () {
         //Log = new Logger( "Mods/SheepyMods.log" ){ Prefix = typeof( Mod ).Namespace + " " };
         HarmonyInstance harmony = HarmonyInstance.Create( typeof( Mod ).Namespace );

         Patch( harmony, typeof( GeoLevelController ), "OnLevelStart", null, "GetDifficulty" );
         Patch( harmony, typeof( VehicleFactionController ), "UpdateNavigation", "Prefix_Navigate", "Postfix_Navigate" );
         Patch( harmony, typeof( GeoFaction ), "AttackHavenFromVehicle", "Override_Attack" );
         //Patch( harmony, typeof( GeoSite ), "CreateHavenDefenseMission", "Stacktrace" );
         //Log.Info( "Init" );
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

      //public static void Stacktrace () { Log.Info( Logger.Stacktrace ); }

      private static int difficulty = 1; // Default to Vet if anything goes wrong
      private static GeoFaction lastAttacker = null;

      public static void GetDifficulty ( GeoLevelController __instance ) { try {
         GameDifficultyLevelDef diff = __instance.CurrentDifficultyLevel;
         difficulty = __instance.DynamicDifficultySystem.DifficultyLevels.ToList().IndexOf( diff );
         lastAttacker = null;
         //Log.Info( "Attacker reset. Difficult is {0}", difficulty );
         // 0 = Rookie, 1 = Veteran, 2 = Hero, 3 = Legend
      } catch ( Exception ) { difficulty = 1; } }

      private static bool AtMaxFight ( GeoLevelController geoLevel, GeoFaction faction ) { try {
         if ( geoLevel == null || geoLevel.Map == null ) return false;
         if ( faction == geoLevel.AlienFaction || faction == geoLevel.PhoenixFaction ) return false;
         foreach ( GeoSite site in geoLevel.Map.AllSites ) {
            if ( ! ( site.ActiveMission is GeoHavenDefenseMission mission ) ) continue;
            if ( mission.GetEnemyFaction() == geoLevel.AlienFaction ) {
               if ( site.Owner == faction ) {
                  //Log.Info( "{0} is being sieged by alian", site.Name );
                  return true;
               }
            }
            if ( difficulty < 2 ) {
               //Log.Info( "{0} already under siege", site.Name );
               return true; // Rokkie and Veteran limits to 1 faction war on globe
            }
            if ( mission.GetEnemyFaction() == faction ) {
               //Log.Info( "{0} already sieging {1}", faction.Name.Localize(), site.Name );
               return true; // Hero and Legend limits to 1 attack per faction
            }
         }
         return false;
      } catch ( Exception ex ) { /* Log.Error( ex ); */ return false; } }

      private static bool ShouldStopFight ( GeoLevelController geoLevel, GeoFaction from, GeoFaction to = null ) {
         if ( lastAttacker != null ) {
            if ( from == lastAttacker ) {
               //Log.Info( "{0} has already attacked", from.Name.Localize() );
               return true;
            }
         } /* else {
            Dictionary<GeoFaction, int> lostHaven = new Dictionary<GeoFaction, int>();
            foreach ( GeoSite site in geoLevel.Map.AllSites ) {
               if ( site.State != GeoSiteState.Destroyed ) continue;
               GeoFaction owner = site.Owner;
               if ( owner == geoLevel.AnuFaction || owner == geoLevel.NewJerichoFaction || owner == geoLevel.SynedrionFaction ) {
                  if ( lostHaven.ContainsKey( owner ) ) ++lostHaven[ owner ];
                  else lostHaven[ owner ] = 1;
               }
            }
            int MaxLost = lostHaven.Values.Max(), MinLost = lostHaven.Values.Min();
            if ( MaxLost != MinLost ) {
               // Avoid attacking the faction with most lost havens
               if ( to != null && lostHaven[ to ] == MaxLost ) return false;
            }
         } */
         return AtMaxFight( geoLevel, from );
      }

      [ HarmonyPriority( Priority.Low ) ]
      public static void Prefix_Navigate ( VehicleFactionController __instance, ref float? __state ) {
         if ( ShouldStopFight( __instance.Vehicle?.GeoLevel, __instance.Vehicle?.Owner ) ) {
            __state = __instance.ControllerDef.FactionInWarWeightMultiplier;
            __instance.ControllerDef.FactionInWarWeightMultiplier = -2f;

         } else {
            __state = null;
         }
      }

      [ HarmonyPriority( Priority.High ) ]
      public static void Postfix_Navigate ( VehicleFactionController __instance, float? __state ) {
         if ( __state != null ) __instance.ControllerDef.FactionInWarWeightMultiplier = __state.Value;
      }

      [ HarmonyPriority( Priority.High ) ]
      public static bool Override_Attack ( GeoFaction __instance, GeoVehicle vehicle, GeoSite site, GeoLevelController ____level ) { try {
         //Log.Info( "{0} wants to attack {1}", vehicle.Name, site.Name );
         if ( ShouldStopFight( ____level, vehicle?.Owner, site?.Owner ) ) {
            //Log.Info( "{0} attack prevented at {2}", __instance.Name.Localize(), vehicle.Name, site.Name );
            return false;
         }
         lastAttacker = vehicle.Owner;
         return true;
      } catch ( Exception ex ) { /* Log.Error( ex ); */ return true; } }
   }
}