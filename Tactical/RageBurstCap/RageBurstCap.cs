using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;

namespace Sheepy.PhoenixPt.RageBurstCap {

   internal class ModConfig {
      public uint Config_Version = 20200702;
   }

   public class Mod : ZyMod {
      public static void Init () => new Mod().MainMod();
      public void MainMod ( Func<string,object,object> api = null ) => TacticalMod( api );

      public void TacticalMod ( Func<string,object,object> api ) {
         SetApi( api );
         Patch( typeof( Weapon ), "GetNumberOfShots", null, "AfterGetNumberOfShots_Cap" );
      }

      public static void AfterGetNumberOfShots_Cap ( Weapon __instance, ref int __result, AttackType attackType, int executionsCount = 1 ) { try {
         if ( attackType != AttackType.RageBurst ) return;
         __result = Math.Min( __result, Math.Max( 0, __instance.AutoFireShotsCount * executionsCount * 300 / __instance.WeaponDef.APToUsePerc ) );
      } catch ( Exception ex ) { Api( "log e", ex ); } }
   }
}