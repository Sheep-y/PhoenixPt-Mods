using Harmony;
using I2.Loc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sheepy.PhoenixPt.Zht {
    public class Mod {
      public static void Init () => new Mod().SplashMod();

      public void SplashMod ( Func< string, object, object > api = null ) {
         api( "log", LocalizationManager.CurrentLanguageCode );
         //HarmonyInstance.Create( typeof( Mod ).Namespace ).Patch( );
      }
    }
}
