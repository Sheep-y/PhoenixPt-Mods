using Base.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sheepy.PhoenixPt.GlobeTweaks {

   internal class ModConfig {
      public bool Show_Airplane_Action_Time = true;
      public string Hours_Format = " ({1}:{2:D2})";
      public string Days_Format = " ({3:F0}:{2:D2})";
      public bool Base_Centre_On_Heal = true;
      public bool Base_Pause_On_Heal = true;
      public bool Center_On_New_Base = true;
      public bool No_Auto_Unpause = true;
      public bool Notice_On_HP_Only_Heal = false;
      public bool Notice_On_Stamina_Only_Heal = true;
      public bool Vehicle_Centre_On_Heal = true;
      public bool Vehicle_Pause_On_Heal = true;
      public HavenIconConfig Haven_Icons = new HavenIconConfig();
      public uint  Config_Version = 20200419;

      internal void Upgrade () {
         if ( Config_Version < 20200419 ) {
            Config_Version = 20200419;
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

   public class Mod : ZyAdvMod {

      internal static ModConfig Config;

      public static void Init () => new Mod().MainMod();

      public void MainMod ( Func< string, object, object > api = null ) {
         SetApi( api, out Config );
         if ( Config.Show_Airplane_Action_Time )
            Patch( typeof( UIModuleSiteContextualMenu ), "SetMenuItems", postfix: nameof( AfterSetMenuItems_CalcTime ) );
         new PauseModule().DoPatches();
         new GlyphModule().DoPatches();
      }

      private static void AfterSetMenuItems_CalcTime ( GeoSite site, List<SiteContextualMenuItem> ____menuItems ) { try {
         foreach ( var menu in ____menuItems ) {
            if ( menu.Ability is MoveVehicleAbility move && move.GeoActor is GeoVehicle flier ) {
               var time = FlightHours( site, flier );
               if ( time > 0 )
                  menu.ItemText.text += HoursToText( time );
            } else if ( menu.Ability is ExploreSiteAbility explore ) {
               menu.ItemText.text += HoursToText( (float) site.ExplorationTime.TimeSpan.TotalMinutes / 60f );
            //} else if ( menu.Ability is EmergencyRepairAbility repair ) { // Unused as of game ver 1.0.57335
            //   menu.ItemText.text += HoursToText( repair.EmergencyRepairAbilityDef.TimeInHours );
            }
         }
      } catch ( Exception ex ) { Error( ex ); } }

      private static float FlightHours ( GeoSite site, GeoVehicle vehicle ) {
         var pos = vehicle.CurrentSite?.WorldPosition ?? vehicle.WorldPosition;
         var path = vehicle.Navigation.FindPath( pos, site.WorldPosition, out bool hasPath );
         if ( ! hasPath ) return -1;
         float distance = 0;
         for ( int i = 0, len = path.Count - 1 ; i < len ; )
            distance += GeoMap.Distance( path[i].Pos.WorldPosition, path[++i].Pos.WorldPosition ).Value;
         return distance / vehicle.Stats.Speed.Value;
      }

      private static string HoursToText ( float hours ) {
         int intHour = (int) hours;
         int d = Math.DivRem( intHour, 24, out int h ), m = (int) Math.Ceiling( ( hours - intHour ) * 60 );
         return string.Format( d > 0 ? Config.Days_Format : Config.Hours_Format, d, h, m, hours, hours * 60 );
      }
   }
}
