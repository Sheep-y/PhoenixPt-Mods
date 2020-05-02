using Harmony;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Sheepy.PhoenixPt.BlockTelemetry {

   public static class Mod {

      internal static Func<string,object,object> Api;

      public static void Init () => SplashMod();

      public static void SplashMod ( Func<string, object, object> api = null ) {
         var type = ( api( "assembly", "game" ) as Assembly ??
            AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault( e => e.FullName.StartsWith( "Assembly-CSharp," ) ) )
            ?.GetType( "AmplitudeWebClient" );
         if ( type == null ) {
            api?.Invoke( "log error", "AmplitudeWebClient not found." );
            return;
         }

         Api = api;
         var harmony = HarmonyInstance.Create( typeof( Mod ).Namespace );

         api?.Invoke( "log", "Force disabling WebClient." );
         harmony.Patch( type.GetMethod( "get_Enabled" ),
            new HarmonyMethod( typeof( Mod ).GetMethod( nameof( DisableAll ), BindingFlags.NonPublic | BindingFlags.Static ) ) );

         api?.Invoke( "log", "Force disabling WebClient.UserHasAllowed." );
         harmony.Patch( type.GetMethod( "get_UserHasAllowed" ),
            new HarmonyMethod( typeof( Mod ).GetMethod( nameof( DisableAll ), BindingFlags.NonPublic | BindingFlags.Static ) ) );

         api?.Invoke( "log", "Blocking telemetry events." );
         harmony.Patch( type.GetMethod( "SendEventTask", BindingFlags.NonPublic | BindingFlags.Instance ),
            new HarmonyMethod( typeof( Mod ).GetMethod( nameof( BlockSendEvent ), BindingFlags.NonPublic | BindingFlags.Static ) ) );
      }

      private static bool DisableAll ( ref bool __result ) {
         __result = false;
         return false;
      }

      private static bool BlockSendEvent () {
         Api?.Invoke( "log info", new ApplicationException( "Blocking unexpected telemetry" ) );
         return false;
      }
   }
}
