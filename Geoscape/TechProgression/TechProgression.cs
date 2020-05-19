using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sheepy.PhoenixPt_TechProgression {
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
   using System.Reflection.Emit;
   using static System.Reflection.BindingFlags;

   public class Mod {
      //private static Logging.Logger Log = new Logging.Logger( "Mods/SheepyMods.log" );

      private static HarmonyInstance harmony;

      public static void Init () {
         harmony = HarmonyInstance.Create( typeof( Mod ).Namespace );

         Patch( harmony, typeof( Research ), "Initialize", null, "AfterSetupResearch_AddResearch" );
         Patch( harmony, typeof( UIModuleMutate ), "Init", "BeforeMutationInit_UnlockMutation" );
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

      private static ResearchElement UltimateMutation;

      public static void AfterSetupResearch_AddResearch ( Research __instance ) { try {
         if ( ! ( __instance.Faction is GeoPhoenixFaction ) ) return;
         int patchCount = 2;

         foreach ( var tech in __instance.AllResearchesArray ) {
            switch ( tech.ResearchDef.Id ) {
               // The Phoenix Archives, 11dd78fb-37a5-0341-d2bb-a7b0e77fefb8
               case "PX_PhoenixProject_ResearchDef" :
                  // Uragan MG, 5694855c-3869-fbd4-19a0-06850493b037
                  WeaponDef mg = GameUtl.GameComponent<DefRepository>().GetAllDefs<WeaponDef>().FirstOrDefault( e => e.name == "NE_MachineGun_WeaponDef" );
                  UnlockWeaponByResearch( __instance.Faction, tech, mg );
                  break;

               // Ultimate Mutation Technology, 61602b54-8940-70d2-5353-a8c87e2b74b6
               case "ANU_MutationTech3_ResearchDef" :
                  UltimateMutation = tech;
                  break;

               default: continue; // Skip patch count decrement
            }
            //Log.Info( "Upgraded research {0}", tech.GetLocalizedName() );
            if ( --patchCount <= 0 ) break;
         }
      } catch ( Exception ex ) { LogError( ex ); } }

      public static void UnlockWeaponByResearch( GeoFaction faction, ResearchElement tech, WeaponDef weapon ) {
         if ( tech.Rewards.Any( e => (e.BaseDef as ManufactureResearchRewardDef)?.Items?.Contains( weapon ) ?? false ) )
            return;

         List<ResearchReward> rewards = tech.Rewards.ToList();
         rewards.Add( new ManufactureResearchReward( new ManufactureResearchRewardDef() {
            Items = new ItemDef[] { weapon, weapon.CompatibleAmmunition[ 0 ] },
            ValidForFactions = new GeoFactionDef[] { faction.Def }.ToList()
         } ) );
         typeof( ResearchElement ).GetProperty( "Rewards" ).SetValue( tech, rewards.ToArray() );
      }

      #region Mutation Cap

      public static void BeforeMutationInit_UnlockMutation () { try {
         if ( UltimateMutation == null ) return;
         if ( UltimateMutation.IsCompleted )
            PatchFullMutation();
         else
            UnpatchFullMutation();
      } catch ( Exception ex ) { LogError( ex ); } }

      private static bool FullMutationPatched = false;
      private static MethodInfo FullMutationMethod, FullMutationPatch;
      //private static List<CodeInstruction> FullMutationCode;

      private static void PatchFullMutation () {
         if ( FullMutationPatched ) return;
         if ( FullMutationMethod == null ) {
            FullMutationMethod = typeof( UIModuleMutate ).GetMethod( "InitCharacterInfo", NonPublic | Instance );
            FullMutationPatch = typeof( Mod ).GetMethod( "TranspileInitCharacterInfo", NonPublic | Static );
         }
         harmony.Patch( FullMutationMethod, null, null, new HarmonyMethod( FullMutationPatch ) );
         FullMutationPatched = true;
      }

      private static void UnpatchFullMutation () {
         if ( ! FullMutationPatched ) return;
         harmony.Unpatch( FullMutationMethod, FullMutationPatch );
         FullMutationPatched = false;
      }

      private static IEnumerable<CodeInstruction> TranspileInitCharacterInfo ( IEnumerable<CodeInstruction> instr ) {
         //if ( FullMutationCode != null ) return FullMutationCode;
         if ( instr.Count( e => e.opcode == OpCodes.Ldc_I4_2 ) != 3 )
            throw new Exception( "Unrecognised mutation code. Aborting." );
         foreach ( var code in instr )
            if ( code.opcode == OpCodes.Ldc_I4_2 )
               code.opcode = OpCodes.Ldc_I4_3;
         return instr.ToList();
      }

      #endregion
   }
}