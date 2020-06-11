using System;
using PhoenixPoint.Tactical.Entities;
using UnityEngine;

namespace Sheepy.PhoenixPt.MitigateShred {

   internal class ModConfig {
      public float Convert_Ratio = 0.1f;
      public float Min_Shred = 1f;
      public uint Config_Version = 20200530;
   }

   public class Mod : ZyMod {
      internal static ModConfig Config;

      public static void Init () => new Mod().TacticalMod();

      public void MainMod ( Func< string, object, object > api ) => TacticalMod( api );

      public void TacticalMod ( Func< string, object, object > api = null ) {
         SetApi( api, out Config );
         Patch( typeof( DamageAccumulation ), "GenerateStandardDamageTargetData", postfix: nameof( AfterDamage_AddShred ) );
      }

      public static void AfterDamage_AddShred ( DamageAccumulation __instance, DamageAccumulation.TargetData __result ) { try {
         var damage = __result.DamageResult;
         var mitigated = damage.ArmorMitigatedDamage;
         if ( mitigated <= 0 ) return;
         var shred = Mathf.Max( Config.Min_Shred, Mathf.Round( mitigated * Config.Convert_Ratio ) );
         var orig = damage.ArmorDamage;
         //Trace( "Shred {0}+{1} = {2}", orig, shred, orig + shred ); // Triggered by damage preview. Pretty hot code.
         damage.ArmorDamage += shred;
         __instance.ArmorShred += shred;
      } catch ( Exception ex ) { Api( "log e", ex ); } }
   }
}