using Harmony;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using static System.Reflection.BindingFlags;

namespace Sheepy.PhoenixPt.GlobeTweaks {
   class ModSettings {
      public string Settings_Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
      public bool CenterOnNewBase = true;
      public bool NoAutoUnpause = true;
      public bool PauseOnHeal = true;
   }

   class Mod : SheepyMod {
      public static ModSettings DefaultSettings => new ModSettings();

      private static PropertyInfo ContextGetter;

      public void Init () => new Mod().MainMod();

      public void MainMod ( ModSettings settings = null, Action< SourceLevels, object, object[] > logger = null ) {
         if ( settings == null ) settings = DefaultSettings;
         SetLogger( logger );

         if ( settings.CenterOnNewBase ) {
            Patch( typeof( GeoPhoenixFaction ), "ActivatePhoenixBase", postfix: nameof( AfterActivatePhoenixBase_Center ) );
            Patch( typeof( GeoFaction ), "OnVehicleSiteExplored", postfix: nameof( AfterExplore_Center ) );
         }

         if ( settings.NoAutoUnpause ) {
            ContextGetter = typeof( GeoscapeViewState ).GetProperty( "Context", NonPublic | Instance );
            if ( ContextGetter != null )
               Patch( typeof( UIStateVehicleSelected ), "AddTravelSite", nameof( Prefix_AddTravelSite ), nameof( Postfix_AddTravelSite ) );
         }
      }

      #region CenterOnNewBase
      public static void AfterActivatePhoenixBase_Center ( GeoSite site, GeoLevelController ____level ) { try {
         ____level.View.ChaseTarget( site, false );
      } catch ( Exception ex ) { Error( ex ); } }

      public static void AfterExplore_Center ( GeoFaction __instance, GeoVehicle vehicle, GeoLevelController ____level ) { try {
         if ( __instance != ____level.PhoenixFaction ) return;
         ____level.View.ChaseTarget( vehicle.CurrentSite, false );
      } catch ( Exception ex ) { Error( ex ); } }
      #endregion

      #region NoAutoUnpause
      private static GeoscapeViewContext getContext ( UIStateVehicleSelected __instance ) => (GeoscapeViewContext) ContextGetter.GetValue( __instance );

      [ HarmonyPriority( Priority.VeryHigh ) ]
      public static void Prefix_AddTravelSite ( UIStateVehicleSelected __instance, ref bool __state ) { try {
         __state = getContext( __instance ).Level.Timing.Paused;
      } catch ( Exception ex ) { Error( ex ); } }

      public static void Postfix_AddTravelSite ( UIStateVehicleSelected __instance, ref bool __state ) { try {
         getContext( __instance ).Level.Timing.Paused = __state;
      } catch ( Exception ex ) { Error( ex ); } }
      #endregion
   }
}
