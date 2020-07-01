using Base.Entities.Statuses;
using Base.UI;
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
         var cls = character?.ClassDef;
         if ( cls == null || cls.IsVehicle || cls.IsMutog || character.Progression == null ) return;
         var TextComp = ( typeof( UIStateEquipSoldier ).GetProperty( "_soldierEquipModule", BindingFlags.NonPublic | BindingFlags.Instance )?.GetValue( __instance ) as UIModuleSoldierEquip )?.WeightLimitTextComp;

         var stats = character.CharacterStats;
         var txt = Mod.Config.Equip_Weight_Replacement;
         // "{encumbrance}, {speed_txt} {speed}, {accuracy_txt} {accuracy}, {perception_txt} {perception}, {stealth_txt} {stealth}";
         txt = txt.Replace( "{encumbrance}", TextComp.text );
         txt = txt.Replace( "{speed_txt}", new LocalizedTextBind( "KEY_ITEM_STAT_SPEED", false ).Localize() );
         txt = txt.Replace( "{accuracy_txt}", new LocalizedTextBind( "KEY_ITEM_STAT_ACCURACY", false ).Localize() );
         txt = txt.Replace( "{perception_txt}", new LocalizedTextBind( "KEY_ITEM_STAT_PERCEPTION", false ).Localize() );
         txt = txt.Replace( "{stealth_txt}", new LocalizedTextBind( "KEY_ITEM_STAT_STEALTH", false ).Localize() );
         txt = txt.Replace( "{speed}", ModTxt( stats.Speed.Value.EndValue - character.GetProgressionBaseStats().Speed ) );
         txt = txt.Replace( "{accuracy}", PercTxt( stats.Accuracy ) );
         txt = txt.Replace( "{perception}", ModTxt( stats.Perception.Value.EndValue - stats.Perception.Value.BaseValue ) );
         txt = txt.Replace( "{stealth}", PercTxt( stats.Stealth ) );
         TextComp.text = txt;
      } catch ( Exception ex ) { Error( ex ); } }

      private static string ModTxt ( float val ) {
         return val >= 0 ? ( "+" + val ) : val.ToString();
      }

      private static string PercTxt ( BaseStat stat ) => PercTxt( stat.Value.EndValue );
      private static string PercTxt ( float val ) {
         return ( val >= 0 ? "+" : "" ) + ( val * 100 ) + "%";
      }
   }
}