using Base.Utils.GameConsole;
using Harmony;
using PhoenixPoint.Home.View.ViewModules;
using PhoenixPoint.Home.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.UI;

namespace VersionText {

   /* Mod config class, saved as json through Newtonsoft Json.Net through Modnix api.
    * The config class must be specfied as "ConfigType" in mod_info. 
    * The initial values will be used as the default values. */
   internal class ModConfig {
      public int ConfigVersion = 2; // Having a "version" helps when you need to migrate from old config.
      public string Text { internal get; set; } // This prop is legacy; it can be read (set) by Json.Net, but will not be saved (get).
      public string VersionText = "Hello";
      public string BuildText = "World!";

      internal ModConfig Update () {
         if ( ConfigVersion < 2 ) { // In this case we are moving Text to VersionText as a demo.
            VersionText = Text;     // An alternative would be to split it between the two new fields.
            BuildText = "";
            ConfigVersion = 2;      // Don't forget to update version!
            Mod.Api?.Invoke( "config save", this ); // or to save the updated config!
         }
         return this;
      }
   }

   /* Main mod class. The initialisers are here.
    * Make sure it is public, non-abstract and non-nested. */
   public static class Mod {

      internal static Func<string,object,object> Api; // Modnix api function, used by 3 out of 4 mod classes.
      internal static ModConfig Config;               // Mod config, the whole mod need this to work.

      public static void Init () => MainMod(); // PPML 0.1 and 0.3+ entry point. 0.2 not supported because of conflict.

      /* Modnix entry point, Main Menu phase */
      public static void MainMod ( Func<string, object, object> api = null ) {
         Api = api; // Below: read config from Modnix and update it, or use default config.
         Config = ( api?.Invoke( "config", typeof( ModConfig ) ) as ModConfig )?.Update() ?? new ModConfig();
         HarmonyInstance.Create( typeof( Mod ).Namespace ).PatchAll();
         api?.Invoke( "api_add Sheepy.VerText", (Func<string, object, string>) ApiVerText );
      }

      /* Modnix API extension, registered in MainMod.
       * Default: set version text.  With "build" specifier: set build text. */
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

      /* Console command.  Depends on DebugConsole to load.  PPML 0.3+ may or may not work. */
      [ConsoleCommand( Command = "VerText", Description = "Set home screen version text." )]
      public static void ConsoleVerText ( IConsole console, string ver, string build ) {
         console.WriteLine( Config.VersionText + " " + Config.BuildText );
         Config.VersionText = ver?.ToString();
         Config.BuildText = build?.ToString();
         UIModuleBuildRevision_SetRevisionNumber.RefreshText();
      }
   }

   /* Main patch.  For changing the version text. */
   [HarmonyPatch( typeof( UIModuleBuildRevision ), "SetRevisionNumber" )]
   internal static class UIModuleBuildRevision_SetRevisionNumber {

      // This is the version text component, a Unity Text.
      private static Text VersionText;

      // Save a copy of the text componet and change it.
      private static void Postfix ( UIModuleBuildRevision __instance ) {
         VersionText = __instance.BuildRevisionNumber;
         RefreshText();
      }

      // Actually change the text.  Also used by API extension and console command.
      internal static void RefreshText () {
         // Abort if a change was requested before Postfix, e.g. use types a console command before main screen.
         // Unity References are required to compare any unity component with null.
         if ( VersionText == null ) return;
         VersionText.text = Mod.Config.VersionText + " " + Mod.Config.BuildText;
         UIStateMainMenu_EnterState.Refresh();
      }
   }

   /* Secondary patch.  For DebugConsole compatibility. */
   [HarmonyPatch( typeof( UIStateMainMenu ), "EnterState" )]
   internal static class UIStateMainMenu_EnterState {

      private static UIStateMainMenu MainMenu; // Copy of home screen for DebugConsole call.
      private static MethodBase DebugConsoleAdder; // DebugConsole method to call.

      // Save a copy of the home screen and get mod method through reflection.
      private static void Postfix ( UIStateMainMenu __instance ) {
         try {
            var asm = Mod.Api?.Invoke( "assembly", "Sheepy.DebugConsole" ) as Assembly;
            if ( asm == null ) return; // Abort if mod is not found.  e.g. When using PPML.
            MainMenu = __instance;
            DebugConsoleAdder = asm.GetType( "Sheepy.PhoenixPt.DebugConsole.Mod" )
               .GetMethod( "AfterMainMenu_AddModCount", BindingFlags.NonPublic | BindingFlags.Static );
         } catch ( Exception ex ) {
            // Duplicate exceptions will be ignored, when it is logged as the solo parameter.
            Mod.Api?.Invoke( "log error", ex ); // "error" specifier is unnecessary, but makes our intention clear.
         }
      }

      // Ask DebugConsole to do its job. Does not respect whether it is disabled, though.
      internal static void Refresh () => DebugConsoleAdder?.Invoke( null, new object[] { MainMenu } );
   }
}
