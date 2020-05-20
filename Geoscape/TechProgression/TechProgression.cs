using Base.Core;
using Base.Defs;
using Harmony;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using static System.Reflection.BindingFlags;

namespace Sheepy.PhoenixPt.TechProgression {

   public class Mod : ZyMod {
      private static Mod ME;

      public static void Init () => new Mod().GeoscapeMod();

      public void MainMod ( Func< string, object, object > api ) => GeoscapeMod( api );

      public void GeoscapeMod ( Func< string, object, object > api = null ) {
         ME = this;
         SetApi( api );
         Patch( typeof( Research ), "Initialize", postfix: "AfterSetupResearch_AddResearch" );
         Patch( typeof( UIModuleMutate ), "Init", "BeforeMutationInit_UnlockMutation" );
      }

      private static ResearchElement UltimateMutation;

      public static void AfterSetupResearch_AddResearch ( Research __instance ) {
         try {
            if ( !( __instance.Faction is GeoPhoenixFaction ) ) return;
            int patchCount = 2;

            foreach ( var tech in __instance.AllResearchesArray ) {
               switch ( tech.ResearchDef.Id ) {
                  // The Phoenix Archives, 11dd78fb-37a5-0341-d2bb-a7b0e77fefb8
                  case "PX_PhoenixProject_ResearchDef":
                     // Uragan MG, 5694855c-3869-fbd4-19a0-06850493b037
                     WeaponDef mg = GameUtl.GameComponent<DefRepository>().GetAllDefs<WeaponDef>().FirstOrDefault( e => e.name == "NE_MachineGun_WeaponDef" );
                     UnlockWeaponByResearch( __instance.Faction, tech, mg );
                     break;

                  // Ultimate Mutation Technology, 61602b54-8940-70d2-5353-a8c87e2b74b6
                  case "ANU_MutationTech3_ResearchDef":
                     UltimateMutation = tech;
                     break;

                  default: continue; // Skip patch count decrement
               }
               //Log.Info( "Upgraded research {0}", tech.GetLocalizedName() );
               if ( --patchCount <= 0 ) break;
            }
         } catch ( Exception ex ) { Error( ex ); }
      }

      public static void UnlockWeaponByResearch ( GeoFaction faction, ResearchElement tech, WeaponDef weapon ) {
         if ( tech.Rewards.Any( e => ( e.BaseDef as ManufactureResearchRewardDef )?.Items?.Contains( weapon ) ?? false ) )
            return;

         List<ResearchReward> rewards = tech.Rewards.ToList();
         rewards.Add( new ManufactureResearchReward( new ManufactureResearchRewardDef() {
            Items = new ItemDef[] { weapon, weapon.CompatibleAmmunition[ 0 ] },
            ValidForFactions = new GeoFactionDef[] { faction.Def }.ToList()
         } ) );
         typeof( ResearchElement ).GetProperty( "Rewards" ).SetValue( tech, rewards.ToArray() );
      }

      #region Mutation Cap

      public static void BeforeMutationInit_UnlockMutation () {
         try {
            if ( UltimateMutation == null ) return;
            if ( UltimateMutation.IsCompleted )
               PatchFullMutation();
            else
               UnpatchFullMutation();
         } catch ( Exception ex ) { Error( ex ); }
      }

      private static IPatch FullMutationPatch;

      private static void PatchFullMutation () {
         if ( FullMutationPatch != null ) return;
         FullMutationPatch = ME.Patch( typeof( UIModuleMutate ), "InitCharacterInfo", transpiler: nameof( TranspileInitCharacterInfo ) );
      }

      private static void UnpatchFullMutation () {
         Unpatch( ref FullMutationPatch );
      }

      private static IEnumerable<CodeInstruction> TranspileInitCharacterInfo ( IEnumerable<CodeInstruction> instr ) {
         if ( instr.Count( e => e.opcode == OpCodes.Ldc_I4_2 ) != 3 ) {
            Error( new Exception( "Unrecognised mutation code. Aborting." ) );
            foreach ( var code in instr ) Verbo( code );
            return instr;
         }
         foreach ( var code in instr )
            if ( code.opcode == OpCodes.Ldc_I4_2 )
               code.opcode = OpCodes.Ldc_I4_3;
         return instr.ToList();
      }

      #endregion
   }
}