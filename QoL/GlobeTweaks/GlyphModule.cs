using PhoenixPoint.Geoscape.View;
using System;
using UnityEngine;

namespace Sheepy.PhoenixPt.GlobeTweaks {
   internal class GlyphModule : ZyAdvMod {

      private static IPatch FovPatch;

      public void DoPatches () {
         var conf = Mod.Config.Haven_Icons ?? new HavenIconConfig();
         if ( conf.Always_Show_Recruit || conf.Always_Show_Soldier || conf.Always_Show_Trade )
            FovPatch = TryPatch( typeof( GeoSiteVisualsController ), "Awake", postfix: nameof( GeoSiteVisualsController_AfterAwake ) );
      }

      private static void GeoSiteVisualsController_AfterAwake ( GeoSiteVisualsController __instance ) { try {
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
   }
}
