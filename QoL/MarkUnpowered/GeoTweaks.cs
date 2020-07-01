using System;

namespace Sheepy.PhoenixPt.GeoTweaks {

   internal class ModConfig {
      public bool Greyout_Unpowered_Facility = true;
      public string Equip_Weight_Replacement = "{encumbrance}, {speed_txt} {speed}, {accuracy_txt} {accuracy}, {perception_txt} {perception}, {stealth_txt} {stealth}";
      public int Config_Version = 20200701;
   }

   public class Mod : ZyMod {
      internal static ModConfig Config;

      public static void Init () => MainMod();

      public static void MainMod ( Func< string, object, object > api = null ) => GeoscapeMod( api );

      public static void GeoscapeMod ( Func< string, object, object > api ) {
         SetApi( api, out Config );
         if ( Config.Greyout_Unpowered_Facility )
            new MarkUnpowered().Init();
         if ( ! string.IsNullOrWhiteSpace( Config.Equip_Weight_Replacement ) )
            new SoldierStats().Init();
      }
   }
}