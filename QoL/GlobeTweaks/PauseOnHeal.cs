using System;
using System.Reflection;

namespace Sheepy.PhoenixPt_PauseOnHeal {
   using Harmony;
   using PhoenixPoint.Geoscape.Entities;
   using PhoenixPoint.Geoscape.Levels;
   using PhoenixPoint.Geoscape.View.ViewStates;
   using System.Collections.Generic;
   using static System.Reflection.BindingFlags;

   public class Mod {
      //private static Logging.Logger Log = new Logging.Logger( "Mods/SheepyMods.log" );

      public static void Init () {
         HarmonyInstance harmony = HarmonyInstance.Create( typeof( Mod ).Namespace );
         Patch( harmony, typeof( GeoscapeLog ), "ProcessQueuedEvents", "BeforeProcessQueuedEvents_Pause" );
      }

      #region Modding helpers
      private static void Patch ( HarmonyInstance harmony, Type target, string toPatch, string prefix, string postfix = null ) {
         Patch( harmony, target.GetMethod( toPatch, Public | NonPublic | Instance | Static ), prefix, postfix );
      }

      private static void Patch ( HarmonyInstance harmony, MethodInfo toPatch, string prefix, string postfix = null ) {
         harmony.Patch( toPatch, ToHarmonyMethod( prefix ), ToHarmonyMethod( postfix ) );
      }

      private static HarmonyMethod ToHarmonyMethod ( string name ) {
         if ( name == null ) return null;
         MethodInfo func = typeof( Mod ).GetMethod( name );
         if ( func == null ) throw new NullReferenceException( name + " is null" );
         return new HarmonyMethod( func );
      }

      private static bool LogError ( Exception ex ) {
         //Log.Error( ex );
         return true;
      }
      #endregion

      public static void BeforeProcessQueuedEvents_Pause ( List<IGeoCharacterContainer> ____justRestedContainer, GeoLevelController ____level ) { try {
         foreach ( IGeoCharacterContainer container in ____justRestedContainer ) {
            if ( container is GeoVehicle vehicle ) {
               ____level.Timing.Paused = true;
               if ( ____level.View.CurrentViewState is UIStateVehicleSelected state )
                  typeof( UIStateVehicleSelected ).GetMethod( "SelectVehicle", NonPublic | Instance ).Invoke( state, new object[]{ vehicle, true } );
               else
                  ____level.View.ChaseTarget( vehicle.CurrentSite, false );
            }
         }
      } catch ( Exception ex ) { LogError( ex ); } }
   }
}