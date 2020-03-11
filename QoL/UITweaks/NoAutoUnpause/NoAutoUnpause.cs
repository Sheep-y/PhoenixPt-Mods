using System;
using System.Reflection;

namespace Sheepy.PhoenixPt_NoAutoUnpause {
   using Harmony;
   using PhoenixPoint.Geoscape.View;
   using PhoenixPoint.Geoscape.View.ViewStates;
   using static System.Reflection.BindingFlags;

   public class Mod {
      //private static Logger Log = new Logger( "Mods/SheepyModLog.txt" );
      private static PropertyInfo ContextGetter;

      public static void Init () {
         HarmonyInstance harmony = HarmonyInstance.Create( typeof( Mod ).Namespace );

         ContextGetter = typeof( GeoscapeViewState ).GetProperty( "Context", NonPublic | Instance );
         if ( ContextGetter != null ) {
            Patch( harmony, typeof( UIStateVehicleSelected ).GetMethod( "AddTravelSite", NonPublic | Instance ),
                            typeof( Mod ).GetMethod( "Prefix_AddTravelSite" ),
                            typeof( Mod ).GetMethod( "Postfix_AddTravelSite" ) );
         }
      }

      private static void Patch ( HarmonyInstance harmony, MethodInfo toPatch, MethodInfo prefix, MethodInfo postfix ) {
         if ( toPatch == null ) throw new NullReferenceException( "Cannot find " + toPatch.Name + " to patch" );
         if ( prefix == null && postfix == null ) throw new NullReferenceException( "Prefix/Postfix patch not found for " + toPatch.Name );
         harmony.Patch( toPatch, new HarmonyMethod( prefix ), new HarmonyMethod( postfix ) );
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