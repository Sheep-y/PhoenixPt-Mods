using System;
using System.Reflection;

namespace Sheepy.PhoenixPt_MarkUnpowered {
   using Harmony;
   using I2.Loc;
   using PhoenixPoint.Geoscape.Entities.PhoenixBases;
   using PhoenixPoint.Geoscape.View.ViewControllers.PhoenixBase;
   using PhoenixPoint.Geoscape.View.ViewModules;
   using System.Collections.Generic;
   using System.Timers;
   using UnityEngine;
   using static System.Reflection.BindingFlags;

   public class Mod {
      //private static Logging.Logger Log = new Logging.Logger( "Mods/SheepyMods.log" );

      private static HarmonyInstance harmony;
      public static void Init () { try {
         harmony = HarmonyInstance.Create( typeof( Mod ).Namespace );
         Patch( harmony, typeof( PhoenixFacilityController ), "UpdatePowerState", null, "AfterUpdatePowerState_MarkPower" );
         Patch( harmony, typeof( UIModuleBaseLayout ), "Init", "BeforeSwitchState_CreateSet", "AfterSwitchState_StartTimer" );
      } catch ( Exception ex ) { LogError( ex ); } }

      #region Modding helpers
      private static void Patch ( HarmonyInstance harmony, Type target, string toPatch, string prefix, string postfix = null ) {
         Patch( harmony, target.GetMethod( toPatch, Public | NonPublic | Instance | Static ), prefix, postfix );
      }

      private static void Patch ( HarmonyInstance harmony, MethodInfo toPatch, string prefix, string postfix = null ) {
         harmony.Patch( toPatch, ToHarmonyMethod( prefix ), ToHarmonyMethod( postfix ) );
      }

      private static HarmonyMethod ToHarmonyMethod ( string name ) {
         if ( name == null ) return null;
         MethodInfo func = typeof( Mod ).GetMethod( name );
         if ( func == null ) throw new NullReferenceException( name + " is null" );
         return new HarmonyMethod( func );
      }

      private static bool LogError ( Exception ex ) {
         //Log?.Error( ex );
         return true;
      }
      #endregion
      
      private static HashSet<PhoenixFacilityController> MarkFacilities;
      private static Timer MarkTimer;
      private static int LoopCount = 0;

      public static void AfterUpdatePowerState_MarkPower ( PhoenixFacilityController __instance ) { try { lock( harmony ) {
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
      } } catch ( Exception ex ) { LogError( ex ); } }

      public static void BeforeSwitchState_CreateSet () { try { lock( harmony ) {
         MarkFacilities = new HashSet<PhoenixFacilityController>();
         if ( MarkTimer != null ) {
            MarkTimer.Stop();
            MarkTimer = null;
         }
      } } catch ( Exception ex ) { LogError( ex ); } }

      // Because text colour is not applied on first load, need a timer to kick them again
      public static void AfterSwitchState_StartTimer () { try { lock( harmony ) {
         LoopCount = 9;
         MarkTimer = new Timer( 10 ) { AutoReset = false };
         MarkTimer.Elapsed += RemarkTitle;
         MarkTimer.Enabled = true;
      } } catch ( Exception ex ) { LogError( ex ); } }

      public static void RemarkTitle ( object sender, ElapsedEventArgs e ) { try { lock( harmony ) {
         if ( MarkTimer == null ) return; // Interrupted by base switch
         foreach ( PhoenixFacilityController tile in MarkFacilities )
            AfterUpdatePowerState_MarkPower( tile );
         if ( --LoopCount > 0 ) {
            MarkTimer.Interval += 10;
            MarkTimer.Start();
         } else {
            MarkFacilities = null;
            MarkTimer.Dispose();
            MarkTimer = null;
         }
      } } catch ( Exception ex ) { LogError( ex ); } }
   }
}