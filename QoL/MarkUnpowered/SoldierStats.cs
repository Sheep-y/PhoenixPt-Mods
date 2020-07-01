using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.View.ViewStates;
using System;
using System.Reflection;

namespace Sheepy.PhoenixPt.GeoTweaks {

   internal class SoldierStats : ZyMod {

      internal void Init () {
         Patch( typeof( UIStateEquipSoldier ), "DisplaySoldier", postfix: nameof( AfterDisplaySoldier_AddStats ) );
      }

      private static void AfterDisplaySoldier_AddStats ( UIStateEquipSoldier __instance, GeoCharacter character ) { try {
         var equip = typeof( UIStateEquipSoldier ).GetProperty( "_soldierEquipModule", BindingFlags.NonPublic | BindingFlags.Instance ).GetValue( __instance ) as UIModuleSoldierEquip;
         var stats = character.CharacterStats;
         equip.WeightLimitTextComp.text += "Speed " + stats.Speed + ", Accuracy " + stats.Accuracy + ", Perception " + stats.Perception + ", Stealth " + stats.Stealth;
      } catch ( Exception ex ) { Error( ex ); } }
   }
}