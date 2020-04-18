using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.View;
using System;
using UnityEngine;

namespace Sheepy.PhoenixPt.GlobeTweaks {
   internal class GlyphModule : ZyAdvMod {

      private static IPatch FovPatch;

      public void DoPatches () {
         FovPatch = TryPatch( typeof( GeoSiteVisualsController ), "Awake", postfix: nameof( Test2 ) );
      }

      private static void Test2 ( GeoSiteVisualsController __instance ) { try {
         var fov = __instance.RecruitAvailableIcon.transform.parent.gameObject.GetComponent<FovControllableBehavior>();
         if ( fov == null || fov.ControllingDef.InvisibleOverFov < 0 ) return;
         fov.ControllingDef.InvisibleOverFov = -1;
         Api( "zy.ui.dump", __instance.RecruitAvailableIcon.transform.parent.parent );
         FovPatch.Unpatch();
      } catch ( Exception ex ) { Error( ex ); } }
   }
}
