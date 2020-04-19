using Base.UI;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using System.Linq;
using UnityEngine;

namespace Sheepy.PhoenixPt.GlobeTweaks {
   internal class GlyphModule : ZyAdvMod {

      private static IPatch FovPatch;

      public void DoPatches () {
         var conf = Mod.Config.Haven_Icons ?? new HavenIconConfig();
         if ( conf.Always_Show_Action || conf.Always_Show_Soldier )
            FovPatch = TryPatch( typeof( GeoSiteVisualsController ), "Awake", postfix: nameof( AfterAwake_DisableFov ) );
         if ( conf.Popup_Show_Recruit_Class )
            TryPatch( typeof( UIModuleSelectionInfoBox ), "SetHaven", postfix: nameof( AfterSetHaven_ShowRecruitClass ) );
         if ( conf.Popup_Show_Trade )
            TryPatch( typeof( UIModuleSelectionInfoBox ), "SetHaven", postfix: nameof( AfterSetHaven_ShowResourceStock ) );
      }

      private static void AfterAwake_DisableFov ( GeoSiteVisualsController __instance ) { try {
         var conf = Mod.Config.Haven_Icons ?? new HavenIconConfig();
         if ( conf.Always_Show_Action ) DisableFov( __instance.RecruitAvailableIcon .transform.parent, "RecruitAvailableIcon"  );
         if ( conf.Always_Show_Soldier ) DisableFov( __instance.SoldiersAvailableIcon.transform.parent, "SoldiersAvailableIcon" );
         // Supply FOV is same as recruit
         // if ( conf.Always_Show_Trade   ) DisableFov( __instance.SuppliesResourcesIcon.transform.parent, "SuppliesResourcesIcon" );
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

      private static void AfterSetHaven_ShowRecruitClass ( UIModuleSelectionInfoBox __instance, GeoSite ____site, bool showRecruits ) { try {
         if ( ! showRecruits ) return;
         var recruit = ____site.GetComponent<GeoHaven>()?.AvailableRecruit;
         if ( recruit == null ) return;
         if ( OriginalRecruitText == null ) OriginalRecruitText = __instance.RecruitAvailableText.text;
         __instance.RecruitAvailableText.text = OriginalRecruitText + " (" + GetClassName( recruit ) + ')';
      } catch ( Exception ex ) { Error( ex ); } }

      private static string GetClassName ( GeoCharacter recruit ) => recruit.Progression.MainSpecDef.ViewElementDef.DisplayName1.Localize();

      private static void AfterSetHaven_ShowResourceStock ( UIModuleSelectionInfoBox __instance, GeoSite ____site ) { try {
         var res = ____site.GetComponent<GeoHaven>()?.GetResourceTrading();
         if ( res == null || res.Count == 0 ) return;
         var conf = Mod.Config.Haven_Icons;
         __instance.LeaderMottoText.text = string.Join( "", res.Select( e =>
            string.Format( e.ResourceStock >= e.OfferQuantity ? conf.In_Stock_Line : conf.Out_Of_Stock_Line,
            e.WantsQuantity, ResName( e.HavenWants ), e.OfferQuantity, ResName( e.HavenOffers ), e.ResourceStock )
         ) ).Trim();
      } catch ( Exception ex ) { Error( ex ); } }

      private static string ResName ( ResourceType type ) {
         switch ( type ) {
            case ResourceType.Materials : return new LocalizedTextBind( "Geoscape/KEY_GEOSCAPE_MATERIALS" ).Localize();
            case ResourceType.Supplies : return new LocalizedTextBind( "Geoscape/KEY_GEOSCAPE_FOOD" ).Localize();
            case ResourceType.Tech : return new LocalizedTextBind( "Geoscape/KEY_GEOSCAPE_TECH" ).Localize();
         }
         return type.ToString();
      }

      /* Not work, do not use
         TryPatch( typeof( GeoSiteVisualsController ), "RefreshHavenDetailsInformation", postfix: nameof( AfterHavenInfo_SetClassIcon ) );
      private static void AfterHavenInfo_SetClassIcon ( GeoSiteVisualsController __instance, GeoFaction viewer, GeoSite site ) { try {
         if ( ! ( viewer is GeoPhoenixFaction pp ) || ! pp.RecruitmentFunctionalityUnlocked ) return;
         var classIcon = site.GetComponent<GeoHaven>()?.AvailableRecruit?.Progression?.MainSpecDef?.ViewElementDef?.SmallIcon;
         if ( classIcon == null ) return;
         var material = __instance.RecruitAvailableIcon.GetComponent<MeshRenderer>().material;
         material.mainTexture = classIcon.texture;
      } catch ( Exception ex ) { Error( ex ); } }
      */
   }
}
