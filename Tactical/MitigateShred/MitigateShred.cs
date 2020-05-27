using System;
using System.Diagnostics;
using System.Linq;
using Base.Core;
using Base.Defs;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;

namespace Sheepy.PhoenixPt.MitigateShred {

   internal class ModConfig {
      public float Convert_Ratio = 0.1f;
      public float Min_Shred = 1f;
   }

   public class Mod : ZyMod {
      internal static ModConfig Config;

      public static void Init () => new Mod().TacticalMod();

      public void MainMod ( Func< string, object, object > api ) => TacticalMod( api );

      public void TacticalMod ( Func< string, object, object > api = null ) {
         SetApi( api, out Config );
         Patch( typeof( DamageAccumulation ), "GenerateStandardDamageTargetData", null, "AfterStandardDamage_Shred" );
         //Patch( typeof( DamageAccumulation ), "GenerateStandardDamageTargetData", null, "AfterStandardDamage_Shred" );
      }

      private static PiercingDamageKeywordDataDef ShredDef;

      public static void AfterStandardDamage_Shred ( DamageAccumulation __instance, DamageAccumulation.TargetData __result ) { try {
         var damage = __result.DamageResult;
         float mitigated = damage.ArmorMitigatedDamage;
         float shred = mitigated > 0 ? (float) Math.Max( Config.Min_Shred, Math.Round( mitigated * Config.Convert_Ratio ) ) : 0f;
         var shredDef = ShredDef ?? ( ShredDef = GameUtl.GameComponent<DefRepository>().GetAllDefs<PiercingDamageKeywordDataDef>().FirstOrDefault() );
         if ( shredDef == null ) {
               Warn( "Cannot find PiercingDamageKeywordDataDef" );
               return;
            }
         var effect = __instance.DamageKeywords.FirstOrDefault( e => e.DamageKeywordDef == ShredDef );
         if ( shred > 0 ) {
            damage.ArmorDamage += shred;
            if ( effect != null ) {
               Verbo( "Adding shred {0} to {1}", shred, effect.Value );
               effect.Value += shred;
            } else {
               Verbo( "Creating shred {0}", shred );
               __instance.DamageKeywords.Add( new DamageKeywordPair(){ DamageKeywordDef = shredDef, Value = shred } );
            }
            damage.ArmorDamage += shred;
         }
         /* // Called on every frame during damage preview!
         Log( shred > 0 ? TraceEventType.Information : TraceEventType.Verbose,
            "Mitigate Shred Damage: Raw {0}, HP {1}, Mitigated {2}, Shred {3}+{4}",
            __instance.Amount, damage.HealthDamage, damage.ArmorMitigatedDamage, damage.ArmorDamage, shred );
            */
      } catch ( Exception ex ) { Error( ex ); } }
   }
}