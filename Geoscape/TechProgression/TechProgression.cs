using Base;
using Base.Core;
using Base.Defs;
using Harmony;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace Sheepy.PhoenixPt.TechProgression {

   internal class ModConfig {

      public Dictionary<string,string> Manufacture_Unlock = new Dictionary<string, string>( Default_Manufacture_Unlock );

      public string Augment_Uncap = "ANU_MutationTech3_ResearchDef"; // Ultimate Mutation Technology

      public int Config_Version = 20200520;

      private static readonly Dictionary<string,string> Default_Manufacture_Unlock = new Dictionary<string, string> {
         // Unlock indepent guns with "The Phoenix Archives"
         { "NE_Pistol_WeaponDef", "PX_PhoenixProject_ResearchDef" },
         { "NE_AssaultRifle_WeaponDef", "PX_PhoenixProject_ResearchDef" },
         { "NE_SniperRifle_WeaponDef", "PX_PhoenixProject_ResearchDef" },
         { "NE_MachineGun_WeaponDef", "PX_PhoenixProject_ResearchDef" },
         // Indepent armours
         { "NEU_Heavy_Helmet_BodyPartDef", "PX_PhoenixProject_ResearchDef" },
         { "NEU_Heavy_Torso_BodyPartDef", "PX_PhoenixProject_ResearchDef" },
         { "NEU_Heavy_Legs_ItemDef", "PX_PhoenixProject_ResearchDef" },
         { "NEU_Sniper_Helmet_BodyPartDef", "PX_PhoenixProject_ResearchDef" },
         { "NEU_Sniper_Torso_BodyPartDef", "PX_PhoenixProject_ResearchDef" },
         { "NEU_Sniper_Legs_ItemDef", "PX_PhoenixProject_ResearchDef" },
         //{ "NEU_Assault_Helmet_BodyPartDef", "PX_PhoenixProject_ResearchDef" }, // Not exists?
         { "NEU_Assault_Torso_BodyPartDef", "PX_PhoenixProject_ResearchDef" },
         { "NEU_Assault_Legs_ItemDef", "PX_PhoenixProject_ResearchDef" },
         // Basic weapons
         { "AN_Redemptor_WeaponDef", "PX_DisciplesOfAnu_ResearchDef" },
         { "NJ_PRCR_PDW_WeaponDef", "PX_NewJericho_ResearchDef" },
         { "SY_Crossbow_WeaponDef", "PX_Synedrion_ResearchDef" },
      };
   }

   public class Mod : ZyMod {

      private static ModConfig Config;

      private static Mod ME;

      public static void Init () => new Mod().GeoscapeMod();

      public void MainMod ( Func< string, object, object > api ) => GeoscapeMod( api );

      public void GeoscapeMod ( Func< string, object, object > api = null ) {
         ME = this;
         SetApi( api, out Config );
         Patch( typeof( Research ), "Initialize", postfix: nameof( AfterSetupResearch_AddRewards ) );
         Patch( typeof( UIModuleMutate ), "Init", nameof( BeforeMutationInit_UncapAugment ) );
      }

      private static void AfterSetupResearch_AddRewards ( Research __instance ) { try {
         if ( !( __instance.Faction is GeoPhoenixFaction ) ) return;
         var factions = new List<GeoFactionDef>{ __instance.Faction.Def };

         var uncap = Config.Augment_Uncap;
         var techs = new HashSet<string>();
         if ( Config.Manufacture_Unlock != null )
            techs.AddRange( Config.Manufacture_Unlock?.Values );
         if ( uncap != null )
            techs.Add( uncap );
         AugmentUncapTech = null;

         var unlockCount = 0;
         foreach ( var tech in __instance.AllResearchesArray ) {
            var def = tech.ResearchDef;
            if ( techs.Contains( def.Id ) || techs.Contains( def.Guid ) ) {
               foreach ( var entry in Config.Manufacture_Unlock ) {
                  if ( entry.Value == def.Id || entry.Value == def.Guid ) {
                     var item = Api( "\v pp.def", entry.Key ) as TacticalItemDef ?? FindTacItem( entry.Key );
                     if ( item != null ) {
                        UnlockItemByResearch( factions, tech, item );
                        unlockCount++;
                     } else
                        Warn( "Tech {0} found, but Item {1} not found.", def.name, entry.Key );
                  }
               }
               if ( uncap != null && def.Id == uncap || def.Guid == uncap ) {
                  Verbo( "Monitoring augmentation uncap tech {0}", (Func<string>) tech.GetLocalizedName );
                  AugmentUncapTech = tech;
               }
            }
         }
         Info( "{0} items added to research tree.", unlockCount );
      } catch ( Exception ex ) { Error( ex ); } }

      private static DefRepository Repo;
      private static SharedData Shared;

      private static TacticalItemDef FindTacItem ( string key ) {
         if ( Repo == null ) Repo = GameUtl.GameComponent<DefRepository>();
         var def = Repo.GetDef( key ) as TacticalItemDef;
         if ( def != null ) return def;
         return Repo.GetAllDefs<TacticalItemDef>().FirstOrDefault( e => e.name == key );
      }

      private static void UnlockItemByResearch ( List<GeoFactionDef> factions, ResearchElement tech, TacticalItemDef item ) {
         if ( tech.Rewards.Any( e => ( e.BaseDef as ManufactureResearchRewardDef )?.Items?.Contains( item ) ?? false ) ) return;
         Verbo( "Adding {0} to {1}", item.name, tech.ResearchDef.name );

         if ( Shared == null ) Shared = GameUtl.GameComponent<SharedData>();
         if ( ! item.Tags.Any( e => e == Shared.SharedGameTags.ManufacturableTag ) )
            item.Tags.Add( Shared.SharedGameTags.ManufacturableTag );

         var rewards = tech.Rewards.ToList();
         var items = new List<ItemDef>{ item };
         if ( item.CompatibleAmmunition != null )
            items.AddRange( item.CompatibleAmmunition );
         rewards.Add( new ManufactureResearchReward( new ManufactureResearchRewardDef() { Items = items.ToArray(), ValidForFactions = factions } ) );
         typeof( ResearchElement ).GetProperty( "Rewards" ).SetValue( tech, rewards.ToArray() );
      }

      #region Augmentation Cap
      private static ResearchElement AugmentUncapTech;

      private static void BeforeMutationInit_UncapAugment () { try {
         if ( AugmentUncapTech == null ) return;
         if ( AugmentUncapTech.IsCompleted )
            PatchAugUncap();
         else
            Unpatch( ref AugUncapPatch );
      } catch ( Exception ex ) { Error( ex ); } }

      private static IPatch AugUncapPatch;

      private static void PatchAugUncap () {
         if ( AugUncapPatch != null ) return;
         AugUncapPatch = ME.TryPatch( typeof( UIModuleMutate ), "InitCharacterInfo", transpiler: nameof( TranspileInitCharacterInfo ) );
         if ( AugmentUncapTech != null )
            Info( "Tech {0} is researched, uncapped augmentations", (Func<string>) AugmentUncapTech.GetLocalizedName );
      }

      private static IEnumerable<CodeInstruction> TranspileInitCharacterInfo ( IEnumerable<CodeInstruction> instr ) {
         var allCount = instr.Count( e => e.opcode == OpCodes.Ldc_I4_2 );
         if ( allCount < 3 || allCount > 4 ) { // Pre-Blood and Titanium = 3, Post-B&T = 4
            Error( new Exception( $"Unrecognised argument code.  Cap count {allCount}, expected 3 or 4.  Aborting." ) );
            //foreach ( var code in instr ) Verbo( code );
            return instr;
         }
         var codes = instr.ToArray();
         for ( var i = 0 ; i < codes.Length ; i++ ) {
            var code = codes[i];
            if ( code.opcode != OpCodes.Ldc_I4_2 ) continue;
            // Post-B&T, the second ldc.i4.2 loads the enum AugumentSlotState.AugumentationLimitReached.
            // It follows a stloc.s 5 and must be skipped.
            if ( allCount == 3 || ( i > 0 && codes[ i-1 ].opcode != OpCodes.Stloc_S ) )
               code.opcode = OpCodes.Ldc_I4_3;
         }
         return instr.ToList();
      }

      #endregion
   }
}