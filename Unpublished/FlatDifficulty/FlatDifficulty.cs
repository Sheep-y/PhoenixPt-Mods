using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.DifficultySystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static System.Reflection.BindingFlags;

namespace Sheepy.PhotnixPt.FlatDifficulty {
   public class Mod : PhoenixPt.ZyMod {
      static void Init () => new Mod().MainMod();

      public void MainMod ( Func< string, object, object > api = null ) {
         SetApi( api );
         Patch( typeof( DynamicDifficultySystem ), "ReadjustThreatLevelMods", nameof( BeforeReadjust_ClearHistory ) );
         //Patch( harmony, typeof( DynamicDifficultySystem ), "GetCalculatedDeployment", nameof( BeforeCalculate_Readjust ) );
         Patch( typeof( DynamicDifficultySystem ), "GetBattleOutcomeModifier", null, nameof( AfterBattleOutcome_Const ) );
      }

      private static void BeforeReadjust_ClearHistory ( DynamicDifficultySystem __instance ) { try {
         float[] val = __instance.BattleOutcomes.GetType().GetField( "_items", NonPublic | Instance ).GetValue( __instance.BattleOutcomes ) as float[];
         if ( val == null ) return;
         Info( "Resetting battle history to 1.0 from {0}", __instance.BattleOutcomes.DefaultIfEmpty(1f).Average() );
         // Can't just clear the outcome, that will reset all history to 0 without removing them!
         for ( int i = 0 ; i < val.Length ; i++ ) val[ i ] = 1f;
      } catch ( Exception ex ) { Error( ex ); } }

      private static void AfterBattleOutcome_Const ( ref float __result ) {
         Info( "Overwriting battle score to 1.0 from {0}", __result );
         __result = 1f;
      }

      private static void BeforeCalculate_Readjust ( DynamicDifficultySystem __instance, GeoMission mission, Dictionary<DifficultyThreatLevel, DynamicDifficultySystem.DeploymentModifier> ____deploymentModPerThreatLevel ) { try {
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
