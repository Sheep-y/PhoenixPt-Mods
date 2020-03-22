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
   public class ModConfig {
      public int  Config_Version = 20200322;
      public bool Skip_Logos = true;
      public bool Skip_HottestYear = true;
      public bool Skip_Landings = true;
      public bool Skip_CurtainDrop = true;
      public bool Skip_CurtainLift = true;
   }

   public class Mod : ZyMod {
      // PPML v0.1 entry point
      public static void Init () => new Mod().SplashMod();

      // Modnix entry point, splash phase
      public void SplashMod ( ModConfig config = null, Action< SourceLevels, object, object[] > logger = null ) {
         SetLogger( logger );
         config = ReadSettings( config );

         // I prefer doing manual patch for better control, such as reusing a method in patch.
         // Most modders prefer the simpler Harmony Attributes / Annotation, and it'll work the same.

         // Skip logos and splash
         if ( config.Skip_Logos )
            LogoPatch = Patch( typeof( PhoenixGame ), "RunGameLevel", nameof( BeforeRunGameLevel_Skip ) );

         // Skip "The Hottest Year"
         if ( config.Skip_HottestYear ) {
            IntroEnterPatch = Patch( typeof( UIStateHomeScreenCutscene ), "EnterState", postfix: nameof( AfterHomeCutscene_Skip ) );
            IntroLoadPatch  = Patch( typeof( UIStateHomeScreenCutscene ), "PlayCutsceneOnFinishedLoad", nameof( BeforeOnLoad_Skip ) );
         }

         // Skip aircraft landings
         if ( config.Skip_Landings ) {
            Patch( typeof( UIStateTacticalCutscene ), "EnterState", postfix: nameof( AfterTacCutscene_Skip ) );
            Patch( typeof( UIStateTacticalCutscene ), "PlayCutsceneOnFinishedLoad", nameof( BeforeOnLoad_Skip ) );
         }

         // Skip curtain drop (opening loading screen)
         if ( config.Skip_CurtainDrop )
            Patch( typeof( UseInkUI ), "Open", nameof( InkOpen_Skip ) );
         // Skip curtain lift (closing loading screen)
         if ( config.Skip_CurtainLift )
            Patch( typeof( UseInkUI ), "Close", nameof( InkClose_Skip ) );
      }

      // For manual unpatching
      private static IPatch LogoPatch, IntroEnterPatch, IntroLoadPatch;

      private static bool ShouldSkip ( VideoPlaybackSourceDef def ) {
         if ( def == null ) return false;
         string path = def.ResourcePath;
         Verbo( "Checking video {0}", path );
         return path.Contains( "Game_Intro_Cutscene" ) || path.Contains( "LandingSequences" );
      }

      private static bool BeforeRunGameLevel_Skip ( PhoenixGame __instance, LevelSceneBinding levelSceneBinding, ref IEnumerator<NextUpdate> __result ) { try {
         if ( levelSceneBinding != __instance.Def.IntroLevelSceneDef.Binding ) return true;
         Info( "Skipping Logos" );
         __result = Enumerable.Empty<NextUpdate>().GetEnumerator();
         Unpatch( ref LogoPatch );
         return false;
      } catch ( Exception ex ) { return Error( ex ); } }

      private static void AfterHomeCutscene_Skip ( UIStateHomeScreenCutscene __instance, VideoPlaybackSourceDef ____sourcePlaybackDef ) { try {
         if ( ! ShouldSkip( ____sourcePlaybackDef ) ) return;
         Info( "Skipping Intro" );
         typeof( UIStateHomeScreenCutscene ).GetMethod( "OnCancel", NonPublic | Instance ).Invoke( __instance, null );
         Info( "Intro skipped. Unpatching home cutscene." );
         Unpatch( ref IntroEnterPatch );
         Unpatch( ref IntroLoadPatch );
      } catch ( Exception ex ) { Error( ex ); } }

      private static void AfterTacCutscene_Skip ( UIStateTacticalCutscene __instance, VideoPlaybackSourceDef ____sourcePlaybackDef ) { try {
         if ( ! ShouldSkip( ____sourcePlaybackDef ) ) return;
         Info( "Skipping Combat Video" );
         typeof( UIStateTacticalCutscene ).GetMethod( "OnCancel", NonPublic | Instance )?.Invoke( __instance, null );
      } catch ( Exception ex ) { Error( ex ); } }

      private static bool BeforeOnLoad_Skip ( VideoPlaybackSourceDef ____sourcePlaybackDef ) {
         return ! ShouldSkip( ____sourcePlaybackDef );
      }

      private const float SKIP_FRAME = 0.00001f;

      private static void InkOpen_Skip ( ref float ____progress, object __instance ) { try {
         float ____endFrame = (float) typeof( UseInkUI ).GetProperty( "_endFrame", NonPublic | Instance ).GetValue( __instance );
         var target = ____endFrame - SKIP_FRAME;
         if ( ____progress < target ) {
            Verbo( "Skipping Curtain Drop" );
            ____progress = target;
         }
      } catch ( Exception ex ) { Error( ex ); } }

      private static void InkClose_Skip ( ref float ____progress ) {
         if ( ____progress > SKIP_FRAME ) {
            Verbo( "Skipping Curtain Lift" );
            ____progress = SKIP_FRAME;
         }
      }
   }
}