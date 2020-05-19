using Base.Core;
using PhoenixPoint.Common.View.ViewModules;
using System;
using System.Diagnostics;

namespace Sheepy.PhoenixPt.BlockTelemetry {

   public class Mod : ZyMod {

      public static void Init () {
         new Mod().SplashMod();
         MainMod();
      }

      public void SplashMod ( Func<string, object, object> api = null ) {
         SetApi( api );
         var type = GameAssembly?.GetType( "AmplitudeWebClient" );
         if ( type == null ) {
            Api( "log err", "AmplitudeWebClient not found." );
            return;
         }

         // Disable all telemetry checks
         TryPatch( type, "get_Enabled", nameof( DisableAll ) );
         TryPatch( type, "get_UserHasAllowed", nameof( DisableAll ) );
         TryPatch( type, "get_TelemetryOptionValue", nameof( OptOutTele ) );
         // Disable events that ignored the checks
         TryPatch( type, "SendEventTask", nameof( BlockSendEvent ) );

         // Disable tele button
         TryPatch( typeof( UIModuleGameplayOptionsPanel ), "Show", postfix: nameof( DisableTeleToggle ) );
      }

      public static void MainMod () {
         // Opt out of tele
         GameUtl.GameComponent<OptionsComponent>().Options.Set( "TelemetryInformationAccepted", 0 );
      }

      private static bool DisableAll ( ref bool __result ) {
         __result = false;
         return false;
      }

      private static bool OptOutTele ( ref int __result ) {
         __result = 0;
         return false;
      }

      private static bool BlockSendEvent () {
         Api( "log", "Blocking unexpected telemetry" );
         Api( "log v", new StackTrace( 1, false ) );
         return false;
      }

      private static void DisableTeleToggle ( UIModuleGameplayOptionsPanel __instance ) { try {
         __instance.CollectTelemetryDataToggle.interactable = false;
      } catch ( Exception ex ) { Api( "log e", ex ); } }
   }
}
