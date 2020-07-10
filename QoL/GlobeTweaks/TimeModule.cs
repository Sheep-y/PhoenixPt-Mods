using Base.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewModules;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sheepy.PhoenixPt.GlobeTweaks {
   public class TimeModule : ZyMod {
      
      public void DoPatches () {
         var config = Mod.Config;
         if ( config.Show_Airplane_Action_Time )
            TryPatch( typeof( UIModuleSiteContextualMenu ), "SetMenuItems", postfix: nameof( AfterSetMenuItems_CalcTime ) );
         if ( config.Same_Site_Scan_Cooldown_Min != 0 )
            TryPatch( typeof( UIModuleSiteContextualMenu ), "SetMenuItems", postfix: nameof( AfterSetMenuItems_DisableDupScan ) );
      }

      private static void AfterSetMenuItems_CalcTime ( GeoSite site, List<SiteContextualMenuItem> ____menuItems ) {
         foreach ( var menu in ____menuItems ) try {
            var vehicle = menu.Ability?.GeoActor as GeoVehicle;

            if ( menu.Ability is MoveVehicleAbility move && move.GeoActor is GeoVehicle flier )
               menu.ItemText.text += HoursToText( FlightHours( site, flier ) );

            else if ( menu.Ability is ExploreSiteAbility explore ) {
               var hours = GetVehicleTimeLeft( vehicle, (float) site.ExplorationTime.TimeSpan.TotalHours );
               menu.ItemText.text += HoursToText( hours );

            } else if ( menu.Ability is ScanAbility scan ) {
               var scanner = GetScanner( site, scan, out float hours, true );
               if ( scanner != null ) menu.ItemText.text += HoursToText( hours );
            } // LaunchMissionAbility, HavenTradeAbility, HavenDetailsAbility
         } catch ( Exception ex ) { Error( ex ); }
      }

      private static void AfterSetMenuItems_DisableDupScan ( GeoSite site, List<SiteContextualMenuItem> ____menuItems ) { try {
         foreach ( var menu in ____menuItems ) {
            if ( ! ( menu.Ability is ScanAbility scan ) ) continue;
            if ( ! menu.Button.IsInteractable() ) return;
            var scanner = GetScanner( site, scan, out float hours, false );
            if ( scanner == null ) return;
            var duration = scanner.ScannerDef.ExpansionTimeHours - hours;
            var cooldown = Mod.Config.Same_Site_Scan_Cooldown_Min / 60f;
            if ( cooldown == 0 || ( cooldown > 0 && duration > cooldown ) ) return;
            Verbo( "Preventing duplicate scan.  Last scan {0}hr, threshold {1}hr.", duration, cooldown );
            menu.Button.SetInteractable( false );
            break;
         }
      } catch ( Exception ex ) { Error( ex ); } }

      private static GeoScanner GetScanner ( GeoSite site, GeoAbility ability, out float remainingHours, bool GetFirst ) {
         remainingHours = 0;
         var scanners = ( ability.GeoActor as GeoVehicle )?.Owner?.Scanners?.Where( e => e.Location == site );
         if ( scanners == null || ! scanners.Any() ) return null;
         var scanner = GetFirst ? scanners.First() : scanners.Last();
         remainingHours = (float) ( ( scanner.SerializationData as GeoScanner.GeoScannerInstanceData ).ExpansionEndDate - scanner.Timing.Now ).TimeSpan.TotalHours;
         return scanner;
      }

      private static float GetVehicleTimeLeft ( GeoVehicle vehicle, float def ) { try {
         if ( vehicle == null ) return def;
         var updateable = typeof( GeoVehicle ).GetField( "_explorationUpdateable", BindingFlags.NonPublic | BindingFlags.Instance ).GetValue( vehicle );
         if ( updateable == null ) return def;
         NextUpdate endTime = (NextUpdate) updateable.GetType().GetProperty( "NextUpdate" ).GetValue( updateable );
         return (float) -( vehicle.Timing.Now - endTime.NextTime ).TimeSpan.TotalHours;
      } catch ( Exception ex ) { Warn( ex ); return def; } }

      private static float FlightHours ( GeoSite site, GeoVehicle vehicle ) {
         var pos = vehicle.CurrentSite?.WorldPosition ?? vehicle.WorldPosition;
         var path = vehicle.Navigation.FindPath( pos, site.WorldPosition, out bool hasPath );
         if ( ! hasPath || path.Count < 2 ) return -1;
         float distance = 0;
         for ( int i = 0, len = path.Count - 1 ; i < len ; )
            distance += GeoMap.Distance( path[i].Pos.WorldPosition, path[++i].Pos.WorldPosition ).Value;
         return distance / vehicle.Stats.Speed.Value;
      }

      private static string HoursToText ( float hours ) {
         if ( hours < 0 ) return "-" + HoursToText( -hours );
         int intHour = (int) hours;
         int d = Math.DivRem( intHour, 24, out int h ), m = (int) Math.Ceiling( ( hours - intHour ) * 60 );
         var format = d > 0 ? Mod.Config.Days_Format : Mod.Config.Hours_Format;
         if ( string.IsNullOrWhiteSpace( format ) ) return "";
         return string.Format( format, d, h, m, hours, hours * 60 );
      }
   }
}
