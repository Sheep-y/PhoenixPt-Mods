using Base.Build;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionText {

   internal class ModConfig {
      public string Text = "Hello World!";
   }

   public static class Mod {
      internal static ModConfig Config;
      public static void MainMod ( Func<string,object,object> api ) {
         Config = api( "config", typeof( ModConfig ) ) as ModConfig;
         HarmonyInstance.Create( typeof( Mod ).Namespace ).PatchAll();
      }
   }

   [HarmonyPatch( typeof( RuntimeBuildInfo ), "Version", MethodType.Getter )]
   static class RuntimeBuildInfoMod_GetVersion {
      static void Postfix ( ref string __result ) =>
         __result = Mod.Config.Text;
   }

   [HarmonyPatch( typeof( RuntimeBuildInfo ), "Platform", MethodType.Getter )]
   static class RuntimeBuildInfoMod_GetPlatform {
      static void Postfix ( ref string __result ) =>
         __result = "";
   }
}