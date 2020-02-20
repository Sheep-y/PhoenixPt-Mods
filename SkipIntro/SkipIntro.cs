using System;
using System.Reflection;

namespace Sheepy.PhoenixPt_SkipIntro {
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
   using static System.Reflection.BindingFlags;

   public class Mod {
      private static Logger Log = new Logger( "Mods/SheepyMods.log" );

      public static void Init () {
         HarmonyInstance harmony = HarmonyInstance.Create( typeof( Mod ).Namespace );
         
         Patch( harmony, typeof( PhoenixGame ), "Initialize", "Prefix_Init" );
         Patch( harmony, typeof( PhoenixGame ), "BootCrt", "Prefix_Boot" );
         Patch( harmony, typeof( VideoPlaybackController ), "Play", null, "Postfix_Play" );
         //Patch( harmony, typeof( UIStateInitial ).GetMethod( "EnterState", NonPublic | Instance ), typeof( Mod ).GetMethod( "Prefix_Initial" ) );

         Patch( harmony, typeof( UIStateHomeScreenCutscene ), "EnterState", null, "AfterHomeCutscene_Skip");
         Patch( harmony, typeof( UIStateHomeScreenCutscene ), "PlayCutsceneOnFinishedLoad", "BeforeOnLoad_Skip" );

         Patch( harmony, typeof( UIStateTacticalCutscene ), "EnterState", null, "AfterTacCutscene_Skip" );
         Patch( harmony, typeof( UIStateTacticalCutscene ), "PlayCutsceneOnFinishedLoad", "BeforeOnLoad_Skip" );

         Patch( harmony, typeof( LevelSwitchCurtainController ), "DropCurtainCrt", "BeforeDropCurtain_Skip" );
         // Disabled because it is a hassle to call OnCurtainLifted
         //Patch( harmony, typeof( LevelSwitchCurtainController ).GetMethod( "LiftCurtainCrt", Public | Instance ), typeof( Mod ).GetMethod( "Prefix_LiftCurtain" ) );
      }

      #region Modding helpers
      private static void Patch ( HarmonyInstance harmony, Type target, string toPatch, string prefix, string postfix = null ) {
         Patch( harmony, target.GetMethod( toPatch, Public | NonPublic | Instance | Static ), prefix, postfix );
      }

      private static void Patch ( HarmonyInstance harmony, MethodInfo toPatch, string prefix, string postfix = null ) {
         harmony.Patch( toPatch, ToHarmonyMethod( prefix ), ToHarmonyMethod( postfix ) );
      }

      private static HarmonyMethod ToHarmonyMethod ( string name ) {
         if ( name == null ) return null;
         MethodInfo func = typeof( Mod ).GetMethod( name );
         if ( func == null ) throw new NullReferenceException( name + " is null" );
         return new HarmonyMethod( func );
      }

      private static bool LogError ( Exception ex ) {
         //Log.Error( ex );
         return true;
      }
      #endregion

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

      public static void AfterHomeCutscene_Skip ( UIStateHomeScreenCutscene __instance, VideoPlaybackSourceDef ____sourcePlaybackDef ) { try {
         //UIModuleCutscenesPlayer player = (UIModuleCutscenesPlayer) typeof( UIStateHomeScreenCutscene ).GetProperty( "_cutscenePlayer", NonPublic | Instance ).GetValue( __instance );
         //player?.VideoPlayer?.Stop();
         if ( ShouldSkip( ____sourcePlaybackDef ) )
            typeof( UIStateHomeScreenCutscene ).GetMethod( "OnCancel", NonPublic | Instance )?.Invoke( __instance, null );
      } catch ( Exception ex ) { LogError( ex ); } }

      public static void AfterTacCutscene_Skip ( UIStateTacticalCutscene __instance, VideoPlaybackSourceDef ____sourcePlaybackDef ) { try {
         if ( ShouldSkip( ____sourcePlaybackDef ) )
            typeof( UIStateTacticalCutscene ).GetMethod( "OnCancel", NonPublic | Instance )?.Invoke( __instance, null );
      } catch ( Exception ex ) { LogError( ex ); } }

      public static bool BeforeOnLoad_Skip ( VideoPlaybackSourceDef ____sourcePlaybackDef ) {
         return ! ShouldSkip( ____sourcePlaybackDef );
      }

      public static bool BeforeDropCurtain_Skip ( ref IEnumerator<NextUpdate> __result, Action action, ref IUpdateable ____currentFadingRoutine ) { try {
         ____currentFadingRoutine?.Stop();
			action?.Invoke();
         ____currentFadingRoutine = null;
         __result = Enumerable.Empty<NextUpdate>().GetEnumerator();
         return false;
      } catch ( Exception ex ) { return LogError( ex ); } }

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
}