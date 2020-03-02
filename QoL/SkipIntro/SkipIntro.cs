using Base.Core;
using Base.Levels;
using Base.UI.VideoPlayback;
using Harmony;
using PhoenixPoint.Common.Game;
using PhoenixPoint.Home.View.ViewStates;
using PhoenixPoint.Tactical.View.ViewStates;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using static System.Reflection.BindingFlags;

namespace Sheepy.PhoenixPt_SkipIntro {
   
   public static class Mod {
      // PPML v0.1 entry point
      public static void Init () => SplashMod();

      // Modnis entry point, splash phase
      public static void SplashMod ( Action< SourceLevels, object, object[] > logger = null ) {
         SetLogger( logger );

         // I prefer doing manual patch for better control, such as reusing a method.
         // Most modders prefer the simpler Harmony Attributes / Annotation, and it'll work the same.

         // Skip logos and splash
         Patch( typeof( PhoenixGame ), "RunGameLevel", nameof( BeforeRunGameLevel_Skip ) );

         // Skip "The Hottest Year"
         Patch( typeof( UIStateHomeScreenCutscene ), "EnterState", postfix: nameof( AfterHomeCutscene_Skip ) );
         Patch( typeof( UIStateHomeScreenCutscene ), "PlayCutsceneOnFinishedLoad", nameof( BeforeOnLoad_Skip ) );

         // Skip aircraft landings
         Patch( typeof( UIStateTacticalCutscene ), "EnterState", postfix: nameof( AfterTacCutscene_Skip ) );
         Patch( typeof( UIStateTacticalCutscene ), "PlayCutsceneOnFinishedLoad", nameof( BeforeOnLoad_Skip ) );

         // Skip curtain drop
         //Patch( harmony, typeof( LevelSwitchCurtainController ), "DropCurtainCrt", "BeforeDropCurtain_Skip" );
         // Skip curtain raise; disabled because it is a hassle to call OnCurtainLifted
         //Patch( harmony, typeof( LevelSwitchCurtainController ).GetMethod( "LiftCurtainCrt", Public | Instance ), typeof( Mod ).GetMethod( "Prefix_LiftCurtain" ) );
      }

      public static bool ShouldSkip ( VideoPlaybackSourceDef def ) {
         string path = def.ResourcePath;
         Verbo( "Checking cutscene {0}", path );
         return path.Contains( "Game_Intro_Cutscene" ) || path.Contains( "LandingSequences" );
      }

      public static bool BeforeRunGameLevel_Skip ( PhoenixGame __instance, LevelSceneBinding levelSceneBinding, ref IEnumerator<NextUpdate> __result ) { try {
         if ( levelSceneBinding != __instance.Def.IntroLevelSceneDef.Binding ) return true;
         Info( "Skipping IntroLevel" );
         __result = Enumerable.Empty<NextUpdate>().GetEnumerator();
         return false;
      } catch ( Exception ex ) { return Error( ex ); } }

      public static void AfterHomeCutscene_Skip ( UIStateHomeScreenCutscene __instance, VideoPlaybackSourceDef ____sourcePlaybackDef ) { try {
         Info( ____sourcePlaybackDef.ResourcePath );
         if ( ShouldSkip( ____sourcePlaybackDef ) ) {
            typeof( UIStateHomeScreenCutscene ).GetMethod( "OnCancel", NonPublic | Instance )?.Invoke( __instance, null );
            Info( "Home intro skipped. Unpatching home cutscene." );
            harmony.Unpatch( 
               typeof( UIStateHomeScreenCutscene ).GetMethod( "EnterState",  Public | NonPublic | Instance | Static ),
               typeof( Mod ).GetMethod( nameof( AfterHomeCutscene_Skip ), Static | Public )
            );
            harmony.Unpatch( 
               typeof( UIStateHomeScreenCutscene ).GetMethod( "PlayCutsceneOnFinishedLoad",  Public | NonPublic | Instance | Static ),
               typeof( Mod ).GetMethod( nameof( BeforeOnLoad_Skip ), Static | Public )
            );
         }
      } catch ( Exception ex ) { Error( ex ); } }

      public static void AfterTacCutscene_Skip ( UIStateTacticalCutscene __instance, VideoPlaybackSourceDef ____sourcePlaybackDef ) { try {
         if ( ShouldSkip( ____sourcePlaybackDef ) )
            typeof( UIStateTacticalCutscene ).GetMethod( "OnCancel", NonPublic | Instance )?.Invoke( __instance, null );
      } catch ( Exception ex ) { Error( ex ); } }

      public static bool BeforeOnLoad_Skip ( VideoPlaybackSourceDef ____sourcePlaybackDef ) {
         return ! ShouldSkip( ____sourcePlaybackDef );
      }

      /*
      public static bool BeforeDropCurtain_Skip ( ref IEnumerator<NextUpdate> __result, Action action, ref IUpdateable ____currentFadingRoutine ) { try {
         ____currentFadingRoutine?.Stop();
         action?.Invoke();
         ____currentFadingRoutine = null;
         __result = Enumerable.Empty<NextUpdate>().GetEnumerator();
         return false;
      } catch ( Exception ex ) { return Error( ex ); } }

      public static bool Prefix_LiftCurtain ( ref IEnumerator<NextUpdate> __result, LevelSwitchCurtainController __instance ) { try {
         Action onCurtainLifted = __instance.OnCurtainLifted;
         onCurtainLifted?.Invoke();
         __result = Enumerable.Empty<NextUpdate>().GetEnumerator();
         return false;
      } catch ( Exception err ) {
         log.Error( err );
         return true;
      } }
      */

      #region Modding helpers
      private static HarmonyInstance harmony;

      private static void Patch ( Type target, string toPatch, string prefix = null, string postfix = null ) {
         Patch( target.GetMethod( toPatch, Public | NonPublic | Instance | Static ), prefix, postfix );
      }

      private static void Patch ( MethodInfo toPatch, string prefix = null, string postfix = null ) {
         if ( harmony == null ) harmony = HarmonyInstance.Create( typeof( Mod ).Namespace );
         Verbo( "Patching {0}.{1} with {2}:{3}", toPatch.DeclaringType, toPatch.Name, prefix, postfix );
         harmony.Patch( toPatch, ToHarmonyMethod( prefix ), ToHarmonyMethod( postfix ) );
      }

      private static HarmonyMethod ToHarmonyMethod ( string name ) {
         if ( name == null ) return null;
         MethodInfo func = typeof( Mod ).GetMethod( name );
         if ( func == null ) throw new NullReferenceException( name + " not found" );
         return new HarmonyMethod( func );
      }

      private static Action< SourceLevels, object, object[] > Logger;
      private static string LogFile;

      private static void SetLogger ( Action< SourceLevels, object, object[] > logger ) {
         if ( logger == null ) { // Use default
            Logger = DefaultLogger;
            Info( DateTime.Now.ToString( "D" ) + " " + typeof( Mod ).Namespace + " " + Assembly.GetExecutingAssembly().GetName().Version );
            LogFile = Regex.Replace( Assembly.GetExecutingAssembly().Location, "\\.dll$", ".log", RegexOptions.IgnoreCase );
         } else
            Logger = logger; // Logger provided by mod loader
      }

      // A simple file logger when one is not provided by the mod loader.
      private static void DefaultLogger ( SourceLevels lv, object msg, object[] param ) { try {
         string line = msg?.ToString();
         try {
            if ( param != null ) line = string.Format( line, param );
         } catch ( Exception ) { }
         using ( var stream = File.AppendText( LogFile ) ) {
            stream.WriteLineAsync( DateTime.Now.ToString( "T" ) + " " + typeof( Mod ).Namespace + " " + line );
         }  
      } catch ( Exception ex ) { Console.WriteLine( ex ); } }

      private static void Verbo ( object msg, params object[] augs ) => Logger?.Invoke( SourceLevels.Verbose, msg, augs );
      private static void Info  ( object msg, params object[] augs ) => Logger?.Invoke( SourceLevels.Information, msg, augs );
      private static void Warn  ( object msg, params object[] augs ) => Logger?.Invoke( SourceLevels.Warning, msg, augs );
      private static bool Error ( object msg, params object[] augs ) {
         Logger?.Invoke( SourceLevels.Error, msg, augs );
         return true;
      }
      #endregion
   }
}
