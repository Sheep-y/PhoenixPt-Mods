using System;
using System.Linq;
using System.Reflection;

namespace Sheepy.PhoenixPt.BlockTelemetry {

   public class Mod : ZyMod {

      public static void Init () => new Mod().SplashMod();

      public void SplashMod ( Func<string, object, object> api = null ) {
         SetApi( api );
         var type = GameAssembly?.GetType( "AmplitudeWebClient" );
         if ( type == null ) {
            Api( "log error", "AmplitudeWebClient not found." );
            return;
         }

         TryPatch( type, "get_Enabled", nameof( DisableAll ) );
         TryPatch( type, "get_UserHasAllowed", nameof( DisableAll ) );
         TryPatch( type, "SendEventTask", nameof( BlockSendEvent ) );
      }

      private static bool DisableAll ( ref bool __result ) {
         __result = false;
         return false;
      }

      private static bool BlockSendEvent () {
         Api( "log info", new ApplicationException( "Blocking unexpected telemetry." ) );
         return false;
      }
   }
}
