using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using UnityEngine;

namespace Sheepy.PhoenixPt.GlobeTweaks {
   internal class GlyphModule : ZyAdvMod {

      private static IPatch FovPatch;

      public void DoPatches () {
         var conf = Mod.Config.Haven_Icons ?? new HavenIconConfig();
         if ( conf.Always_Show_Recruit || conf.Always_Show_Soldier || conf.Always_Show_Trade )
            FovPatch = TryPatch( typeof( GeoSiteVisualsController ), "Awake", postfix: nameof( AfterAwake_DisableFov ) );
         if ( conf.Show_Recruit_Class_In_Mouseover )
            TryPatch( typeof( UIModuleSelectionInfoBox ), "SetHaven", postfix: nameof( AfterSetHaven_ShowRecruitClass ) );
      }

      private static void AfterAwake_DisableFov ( GeoSiteVisualsController __instance ) { try {
         var conf = Mod.Config.Haven_Icons ?? new HavenIconConfig();
         if ( conf.Always_Show_Recruit ) DisableFov( __instance.RecruitAvailableIcon .transform.parent, "RecruitAvailableIcon"  );
         if ( conf.Always_Show_Soldier ) DisableFov( __instance.SoldiersAvailableIcon.transform.parent, "SoldiersAvailableIcon" );
         if ( conf.Always_Show_Trade   ) DisableFov( __instance.SuppliesResourcesIcon.transform.parent, "SuppliesResourcesIcon" );
         //Api( "zy.ui.dump", __instance.RecruitAvailableIcon.transform.parent.parent ); // Dump gui tree with debug console
         FovPatch.Unpatch();
      } catch ( Exception ex ) { Error( ex ); } }

      private static void DisableFov ( Transform t, string type ) {
         var fov = t.gameObject.GetComponent<FovControllableBehavior>();
         if ( fov?.ControllingDef?.InvisibleOverFov > 0 )
            fov.ControllingDef.InvisibleOverFov = -1;
         else
            Warn( "Fov not found, cannot set always visible: {0}", type );
      }

      private static string OriginalRecruitText;

      private static void AfterSetHaven_ShowRecruitClass ( UIModuleSelectionInfoBox __instance, GeoSite ____site, bool showRecruits ) {
         if ( ! showRecruits ) return;
         var recruit = ____site.GetComponent<GeoHaven>()?.AvailableRecruit;
         if ( recruit == null ) return;
         if ( OriginalRecruitText == null ) OriginalRecruitText = __instance.RecruitAvailableText.text;
         __instance.RecruitAvailableText.text = OriginalRecruitText + " (" + recruit.Progression.MainSpecDef.ViewElementDef.DisplayName1.Localize() + ')';
      }
   }
}
