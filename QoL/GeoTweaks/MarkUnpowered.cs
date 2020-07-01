using I2.Loc;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.View.ViewControllers.PhoenixBase;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;

namespace Sheepy.PhoenixPt.GeoTweaks {

   internal class MarkUnpowered : ZyMod {

      internal void Init () {
         Patch( typeof( PhoenixFacilityController ), "UpdatePowerState", postfix: nameof( AfterUpdatePowerState_MarkPower ) );
         Patch( typeof( UIModuleBaseLayout ), "Init", nameof( BeforeSwitchState_StopTimer ), nameof( AfterSwitchState_StartTimer ) );
      }

      private readonly static HashSet<PhoenixFacilityController> MarkFacilities = new HashSet<PhoenixFacilityController>();
      private static Timer MarkTimer;
      private static int LoopCount = 0;

      private static void AfterUpdatePowerState_MarkPower ( PhoenixFacilityController __instance ) { try { lock( MarkFacilities ) {
         PhoenixFacilityController me = __instance;
         GeoPhoenixFacility facility = me.Facility;
         if ( facility == null || facility.IsWorking ) {
            me.FacilityName.color = Color.white;
            me.RepairBuildContainer.SetActive( false );
         } else {
            me.FacilityName.color = Color.red;
            //Log.Info( "{0} {1}", facility.ViewElementDef.DisplayName1.Localize(), facility.State );
            if ( facility.State == GeoPhoenixFacility.FacilityState.Functioning && facility.CanBePowered && ! facility.IsPowered ) {
               me.RepairBuildContainer.SetActive( true );
               me.RepairBuildSlidingContainer.gameObject.SetActive( true );
               me.RepairBuildSlidingContainer.offsetMin = new Vector2( __instance.RepairBuildSlidingContainer.offsetMin.x, 0 );
               me.RepairBuildText.enabled = true;
               me.RepairBuildText.GetComponent<Localize>().SetTerm( "KEY_BASE_POWER_OFF" );
            }
            MarkFacilities?.Add( me );
         }
      } } catch ( Exception ex ) { Error( ex ); } }

      private static void BeforeSwitchState_StopTimer () { try { lock( MarkFacilities ) {
         if ( MarkTimer != null ) {
            MarkTimer.Stop();
            MarkTimer = null;
         }
      } } catch ( Exception ex ) { Error( ex ); } }

      // Because text colour is not applied on first load, need a timer to kick them again
      private static void AfterSwitchState_StartTimer () { try { lock( MarkFacilities ) {
         LoopCount = 9;
         MarkTimer = new Timer( 10 ) { AutoReset = false };
         MarkTimer.Elapsed += RemarkTitle;
         MarkTimer.Enabled = true;
      } } catch ( Exception ex ) { Error( ex ); } }

      private static void RemarkTitle ( object sender, ElapsedEventArgs e ) { try { lock( MarkFacilities ) {
         if ( MarkTimer == null ) return; // Interrupted by base switch
         foreach ( PhoenixFacilityController tile in MarkFacilities )
            AfterUpdatePowerState_MarkPower( tile );
         if ( --LoopCount > 0 ) {
            MarkTimer.Interval += 10;
            MarkTimer.Start();
         } else {
            MarkFacilities.Clear();
            MarkTimer.Dispose();
            MarkTimer = null;
         }
      } } catch ( Exception ex ) { Error( ex ); } }
   }
}