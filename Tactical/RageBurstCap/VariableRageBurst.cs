using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sheepy.PhoenixPt.VariableRageBurst {

   internal class ModConfig {
      public Dictionary<float,int> AP_to_Rage_Shots = new Dictionary<float, int>{
         { 0, 20 }, { 1, 12 }, { 2, 6 }, { 3, 4 }
      };
      public uint Config_Version = 20200702;
   }

   public class Mod : ZyMod {
      private static ModConfig Config;

      public static void Init () => new Mod().MainMod();
      public void MainMod ( Func<string,object,object> api = null ) => TacticalMod( api );

      public void TacticalMod ( Func<string,object,object> api ) {
         SetApi( api, out Config );
         Patch( typeof( Weapon ), "GetNumberOfShots", postfix: nameof( AfterGetNumberOfShots_Cap ) );
      }

      public static void AfterGetNumberOfShots_Cap ( Weapon __instance, ref int __result, AttackType attackType, int executionsCount = 1 ) { try {
         if ( attackType != AttackType.RageBurst ) return;
         var list = Config.AP_to_Rage_Shots;
         if ( list == null || list.Count == 0 ) return;
         var weaponAp = __instance.WeaponDef.APToUsePerc;
         var closestDiff = float.PositiveInfinity;
         var shot = 5;
         foreach ( var ap in list.Keys ) {
            var diff = Mathf.Abs( ap * 25 - weaponAp );
            if ( diff >= closestDiff ) continue;
            closestDiff = diff;
            shot = Mathf.Max( 0, list[ ap ] );
         }
         __result = Math.Max( 0, __instance.AutoFireShotsCount * executionsCount * shot );
      } catch ( Exception ex ) { Api( "log e", ex ); } }
   }
}