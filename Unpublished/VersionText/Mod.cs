using Base.Utils.GameConsole;
using Harmony;
using PhoenixPoint.Home.View.ViewModules;
using PhoenixPoint.Home.View.ViewStates;
using System;
using System.Reflection;
using UnityEngine.UI;

// Mod tested on Phoenix Point 1.0.57335 / Modnix 2.5 / Debug Console 5.0.2
namespace Sheepy {

   /* Mod config class, saved as json through Modnix api, which calls Newtonsoft Json.NET.
    * The config class must be specfied as "ConfigType" in mod_info. 
    * Its initial values will be used as defaults. */
   internal class ModConfig {
      public int ConfigVersion = 2; // Having a "version" helps when you need to migrate from old config.
      public string Text { internal get; set; } // This prop is legacy; it can be read (set) by Json.NET, but will not be saved (get).
      public string VersionText = "Hello";
      public string BuildText = "World!";

      internal ModConfig Update () {
         if ( ConfigVersion < 2 ) { // Updating config from ver 1.
            VersionText = Text;     // In this case we are moving Text to VersionText as a demo.
            BuildText = "";         // An alternative would be to split it between the two new fields.
            ConfigVersion = 2;      // Remember to update version!
            Mod.Api?.Invoke( "config save", this ); // And save the updated config!
         }
         return this;
      }
   }

   /* Main mod class. The initialisers are here.
    * Make sure it is public, non-abstract and non-nested. */
   public static class Mod {

      internal static volatile Func<string,object,object> Api; // Modnix api function, used by 3 out of 4 mod classes.
      internal static volatile ModConfig Config;               // Mod config, the patches need this to work.

      public static void Init () => MainMod(); // PPML 0.1 and 0.3+ entry point. (Doing 0.2 would crash the other two.)

      /* Primary Modnix entry point, before main menu */
      public static void MainMod ( Func<string, object, object> api = null ) {
         Api = api;
             // Use API to read   config   ( Modnix 1 syntax )               and update it, or use default config.
         Config = ( api?.Invoke( "config", typeof( ModConfig ) ) as ModConfig )?.Update() ?? new ModConfig();

         // Call Harmony to find all patch classes and apply them
         HarmonyInstance.Create( typeof( Mod ).Namespace ).PatchAll();

         // Register a new API if and only if patch success
         api?.Invoke( "api_add Sheepy.VerText", (Func<string, object, string>) ApiVerText );
      }

      /* Modnix API extension, registered in MainMod.
       * Default: set version text.  With "build" specifier: set build text.
       * Returns old text. Send null to get and refresh text, no set. */
      private static string ApiVerText ( string type, object arg ) {
         string old;
         if ( "build".Equals( type?.ToLowerInvariant() ) ) {
            old = Config.BuildText;
            if ( arg != null )
               Config.BuildText = arg?.ToString();
         } else {
            old = Config.VersionText;
            if ( arg != null )
               Config.VersionText = arg?.ToString();
         }
         UIModuleBuildRevision_SetRevisionNumber.RefreshText();
         return old;
      }

      /* Console command.  Requires DebugConsole.  PPML 0.3+ may also work, untested. */
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
      private static volatile Text VersionText;

      // Save a copy of the text componet and change it.
      private static void Postfix ( UIModuleBuildRevision __instance ) {
         VersionText = __instance.BuildRevisionNumber;
         RefreshText();
      }

      // Actually change the text.  Also used by API extension and console command.
      internal static void RefreshText () {
         // Abort if a change was requested before Postfix, e.g. console command is used before main menu.
         // Unity References are required to compare any unity component with null.
         if ( VersionText == null ) return;
         VersionText.text = Mod.Config.VersionText + " " + Mod.Config.BuildText;
         UIStateMainMenu_EnterState.Refresh();
      }
   }

   /* Secondary patch.  For DebugConsole compatibility. */
   [HarmonyPatch( typeof( UIStateMainMenu ), "EnterState" )]
   internal static class UIStateMainMenu_EnterState {

      private static volatile UIStateMainMenu MainMenu; // A copy of main menu for DebugConsole call.
      private static volatile MethodBase DebugConsoleAdder; // DebugConsole method to call.

      // Save a copy of the main menu, and get mod method through reflection.
      private static void Postfix ( UIStateMainMenu __instance ) {
         try {
            var asm = Mod.Api?.Invoke( "assembly", "Sheepy.DebugConsole" ) as Assembly;
            if ( asm == null ) return; // Abort if mod is not found.  e.g. When using PPML.
            MainMenu = __instance;
            // Vanilla reflection :
            // DebugConsoleAdder = asm.GetType( "Sheepy.PhoenixPt.DebugConsole.Mod" ).GetMethod( "AfterMainMenu_AddModCount", BindingFlags.NonPublic | BindingFlags.Static );
            // Harmony reflection helper :
            DebugConsoleAdder = AccessTools.Method( asm.GetType( "Sheepy.PhoenixPt.DebugConsole.Mod" ), "AfterMainMenu_AddModCount" );
         } catch ( Exception ex ) {
            // Log error to Modnix.  Modnix will highlight mods with runtime error to help debug.
            Mod.Api?.Invoke( "log error", ex ); // "error" specifier is unnecessary, but makes our intention clear.
            // Duplicate exceptions will be ignored, when it is logged as the solo parameter.
         }
      }

      // Ask DebugConsole to do its job. Does not respect whether it is disabled, though.
      internal static void Refresh () {
         try {
            // Yes dynamic invocation can throw all kind of errors.
            DebugConsoleAdder?.Invoke( null, new object[] { MainMenu } );
         } catch ( Exception ex ) {
            // Feel free to make this a helper method.
            Mod.Api?.Invoke( "log error", ex );
         }
      }
   }
}
