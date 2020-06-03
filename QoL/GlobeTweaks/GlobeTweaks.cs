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

   internal class ModConfig {
      public bool Show_Airplane_Action_Time = true;
      public int  Same_Site_Scan_Cooldown_Min = 60;
      public string Hours_Format = " ({1}:{2:D2})";
      public string Days_Format = " ({3:F0}:{2:D2})";
      public bool Base_Centre_On_Heal = true;
      public bool Base_Pause_On_Heal = true;
      public bool Center_On_New_Base = true;
      public bool Auto_Unpause_Single = true;
      public bool Auto_Unpause_Multiple = false;
      public bool Notice_On_HP_Only_Heal = false;
      public bool Notice_On_Stamina_Only_Heal = true;
      public bool Vehicle_Centre_On_Heal = true;
      public bool Vehicle_Pause_On_Heal = true;
      public HavenIconConfig Haven_Icons = new HavenIconConfig();
      public uint  Config_Version = 20200603;

      public bool? No_Auto_Unpause { internal get; set; } // Replaced by Auto_Unpause_Multiple on ver 20200603

      internal void Upgrade () {
         if ( Config_Version < 20200603 ) {
            Config_Version = 202000603;
            if ( No_Auto_Unpause == false )
               Auto_Unpause_Multiple = true;
            ZyMod.Api( "config save", this );
         }
      }
   }

   internal class HavenIconConfig {
      public bool Always_Show_Action = true;
      public bool Always_Show_Soldier = true;
      public bool Hide_Recruit_Stickman = true;
      public bool Popup_Show_Recruit_Class = true;
      public bool Popup_Show_Trade = true;
      public string In_Stock_Line = "<size=28>{0} {1} > {2} {3} ({4})</size>\n";
      public string Out_Of_Stock_Line = "<color=red><size=28>{0} {1} > {2} {3} ({4})</size></color>\n";
   }

   public class Mod : ZyMod {

      internal static ModConfig Config;

      public static void Init () => new Mod().GeoscapeMod();

      public void MainMod ( Func< string, object, object > api ) => GeoscapeMod( api );

      public void GeoscapeMod ( Func< string, object, object > api = null ) {
         SetApi( api, out Config ).Upgrade();
         if ( Config.Show_Airplane_Action_Time )
            TryPatch( typeof( UIModuleSiteContextualMenu ), "SetMenuItems", postfix: nameof( AfterSetMenuItems_CalcTime ) );
         if ( Config.Same_Site_Scan_Cooldown_Min != 0 )
            TryPatch( typeof( UIModuleSiteContextualMenu ), "SetMenuItems", postfix: nameof( AfterSetMenuItems_DisableDupScan ) );
         new PauseModule().DoPatches();
         new GlyphModule().DoPatches();
      }

      private static void AfterSetMenuItems_CalcTime ( GeoSite site, List<SiteContextualMenuItem> ____menuItems ) {
         foreach ( var menu in ____menuItems ) try {
            var vehicle = menu.Ability.GeoActor as GeoVehicle;

            if ( menu.Ability is MoveVehicleAbility move && move.GeoActor is GeoVehicle flier )
               menu.ItemText.text += HoursToText( FlightHours( site, flier ) );

            else if ( menu.Ability is ExploreSiteAbility explore ) {
               var hours = GetVehicleTimeLeft( vehicle, (float) site.ExplorationTime.TimeSpan.TotalHours );
               Info( "Explore", hours );
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
            if ( Config.Same_Site_Scan_Cooldown_Min > 0 ) {
               var duration = scanner.ScannerDef.ExpansionTimeHours - hours;
               var cooldown = Config.Same_Site_Scan_Cooldown_Min / 60f;
               Info( duration, hours, cooldown );
               if ( duration > cooldown ) return;
            }
            Info( "Kill" );
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
         return string.Format( d > 0 ? Config.Days_Format : Config.Hours_Format, d, h, m, hours, hours * 60 );
      }
   }
}
