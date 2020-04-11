using Base.Utils.GameConsole;
using Harmony;
using PhoenixPoint.Home.View.ViewModules;
using PhoenixPoint.Home.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;

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
         api?.Invoke( "api_add Sheepy.VerText", (Func<string,object,string>) ApiVerText );
      }

      private static string ApiVerText ( string type, object arg ) {
         string old;
         if ( "build".Equals( type?.ToLowerInvariant() ) ) {
            old = Config.BuildText;
            Config.BuildText = arg?.ToString();
         } else {
            old = Config.VersionText;
            Config.VersionText = arg?.ToString();
         }
         UIModuleBuildRevision_SetRevisionNumber.RefreshText();
         return old;
      }

      [ ConsoleCommand( Command = "VerText", Description = "Set home screen version text." ) ]
      public static void ConsoleVerText ( IConsole console, string ver, string build ) {
         console.WriteLine( Config.VersionText + " " + Config.BuildText );
         Config.VersionText = ver?.ToString();
         Config.BuildText = build?.ToString();
         UIModuleBuildRevision_SetRevisionNumber.RefreshText();
      }
   }

   [ HarmonyPatch( typeof( UIModuleBuildRevision ), "SetRevisionNumber" ) ]
   static class UIModuleBuildRevision_SetRevisionNumber {
      private static Text VersionText;
      static void Postfix ( UIModuleBuildRevision __instance ) {
         VersionText = __instance.BuildRevisionNumber;
         RefreshText();
      }

      internal static void RefreshText () {
         VersionText.text = Mod.Config.VersionText + " " + Mod.Config.BuildText;
         UIStateMainMenu_EnterState.Refresh();
      }
   }

   [ HarmonyPatch( typeof( UIStateMainMenu ), "EnterState" ) ]
   internal static class UIStateMainMenu_EnterState {
      private static UIStateMainMenu MainMenu;
      private static MethodBase DebugConsoleAdder;

      private static void Postfix ( UIStateMainMenu __instance ) {
         try {
            var asm = Mod.Api?.Invoke( "assembly", "Sheepy.DebugConsole" ) as Assembly;
            if ( asm == null ) return;
            MainMenu = __instance;
            DebugConsoleAdder = asm.GetType( "Sheepy.PhoenixPt.DebugConsole.Mod" )
               .GetMethod( "AfterMainMenu_AddModCount", BindingFlags.NonPublic | BindingFlags.Static );
         } catch ( Exception ex ) {
            Mod.Api?.Invoke( "log error", ex );
         }
      }

      internal static void Refresh () => DebugConsoleAdder?.Invoke( null, new object[] { MainMenu } );
   }
}
