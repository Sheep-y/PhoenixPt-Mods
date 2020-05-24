using System;

namespace Sheepy.PhoenixPt.FullBodyAug {

   public class Mod : ZyMod {

      public static void Init () => new Mod().GeoscapeMod();

      public void MainMod ( Func<string, object, object> api ) => GeoscapeMod( api );

      public void GeoscapeMod ( Func<string, object, object> api = null ) {
         SetApi( api );
         var module = new UncapAug();
         module.UncapMutate();
         module.UncapBionic();
      }
   }
}
