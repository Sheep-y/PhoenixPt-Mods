using System;
using System.Reflection;

namespace Sheepy.PhoenixPt_MitigateShred {
   using Harmony;
   using PhoenixPoint.Tactical.Entities;
   using static System.Reflection.BindingFlags;

   public class Mod {
      //private static Logging.Logger Log = new Logging.Logger( "Mods/SheepyMods.log" );

      public static void Init () {
         HarmonyInstance harmony = HarmonyInstance.Create( typeof( Mod ).Namespace );

         Patch( harmony, typeof( DamageAccumulation ), "GenerateStandardDamageTargetData", null, "AfterStandardDamage_Shred" );
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

      public static void AfterStandardDamage_Shred ( DamageAccumulation.TargetData __result ) { try {
         float mitigated = __result.DamageResult.ArmorMitigatedDamage;
         if ( mitigated > 0 )
            __result.DamageResult.ArmorDamage += (float) Math.Max( 1, Math.Round( mitigated / 10 ) );
      } catch ( Exception ex ) { LogError( ex ); } }
   }
}