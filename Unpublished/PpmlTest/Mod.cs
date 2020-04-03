using Base.Build;
using Harmony;
using PhoenixPointModLoader;
using PhoenixPointModLoader.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sheepy.PhoenixPt.PpmlTest {
   public class Mod : IPhoenixPointMod {
      public ModLoadPriority Priority => ModLoadPriority.Normal;
      public void Initialize() {
         HarmonyInstance.Create( typeof( Mod ).Namespace ).PatchAll();
      }
   }

   [HarmonyPatch( typeof( RuntimeBuildInfo ), "Version", MethodType.Getter )]
   static class RuntimeBuildInfoMod_GetVersion {
      static void Postfix ( ref string __result ) => __result = "PPML";
   }

   [HarmonyPatch( typeof( RuntimeBuildInfo ), "Platform", MethodType.Getter )]
   static class RuntimeBuildInfoMod_GetPlatform {
      static void Postfix ( ref string __result ) => __result = "Working";
   }
}
