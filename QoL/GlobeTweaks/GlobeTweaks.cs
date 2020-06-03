using Base.Core;

using com.ootii.Utilities.Debug;

using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine.Experimental.PlayerLoop;

namespace Sheepy.PhoenixPt.GlobeTweaks {

   internal class ModConfig {
      public bool Show_Airplane_Action_Time = true;
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
         new PauseModule().DoPatches();
         new GlyphModule().DoPatches();
      }

      private static void AfterSetMenuItems_CalcTime ( GeoSite site, List<SiteContextualMenuItem> ____menuItems ) { try {
         foreach ( var menu in ____menuItems ) {
            if ( menu.Ability is MoveVehicleAbility move && move.GeoActor is GeoVehicle flier )
               menu.ItemText.text += HoursToText( FlightHours( site, flier ) );
            else if ( menu.Ability is ExploreSiteAbility explore ) {
               var minutes = GetVehicleTimeLeft( explore.GeoActor as GeoVehicle, (float) site.ExplorationTime.TimeSpan.TotalMinutes );
               Info( "Explore", minutes );
               menu.ItemText.text += HoursToText( minutes / 60f );
            }
         }
      } catch ( Exception ex ) { Error( ex ); } }

      private static float GetVehicleTimeLeft ( GeoVehicle vehicle, float def ) { try {
         if ( vehicle == null ) return def;
         var updateable = typeof( GeoVehicle ).GetField( "_explorationUpdateable", BindingFlags.NonPublic | BindingFlags.Instance ).GetValue( vehicle );
         if ( updateable == null ) return def;
         NextUpdate endTime = (NextUpdate) updateable.GetType().GetProperty( "NextUpdate" ).GetValue( updateable );
         return (float) -( vehicle.Timing.Now - endTime.NextTime ).TimeSpan.TotalMinutes;
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
         if ( hours < 0 ) return "";
         int intHour = (int) hours;
         int d = Math.DivRem( intHour, 24, out int h ), m = (int) Math.Ceiling( ( hours - intHour ) * 60 );
         return string.Format( d > 0 ? Config.Days_Format : Config.Hours_Format, d, h, m, hours, hours * 60 );
      }
   }
}
