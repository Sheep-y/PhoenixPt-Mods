using Base.Build;
using Base.Utils.GameConsole;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionText {
   internal class ModConfig {
      public int ConfigVersion = 2;
      public string Text { internal get; set; }
      public string VersionText = "Hello";
      public string BuildText = "World!";

      internal ModConfig Update () {
         if ( ConfigVersion < 2 ) {
            VersionText = Text;
            BuildText = "";
            ConfigVersion = 2;
            Mod.Api?.Invoke( "config save", this );
         }
         return this;
      }
   }
   public static class Mod {
      internal static Func<string,object,object> Api;
      internal static ModConfig Config;
      public static void Init () => MainMod();
      public static void MainMod ( Func<string,object,object> api = null ) {
         Api = api;
         Config = ( api?.Invoke( "config", typeof( ModConfig ) ) as ModConfig )?.Update() ?? new ModConfig();
         HarmonyInstance.Create( typeof( Mod ).Namespace ).PatchAll();
      }

      [ ConsoleCommand( Command = "VerText", Description = "Change version text" ) ]
      static void ChangeText ( IConsole console, string ver, string build ) {
         Mod.Config.VersionText = ver;
         Mod.Config.BuildText = build;
      }
   }

   [ HarmonyPatch( typeof( RuntimeBuildInfo ), "Version", MethodType.Getter ) ]
   static class RuntimeBuildInfoMod_GetVersion {
      static void Postfix ( ref string __result ) => __result = Mod.Config.VersionText;
   } 

   [ HarmonyPatch( typeof( RuntimeBuildInfo ), "Platform", MethodType.Getter ) ]
   static class RuntimeBuildInfoMod_GetPlatform {
      static void Postfix ( ref string __result ) => __result = Mod.Config.BuildText;
   }
}
