using Base.Core;
using Base.Levels;
using Base.UI;
using Base.UI.VideoPlayback;
using PhoenixPoint.Common.Game;
using PhoenixPoint.Home.View.ViewStates;
using PhoenixPoint.Tactical.View.ViewStates;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Diagnostics;
using System.Reflection;
using static System.Reflection.BindingFlags;

namespace Sheepy.PhoenixPt.SkipIntro {

   // Modnix will save and load settings to/from {dll_name}.conf as utf-8 json
   public class ModSettings {
      public string Settings_Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
      public bool Skip_Logos = true;
      public bool Skip_HottestYear = true;
      public bool Skip_Landings = true;
      public bool Skip_CurtainDrop = true;
      public bool Skip_CurtainLift = true;
   }

   public class Mod : SheepyMod {
      // Tell Modnix what type our settings is, and its default values
      public static ModSettings DefaultSettings => new ModSettings();

      // PPML v0.1 entry point
      public static void Init () => new Mod().SplashMod();

      // Modnis entry point, splash phase
      public void SplashMod ( ModSettings settings = null, Action< SourceLevels, object, object[] > logger = null ) {
         if ( settings == null ) settings = DefaultSettings;
         SetLogger( logger );

         // I prefer doing manual patch for better control, such as reusing a method in patch.
         // Most modders prefer the simpler Harmony Attributes / Annotation, and it'll work the same.

         // Skip logos and splash
         if ( settings.Skip_Logos )
            LogoPatch = Patch( typeof( PhoenixGame ), "RunGameLevel", nameof( BeforeRunGameLevel_Skip ) );

         // Skip "The Hottest Year"
         if ( settings.Skip_HottestYear ) {
            IntroEnterPatch = Patch( typeof( UIStateHomeScreenCutscene ), "EnterState", postfix: nameof( AfterHomeCutscene_Skip ) );
            IntroLoadPatch  = Patch( typeof( UIStateHomeScreenCutscene ), "PlayCutsceneOnFinishedLoad", nameof( BeforeOnLoad_Skip ) );
         }

         // Skip aircraft landings
         if ( settings.Skip_Landings ) {
            Patch( typeof( UIStateTacticalCutscene ), "EnterState", postfix: nameof( AfterTacCutscene_Skip ) );
            Patch( typeof( UIStateTacticalCutscene ), "PlayCutsceneOnFinishedLoad", nameof( BeforeOnLoad_Skip ) );
         }

         // Skip curtain drop (opening loading screen)
         if ( settings.Skip_CurtainDrop )
            Patch( typeof( UseInkUI ), "Open", nameof( InkOpen_Skip ) );
         // Skip curtain lift (closing loading screen)
         if ( settings.Skip_CurtainLift )
            Patch( typeof( UseInkUI ), "Close", nameof( InkClose_Skip ) );
      }

      // For manual unpatching
      private static IPatchRecord LogoPatch, IntroEnterPatch, IntroLoadPatch;

      public static bool ShouldSkip ( VideoPlaybackSourceDef def ) {
         if ( def == null ) return false;
         string path = def.ResourcePath;
         Verbo( "Checking video {0}", path );
         return path.Contains( "Game_Intro_Cutscene" ) || path.Contains( "LandingSequences" );
      }

      public static bool BeforeRunGameLevel_Skip ( PhoenixGame __instance, LevelSceneBinding levelSceneBinding, ref IEnumerator<NextUpdate> __result ) { try {
         if ( levelSceneBinding != __instance.Def.IntroLevelSceneDef.Binding ) return true;
         Info( "Skipping Logos" );
         __result = Enumerable.Empty<NextUpdate>().GetEnumerator();
         LogoPatch.Unpatch();
         return false;
      } catch ( Exception ex ) { return Error( ex ); } }

      public static void AfterHomeCutscene_Skip ( UIStateHomeScreenCutscene __instance, VideoPlaybackSourceDef ____sourcePlaybackDef ) { try {
         if ( ! ShouldSkip( ____sourcePlaybackDef ) ) return;
         Info( "Skipping Intro" );
         typeof( UIStateHomeScreenCutscene ).GetMethod( "OnCancel", NonPublic | Instance ).Invoke( __instance, null );
         Info( "Intro skipped. Unpatching home cutscene." );
         IntroEnterPatch.Unpatch();
         IntroLoadPatch.Unpatch();
      } catch ( Exception ex ) { Error( ex ); } }

      public static void AfterTacCutscene_Skip ( UIStateTacticalCutscene __instance, VideoPlaybackSourceDef ____sourcePlaybackDef ) { try {
         if ( ! ShouldSkip( ____sourcePlaybackDef ) ) return;
         Info( "Skipping Combat Video" );
         typeof( UIStateTacticalCutscene ).GetMethod( "OnCancel", NonPublic | Instance )?.Invoke( __instance, null );
      } catch ( Exception ex ) { Error( ex ); } }

      public static bool BeforeOnLoad_Skip ( VideoPlaybackSourceDef ____sourcePlaybackDef ) {
         return ! ShouldSkip( ____sourcePlaybackDef );
      }

      private const float SKIP_FRAME = 0.00001f;

      public static void InkOpen_Skip ( ref float ____progress, object __instance ) { try {
         Verbo( "Skipping Curtain Drop" );
         float ____endFrame = (float) typeof( UseInkUI ).GetProperty( "_endFrame", NonPublic | Instance ).GetValue( __instance );
         var target = ____endFrame - SKIP_FRAME;
         if ( ____progress < target )
            ____progress = target;
      } catch ( Exception ex ) { Error( ex ); } }

      public static void InkClose_Skip ( ref float ____progress ) {
         Verbo( "Skipping Curtain Lift" );
         if ( ____progress > SKIP_FRAME )
            ____progress = SKIP_FRAME;
      }

      #region Modding helpers
      #endregion
   }
}