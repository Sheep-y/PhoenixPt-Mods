using System;
using System.Reflection;

namespace Sheepy.PhoenixPt_RageBurstCap {
   using Harmony;
   using PhoenixPoint.Tactical.Entities.Abilities;
   using PhoenixPoint.Tactical.Entities.Weapons;
   using static System.Reflection.BindingFlags;

   public class Mod {
      //private static Logging.Logger Log = new Logging.Logger( "Mods/SheepyMods.log" );

      public static void Init () {
         HarmonyInstance harmony = HarmonyInstance.Create( typeof( Mod ).Namespace );

         Patch( harmony, typeof( Weapon ), "GetNumberOfShots", null, "AfterGetNumberOfShots_Cap" );
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
      #endregion

      public static void AfterGetNumberOfShots_Cap ( Weapon __instance, ref int __result, AttackType attackType, int executionsCount = 1 ) { try {
         if ( attackType != AttackType.RageBurst ) return;
         __result = Math.Min( __result, Math.Max( 0, __instance.AutoFireShotsCount * executionsCount * 300 / __instance.WeaponDef.APToUsePerc ) );
      } catch ( Exception ex ) { LogError( ex ); } }
   }
}