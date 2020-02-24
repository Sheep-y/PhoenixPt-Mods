using Harmony;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.DifficultySystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using static System.Reflection.BindingFlags;

namespace Sheepy.PhotnixPt_FlatDifficulty {
   public static class Mod {
      public static void Init () => ModMainMenu();
      
      public static void ModMainMenu ( Action< SourceLevels, object, object[] > logger = null ) {
         SetLogger( logger );
         Patch( typeof( DynamicDifficultySystem ), "ReadjustThreatLevelMods", nameof( BeforeReadjust_ClearHistory ) );
         //Patch( harmony, typeof( DynamicDifficultySystem ), "GetCalculatedDeployment", nameof( BeforeCalculate_Readjust ) );
         Patch( typeof( DynamicDifficultySystem ), "GetBattleOutcomeModifier", null, nameof( AfterBattleOutcome_Const ) );
      }

      #region Modding helpers
      private static HarmonyInstance harmony;

      private static void Patch ( Type target, string toPatch, string prefix = null, string postfix = null ) {
         Patch( target.GetMethod( toPatch, Public | NonPublic | Instance | Static ), prefix, postfix );
      }

      private static void Patch ( MethodInfo toPatch, string prefix = null, string postfix = null ) {
         harmony.Patch( toPatch, ToHarmonyMethod( prefix ), ToHarmonyMethod( postfix ) );
      }

      private static HarmonyMethod ToHarmonyMethod ( string name ) {
         if ( name == null ) return null;
         MethodInfo func = typeof( Mod ).GetMethod( name );
         if ( func == null ) throw new NullReferenceException( name + " is null" );
         return new HarmonyMethod( func );
      }

      private static Action< SourceLevels, object, object[] > Logger;

      private static void SetLogger ( Action< SourceLevels, object, object[] > logger ) {
         if ( logger == null ) {
            Logger = DefaultLogger;
            Info( DateTime.Now.ToString( "D" ) + " " + typeof( Mod ).Namespace + " " + Assembly.GetExecutingAssembly().GetName().Version );
         } else
            Logger = logger;
         if ( harmony == null )
            harmony = HarmonyInstance.Create( typeof( Mod ).Namespace );
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

      private static void Info ( object msg, params object[] augs ) =>
         Logger?.Invoke( SourceLevels.Information, msg, augs );

      private static bool Error ( object msg, params object[] augs ) {
         Logger?.Invoke( SourceLevels.Error, msg, null );
         return true;
      }
      #endregion

      public static void BeforeReadjust_ClearHistory ( DynamicDifficultySystem __instance ) { try {
         float[] val = __instance.BattleOutcomes.GetType().GetField( "_items", NonPublic | Instance ).GetValue( __instance.BattleOutcomes ) as float[];
         if ( val == null ) return;
         Info( "Resetting battle history {0} => 1f", __instance.BattleOutcomes.DefaultIfEmpty(1f).Average() );
         // Can't just clear the outcome, that will reset all history to 0 without removing them!
         for ( int i = 0 ; i < val.Length ; i++ ) val[ i ] = 1f;
      } catch ( Exception ex ) { Error( ex ); } }

      public static void AfterBattleOutcome_Const ( ref float __result ) {
         Info( "Overwriting battle score {0} => 1f", __result );
         __result = 1f;
      }

      public static void BeforeCalculate_Readjust ( DynamicDifficultySystem __instance, GeoMission mission, Dictionary<DifficultyThreatLevel, DynamicDifficultySystem.DeploymentModifier> ____deploymentModPerThreatLevel ) { try {
         Info( "Deploy Modifier: Min {0} Cur {1}", 
            ____deploymentModPerThreatLevel[ mission.ThreatLevel ].MinDeploymentModifier,
            ____deploymentModPerThreatLevel[ mission.ThreatLevel ].CurrentDeploymentModifier
            );
         ____deploymentModPerThreatLevel.Clear();
            /*
         typeof( DynamicDifficultySystem ).GetMethod( "SetInitialDeploymentValues", NonPublic | Instance ).Invoke( __instance, new object[]{ mission.ThreatLevel } );
         Info( "After recalc: Min {0} Cur {1}", 
            ____deploymentModPerThreatLevel[ mission.ThreatLevel ].MinDeploymentModifier,
            ____deploymentModPerThreatLevel[ mission.ThreatLevel ].CurrentDeploymentModifier
            );
            */
      } catch ( Exception ex ) { Error( ex ); } }
   }
}
