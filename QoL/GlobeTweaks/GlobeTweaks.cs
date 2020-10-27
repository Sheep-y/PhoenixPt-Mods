using Base.Input;
using PhoenixPoint.Geoscape.View;
using System;

namespace Sheepy.PhoenixPt.GlobeTweaks {

   internal class ModConfig {
      public bool Show_Airplane_Action_Time = true;
      public uint  Same_Site_Scan_Cooldown_Min = 60;
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
      public uint  Config_Version = 20201027;

      public bool? No_Auto_Unpause { internal get; set; } // Replaced by Auto_Unpause_Multiple on ver 20200603

      internal void Upgrade () {
         if ( Config_Version < 20200603 && No_Auto_Unpause == false )
               Auto_Unpause_Multiple = true;
         if ( Config_Version < 20201027 ) {
            Config_Version = 20201027;
            if ( Haven_Icons?.Always_Show_Soldier == true )
               Haven_Icons.Always_Show_Action = true;
            ZyMod.Api( "config save", this );
         }
         if ( Haven_Icons == null ) Haven_Icons = new HavenIconConfig();
      }
   }

   internal class HavenIconConfig {
      public bool Always_Show_Action = true;
      public bool Hide_Recruit_Stickman = true;
      public bool Popup_Show_Recruit_Class = true;
      public bool Popup_Show_Trade = true;
      public string In_Stock_Line = "<size=28>{0} {1} > {2} {3} ({4})</size>\n";
      public string Out_Of_Stock_Line = "<color=red><size=28>{0} {1} > {2} {3} ({4})</size></color>\n";

      public bool? Always_Show_Soldier { internal get; set; } // Merged with Always_Show_Action on ver 20201027
   }

   public class Mod : ZyMod {

      internal static ModConfig Config;

      public static void Init () => new Mod().GeoscapeMod();

      public void MainMod ( Func< string, object, object > api ) => GeoscapeMod( api );

      public void GeoscapeMod ( Func< string, object, object > api = null ) {
         SetApi( api, out Config ).Upgrade();
         new TimeModule().DoPatches();
         new PauseModule().DoPatches();
         new GlyphModule().DoPatches();
         Patch( typeof( GeoscapeViewState ), "OnInputEvent", postfix: nameof( Test ) );
      }

      private static void Test ( object __instance, InputEvent ev ) {
         Info( __instance.GetType(), ev.InputType, ev.Name );
      }
   }
}