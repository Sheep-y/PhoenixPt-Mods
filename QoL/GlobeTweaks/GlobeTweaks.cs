using System;
using System.Text;
using System.Threading.Tasks;

namespace Sheepy.PhoenixPt.GlobeTweaks {

   public class ModConfig {
      public int  Config_Version = 20200324;
      public bool Base_Centre_On_Heal = true;
      public bool Base_Pause_On_Heal = true;
      public bool Center_On_New_Base = true;
      public bool No_Auto_Unpause = true;
      public bool Pause_On_HP_Only_Heal = true;
      public bool Pause_On_Stamina_Only_Heal = true;
      public bool Vehicle_Centre_On_Heal = true;
      public bool Vehicle_Pause_On_Heal = true;
   }

   public class Mod : ZyAdvMod {

      public static ModConfig Config;

      public static void Init () => new Mod().MainMod();

      public void MainMod ( Func< string, object, object > api = null ) {
         SetApi( api, out Config );
         new PauseMod().DoPatches();
      }

   }
}
