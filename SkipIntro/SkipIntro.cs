using Base.Core;
using Base.UI.VideoPlayback;
using Base.Utils;
using Harmony;
using PhoenixPoint.Common.Game;
using PhoenixPoint.Home.View.ViewStates;
using PhoenixPoint.Tactical.View.ViewStates;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Video;
using System;
using System.Reflection;
using static System.Reflection.BindingFlags;
using System.Diagnostics;
using System.IO;

namespace Sheepy.PhoenixPt_SkipIntro {

   public class Mod {
      private static Action< SourceLevels, object, object[] > Logger;

      public static void Init () => ModSplash( DefaultLogger );
      
      public static void ModSplash ( Action< SourceLevels, object, object[] > logger ) {
         Logger = logger;
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
      private static void Patch ( HarmonyInstance harmony, Type target, string toPatch, string prefix, string postfix = null ) =>
         Patch( harmony, target.GetMethod( toPatch, Public | NonPublic | Instance | Static ), prefix, postfix );

      private static void Patch ( HarmonyInstance harmony, MethodInfo toPatch, string prefix, string postfix = null ) =>
         harmony.Patch( toPatch, ToHarmonyMethod( prefix ), ToHarmonyMethod( postfix ) );

      private static HarmonyMethod ToHarmonyMethod ( string name ) {
         if ( name == null ) return null;
         MethodInfo func = typeof( Mod ).GetMethod( name );
         if ( func == null ) throw new NullReferenceException( name + " is null" );
         return new HarmonyMethod( func );
      }

      private static void Info ( object msg, params object[] augs ) =>
         Logger?.Invoke( SourceLevels.Information, msg, augs );

      private static bool Error ( object msg ) {
         Logger?.Invoke( SourceLevels.Error, msg, null );
         return true;
      }

      private static void DefaultLogger ( SourceLevels lv, object msg, object[] param ) { try {
         string line = msg?.ToString();
         try {
            if ( param != null ) line = string.Format( line, param );
         } catch ( Exception ) { }
         using ( var stream = File.AppendText( "Mods\\SheepyMods.log" ) ) {
            stream.WriteLineAsync( DateTime.Now.ToString( "T" ) + " " + typeof( Mod ).Namespace + " " + line );
         }  
      } catch ( Exception ex ) { Console.WriteLine( ex ); } }
      #endregion

      public static void Prefix_Init () { Info( "Game Init" ); }
      public static void Prefix_Boot () { Info( "Game Boot" ); }
      public static void Postfix_Play ( VideoPlaybackController __instance ) {
         VideoPlayer vid = __instance.VideoPlayer;
         Info( vid.source == VideoSource.Url ? vid.url : vid.clip.originalPath );
         Info( new Exception().StackTrace );
      }

      public static bool ShouldSkip ( VideoPlaybackSourceDef def ) {
         string path = def.ResourcePath;
         return path.Contains( "Game_Intro_Cutscene" ) || path.Contains( "LandingSequences" );
      }

      public static void AfterHomeCutscene_Skip ( UIStateHomeScreenCutscene __instance, VideoPlaybackSourceDef ____sourcePlaybackDef ) { try {
         Info( ____sourcePlaybackDef.ResourcePath );
         //UIModuleCutscenesPlayer player = (UIModuleCutscenesPlayer) typeof( UIStateHomeScreenCutscene ).GetProperty( "_cutscenePlayer", NonPublic | Instance ).GetValue( __instance );
         //player?.VideoPlayer?.Stop();
         if ( ShouldSkip( ____sourcePlaybackDef ) )
            typeof( UIStateHomeScreenCutscene ).GetMethod( "OnCancel", NonPublic | Instance )?.Invoke( __instance, null );
      } catch ( Exception ex ) { Error( ex ); } }

      public static void AfterTacCutscene_Skip ( UIStateTacticalCutscene __instance, VideoPlaybackSourceDef ____sourcePlaybackDef ) { try {
         if ( ShouldSkip( ____sourcePlaybackDef ) )
            typeof( UIStateTacticalCutscene ).GetMethod( "OnCancel", NonPublic | Instance )?.Invoke( __instance, null );
      } catch ( Exception ex ) { Error( ex ); } }

      public static bool BeforeOnLoad_Skip ( VideoPlaybackSourceDef ____sourcePlaybackDef ) {
         return ! ShouldSkip( ____sourcePlaybackDef );
      }

      public static bool BeforeDropCurtain_Skip ( ref IEnumerator<NextUpdate> __result, Action action, ref IUpdateable ____currentFadingRoutine ) { try {
         ____currentFadingRoutine?.Stop();
         action?.Invoke();
         ____currentFadingRoutine = null;
         __result = Enumerable.Empty<NextUpdate>().GetEnumerator();
         return false;
      } catch ( Exception ex ) { return Error( ex ); } }

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