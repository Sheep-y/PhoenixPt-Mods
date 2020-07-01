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
         if ( PatchTelemetry( GameAssembly?.GetType( "AmplitudeWebClient" ) ?? typeof( PhoenixTelemetry ) ) ) {
            // Disable tele button
            TryPatch( typeof( UIModuleGameplayOptionsPanel ), "Show", postfix: nameof( DisableTeleToggle ) );
         }
      }

      public bool PatchTelemetry ( Type type ) {
         if ( type == null ) {
            Api( "log err", "Telemetry client not found" );
            return false;
         }
         var patched = false;
         // Disable all telemetry checks
         if ( TryPatch( type, "get_Enabled", nameof( DisableAll ) ) != null ) patched = true;
         if ( TryPatch( type, "get_UserHasAllowed", nameof( DisableAll ) ) != null ) patched = true;
         if ( TryPatch( type, "get_TelemetryOptionValue", nameof( OptOutTele ) ) != null ) patched = true;
         // Disable events that ignored the checks
         if ( TryPatch( type, "AddEvent", nameof( BlockAddEvent ) ) != null ) patched = true;
         if ( TryPatch( type, "SendEventTask", nameof( BlockSendEvent ) ) != null ) patched = true;
         return patched;
      }

      public void PatchPhoenixTelemetry () {
         TryPatch( typeof( PhoenixTelemetry ), "SendEventTask", postfix: nameof( BlockSendEvent ) );
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

      private static bool BlockAddEvent () {
         Api( "log", "Blocking unexpected telemetry" );
         Api( "log v", new StackTrace( 1, false ) );
         return false;
      }

      private static bool BlockSendEvent () {
         Api( "log", "Blocking unexpected telemetry send" );
         return false;
      }

      private static void DisableTeleToggle ( UIModuleGameplayOptionsPanel __instance ) { try {
         __instance.CollectTelemetryDataToggle.interactable = false;
      } catch ( Exception ex ) { Api( "log e", ex ); } }
   }
}
