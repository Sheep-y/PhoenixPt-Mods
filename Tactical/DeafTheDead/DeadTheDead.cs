using Harmony;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sheepy.PhoenixPt.DeafTheDead {

   public static class Mod {
      public static void Init () => MainMod();
      public static void MainMod ( Func<string,object,object> api = null ) {
         HarmonyInstance.Create( typeof( Mod ).Namespace ).PatchAll();
         api?.Invoke( "log", "Mod initiated" );
      }
   }

   [ HarmonyPatch( typeof( TacticalFactionVision ), "ReUpdateHearingImpl" ) ]
   internal static class TacticalFactionVision_ReUpdateHearingImpl {
      private static bool Prefix ( TacticalActorBase fromActor, ref bool __result ) {
         if ( fromActor?.IsDead == true ) {
            __result = false;
            return false;
         }
         return true;
      }
   }
}
