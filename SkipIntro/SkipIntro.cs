using Base.Core;
using Base.UI.VideoPlayback;
using Base.Utils;
using Harmony;
using PhoenixPoint.Common.Game;
using PhoenixPoint.Home.View.ViewStates;
using PhoenixPoint.Tactical.View.ViewStates;
using Sheepy.Logging;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Video;
using System;
using System.Reflection;
using static System.Reflection.BindingFlags;
using Base.Utils.GameConsole;
using System.Globalization;


namespace Sheepy.PhoenixPt_SkipIntro {

   public class Mod {
      private static readonly Logger Log = new Logger( "Mods/SheepyMods.log" );

      public static void Init () {
         HarmonyInstance.Create( typeof( Mod ).Namespace ).PatchAll();
         /*
         Patch( harmony, typeof( PhoenixGame ), "Initialize", "Prefix_Init" );
         Patch( harmony, typeof( PhoenixGame ), "BootCrt", "Prefix_Boot" );
         Patch( harmony, typeof( VideoPlaybackController ), "Play", null, "Postfix_Play" );
         //Patch( harmony, typeof( UIStateInitial ).GetMethod( "EnterState", NonPublic | Instance ), typeof( Mod ).GetMethod( "Prefix_Initial" ) );
         */
         // Disabled because it is a hassle to call OnCurtainLifted
         //Patch( harmony, typeof( LevelSwitchCurtainController ).GetMethod( "LiftCurtainCrt", Public | Instance ), typeof( Mod ).GetMethod( "Prefix_LiftCurtain" ) );
      }

      public static bool LogError ( Exception ex ) {
         //Log.Error( ex );
         return true;
      }

      public static void Prefix_Init () { Log.Info( "Game Init" ); }
      public static void Prefix_Boot () { Log.Info( "Game Boot" ); }
      public static void Postfix_Play ( VideoPlaybackController __instance ) {
         VideoPlayer vid = __instance.VideoPlayer;
         Log.Info( vid.source == VideoSource.Url ? vid.url : vid.clip.originalPath );
         Log.Info( Logger.Stacktrace );
      }

      public static bool ShouldSkip ( VideoPlaybackSourceDef def ) {
         string path = def.ResourcePath;
         return path.Contains( "Game_Intro_Cutscene" ) || path.Contains( "LandingSequences" );
      }

      /*
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
   }

   [HarmonyPatch( typeof( UIStateHomeScreenCutscene ), "EnterState" )]
   public static class UIStateHomeScreenCutscene_EnterState {
      static void Postfix ( UIStateHomeScreenCutscene __instance, VideoPlaybackSourceDef ____sourcePlaybackDef ) { try {
         //UIModuleCutscenesPlayer player = (UIModuleCutscenesPlayer) typeof( UIStateHomeScreenCutscene ).GetProperty( "_cutscenePlayer", NonPublic | Instance ).GetValue( __instance );
         //player?.VideoPlayer?.Stop();
         if ( Mod.ShouldSkip( ____sourcePlaybackDef ) )
            typeof( UIStateHomeScreenCutscene ).GetMethod( "OnCancel", NonPublic | Instance )?.Invoke( __instance, null );
      } catch ( Exception ex ) { Mod.LogError( ex ); } }
   }

   [HarmonyPatch( typeof( UIStateHomeScreenCutscene ), "PlayCutsceneOnFinishedLoad" )]
   public static class UIStateHomeScreenCutscene_PlayCutsceneOnFinishedLoad {
      static bool Prefix  ( VideoPlaybackSourceDef ____sourcePlaybackDef ) {
         return ! Mod.ShouldSkip( ____sourcePlaybackDef );
      }
   }

   [HarmonyPatch( typeof( UIStateTacticalCutscene ), "EnterState" )]
   public static class UIStateTacticalCutscene_EnterState {
      static void Postfix ( UIStateTacticalCutscene __instance, VideoPlaybackSourceDef ____sourcePlaybackDef ) { try {
         if ( Mod.ShouldSkip( ____sourcePlaybackDef ) )
            typeof( UIStateTacticalCutscene ).GetMethod( "OnCancel", NonPublic | Instance )?.Invoke( __instance, null );
      } catch ( Exception ex ) { Mod.LogError( ex ); } }
   }

   [HarmonyPatch( typeof( UIStateTacticalCutscene ), "PlayCutsceneOnFinishedLoad" )]
   public static class UIStateTacticalCutscene_PlayCutsceneOnFinishedLoad {
      static bool Prefix ( VideoPlaybackSourceDef ____sourcePlaybackDef ) {
         return ! Mod.ShouldSkip( ____sourcePlaybackDef );
      }
   }

   [HarmonyPatch( typeof( LevelSwitchCurtainController ), "DropCurtainCrt" )]
   public static class LevelSwitchCurtainController_DropCurtainCrt {
      static bool Prefix ( ref IEnumerator<NextUpdate> __result, Action action, ref IUpdateable ____currentFadingRoutine ) { try {
         ____currentFadingRoutine?.Stop();
         action?.Invoke();
         ____currentFadingRoutine = null;
         __result = Enumerable.Empty<NextUpdate>().GetEnumerator();
         return false;
      } catch ( Exception ex ) { return Mod.LogError( ex ); } }
   }
}