using System;

namespace Sheepy.PhoenixPt.FullBodyAug {

   public static class Mod {

      public static void Init () => GeoscapeMod();

      public static void MainMod ( Func<string, object, object> api ) => GeoscapeMod( api );

      public static void GeoscapeMod ( Func<string, object, object> api = null ) {
         ZyMod.SetApi( api );
         var module = new UncapAug();
         module.UncapMutate();
         module.UncapBionic();
      }
   }
}
