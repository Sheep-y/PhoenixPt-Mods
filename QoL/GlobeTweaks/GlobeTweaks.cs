using Harmony;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using static System.Reflection.BindingFlags;

namespace Sheepy.PhoenixPt.GlobeTweaks {
   class Settings {
      public bool NoAutoUnpause = true;
   }

   class Mod : SheepyMod {
      private static PropertyInfo ContextGetter;

      public void Init () => new Mod().MainMod();

      public void MainMod ( Settings settings = null ) {
         if ( settings == null ) settings = new Settings();

         if ( settings.NoAutoUnpause ) {
            var ContextGetter = typeof( GeoscapeViewState ).GetProperty( "Context", NonPublic | Instance );
            if ( ContextGetter != null )
               Patch( typeof( UIStateVehicleSelected ), "AddTravelSite", nameof( Prefix_AddTravelSite ), nameof( Postfix_AddTravelSite ) );
         }
      }

      private static GeoscapeViewContext getContext ( UIStateVehicleSelected __instance ) {
         return (GeoscapeViewContext) ContextGetter.GetValue( __instance );
      }

      [ HarmonyPriority( Priority.VeryHigh ) ]
      public static void Prefix_AddTravelSite ( UIStateVehicleSelected __instance, ref bool __state ) {
         __state = getContext( __instance ).Level.Timing.Paused;
      }

      public static void Postfix_AddTravelSite ( UIStateVehicleSelected __instance, ref bool __state ) {
         getContext( __instance ).Level.Timing.Paused = __state;
      }
   }
}
