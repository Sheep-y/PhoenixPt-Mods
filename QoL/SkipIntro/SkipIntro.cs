using Base.Core;
using Base.Levels;
using Base.UI;
using Base.UI.VideoPlayback;
using PhoenixPoint.Common.Game;
using PhoenixPoint.Home.View.ViewStates;
using PhoenixPoint.Geoscape.View.ViewStates;
using PhoenixPoint.Tactical.View.ViewStates;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Diagnostics;
using static System.Reflection.BindingFlags;

namespace Sheepy.PhoenixPt.SkipIntro {

   // Modnix will save and load settings to/from {dll_name}.conf as utf-8 json
   public class ModConfig {
      public int  Config_Version = 20200322;
      public bool Skip_Logos = true;
      public bool Skip_HottestYear = true;
      public bool Skip_NewGameIntro = false;
      public bool Skip_Landings = true;
      public bool Skip_CurtainDrop = true;
      public bool Skip_CurtainLift = true;

      internal bool SkipAnyVideo => Skip_HottestYear || Skip_NewGameIntro || Skip_Landings;
   }

   public class Mod : ZyMod {
      // PPML v0.1 entry point
      public static void Init () => new Mod().SplashMod();

      private static ModConfig Config;

      // Modnix entry point, splash phase
      public void SplashMod ( Func< string, object, object > api = null ) {
         SetApi( api, out Config );

         // I prefer doing manual patch for better control, such as reusing methods, configurable patch flow, or try catch optional patches.
         // Simpler mods can use Harmony Attributes / Annotation, and it'll work the same.

         // Skip logos and splash
         if ( Config.Skip_Logos ) {
            LogoPatch = TryPatch( typeof( PhoenixGame ), "RunGameLevel", nameof( BeforeRunGameLevel_Skip ) );
            if ( LogoPatch == null ) Config.Skip_Logos = false; // Allow video to be loaded, ditto below
         }

         // Skip "The Hottest Year"
         if ( Config.Skip_HottestYear ) {
            IntroEnterPatch = TryPatch( typeof( UIStateHomeScreenCutscene ), "EnterState", postfix: nameof( AfterHomeCutscene_Skip ) );
            if ( IntroEnterPatch == null ) Config.Skip_HottestYear = false;
         }

         // Skip New Campaign Video
         if ( Config.Skip_NewGameIntro ) {
            if ( TryPatch( typeof( UIStateGeoCutscene ), "EnterState", postfix: nameof( AfterGeoCutscene_Skip ) ) == null )
               Config.Skip_NewGameIntro = false;
         }

         // Skip aircraft landings
         if ( Config.Skip_Landings )
            TryPatch( typeof( UIStateTacticalCutscene ), "EnterState", postfix: nameof( AfterTacCutscene_Skip ) );

         // Don't load skipped videos
         if ( Config.SkipAnyVideo )
            TryPatch( typeof( VideoPlaybackController ), "Setup", nameof( BeforeVideoSetup_Skip ) );

         // Skip curtain drop (opening loading screen)
         if ( Config.Skip_CurtainDrop )
            TryPatch( typeof( UseInkUI ), "Open", nameof( InkOpen_Skip ) );
         // Skip curtain lift (closing loading screen)
         if ( Config.Skip_CurtainLift )
            TryPatch( typeof( UseInkUI ), "Close", nameof( InkClose_Skip ) );
      }

      // For manual unpatching
      private static IPatch LogoPatch, IntroEnterPatch;

      private static bool ShouldSkip ( VideoPlaybackSourceDef def ) {
         if ( def == null ) return false;
         string path = def.ResourcePath;
         Verbo( "Checking video {0}", path );
         if ( Config.Skip_Landings && path.Contains( "LandingSequences" ) ) return true;
         if ( Config.Skip_NewGameIntro && path.Contains( "PP_Intro_Cutscene" ) ) return true;
         if ( Config.Skip_HottestYear && path.Contains( "Game_Intro_Cutscene" ) ) return true;
         return false;
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
         Config.Skip_HottestYear = false; // Allows video to be loaded after unpatch
      } catch ( Exception ex ) { Error( ex ); } }

      private static void AfterGeoCutscene_Skip ( UIStateGeoCutscene __instance, VideoPlaybackSourceDef ____sourcePlaybackDef ) { try {
         if ( ! ShouldSkip( ____sourcePlaybackDef ) ) return;
         Info( "Skipping New Game Intro" );
         typeof( UIStateGeoCutscene ).GetMethod( "OnCancel", NonPublic | Instance )?.Invoke( __instance, null );
      } catch ( Exception ex ) { Error( ex ); } }

      private static void AfterTacCutscene_Skip ( UIStateTacticalCutscene __instance, VideoPlaybackSourceDef ____sourcePlaybackDef ) { try {
         if ( ! ShouldSkip( ____sourcePlaybackDef ) ) return;
         Info( "Skipping Combat Video" );
         typeof( UIStateTacticalCutscene ).GetMethod( "OnCancel", NonPublic | Instance )?.Invoke( __instance, null );
      } catch ( Exception ex ) { Error( ex ); } }

      private static bool BeforeVideoSetup_Skip ( VideoPlaybackController __instance ) {
         return ! ShouldSkip( __instance.PlaybackSource );
      }

      private const float SKIP_FRAME = 0.00001f;

      private static void InkOpen_Skip ( ref float ____progress, object __instance ) { try {
         float ____endFrame = (float) typeof( UseInkUI ).GetProperty( "_endFrame", NonPublic | Instance ).GetValue( __instance );
         var target = ____endFrame - SKIP_FRAME;
         if ( ____progress >= target ) return;
         Verbo( "Skipping Curtain Drop" );
         ____progress = target;
      } catch ( Exception ex ) { Error( ex ); } }

      private static void InkClose_Skip ( ref float ____progress ) {
         if ( ____progress <= SKIP_FRAME ) return;
         Verbo( "Skipping Curtain Lift" );
         ____progress = SKIP_FRAME;
      }
   }
}