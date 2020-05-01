using System;
using PhoenixPoint.Tactical.Entities;

namespace Sheepy.PhoenixPt.MitigateShred {

   internal class ModConfig {
      public float Convert_Ratio = 0.1f;
      public float Min_Shred = 1f;
   }

   public class Mod : ZyMod {
      internal static ModConfig Config;

      public static void Init () => new Mod().MainMod();

      public void MainMod ( Func< string, object, object > api = null ) {
         SetApi( api, out Config );
         Patch( typeof( DamageAccumulation ), "GenerateStandardDamageTargetData", null, "AfterStandardDamage_Shred" );
      }

      public static void AfterStandardDamage_Shred ( DamageAccumulation.TargetData __result ) { try {
         float mitigated = __result.DamageResult.ArmorMitigatedDamage;
         if ( mitigated > 0 )
            __result.DamageResult.ArmorDamage += (float) Math.Max( Config.Min_Shred, Math.Round( mitigated * Config.Convert_Ratio ) );
      } catch ( Exception ex ) { Error( ex ); } }
   }
}