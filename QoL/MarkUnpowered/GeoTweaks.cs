using System;

namespace Sheepy.PhoenixPt.GeoTweaks {

   public class Mod : ZyMod {

      public static void Init () => MainMod();

      public static void MainMod ( Func< string, object, object > api = null ) => GeoscapeMod( api );

      public static void GeoscapeMod ( Func< string, object, object > api ) {
         SetApi( api );
         new MarkUnpowered().Init();
         new SoldierStats().Init();
      }
   }
}