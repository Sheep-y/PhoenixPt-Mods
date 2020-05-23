using Base;
using Base.Core;
using Base.Defs;

using Epic.OnlineServices;

using Harmony;

using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.Utils;
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

      public string Mutation_Uncap = "ANU_MutationTech3_ResearchDef"; // Ultimate Mutation Technology
      public string Bionics_Uncap = "SYN_Bionics3_ResearchDef"; // Restricted Bionic Technology

      public int Config_Version = 20200521;

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

      public void MainMod ( Func<string, object, object> api ) => GeoscapeMod( api );

      public void GeoscapeMod ( Func<string, object, object> api = null ) {
         ME = this;
         SetApi( api, out Config );
         Patch( typeof( Research ), "Initialize", postfix: nameof( AfterSetupResearch_AddRewards ) );
         if ( !string.IsNullOrWhiteSpace( Config.Mutation_Uncap ) )
            Patch( typeof( UIModuleMutate ), "Init", nameof( BeforeAugInit_UncapAugment ) );
         if ( !string.IsNullOrWhiteSpace( Config.Bionics_Uncap ) )
            Patch( typeof( UIModuleBionics ), "Init", nameof( BeforeAugInit_UncapAugment ) );
         Patch( typeof( CommonCharacterUtils ), "CanSwapItem", prefix: nameof( Test ) );
      }

      //private static void Test ( AddonDef.RequiredSlotBind __instance, AddonDef addonDef, bool __result ) {
      private static bool Test ( AddonsManagerDef addonsManagerDef, ItemDef addedItem, IEnumerable<ItemDef> otherUsedItems, InventoryComponentDef inventoryDef, bool lostHand, ref List<ItemDef> __result ) {
         Info( "In {0}", otherUsedItems.Count() );
         __result = new List<ItemDef>();
         if ( inventoryDef == null ) {
            PopulateProvidedUsedSlots( addonsManagerDef, otherUsedItems, out HashSet<AddonSlotPair> hashSet, out Dictionary<AddonSlotPair, int> dictionary );
            foreach ( var pair in dictionary ) Info( "Pair A={0} S={1} V={2}", pair.Key.Addon?.name, pair.Key.Slot?.name, pair.Value );
            foreach ( AddonSlotPair addonSlotPair in hashSet ) {
               if ( addedItem.IsCompatibleWithSlotFrom( addonSlotPair.Slot, addonSlotPair.Addon ) ) {
                  Info( "{0} is compatible with {1} {2}", addedItem.name, addonSlotPair.Slot.name, addonSlotPair.Addon.name );
                  if ( !dictionary.ContainsKey( addonSlotPair ) ) return false;
                  foreach ( ItemDef itemDef in otherUsedItems ) {
                     Info( "Checking {0} requires {1}", itemDef.name,
                        string.Join( " & ", itemDef.RequiredSlotBinds.Select( e => e.RequiredSlot?.name + "=" + e.GameTagFilter?.name ).ToArray() ) );
                     foreach ( var bind in itemDef.RequiredSlotBinds ) {
                        if ( bind.RequiredSlot == addonSlotPair.Slot ) {
                           Info( "Add 1 {0}", itemDef.name );
                           __result.Add( itemDef );
                           AddonDef.ProvidedSlotBind[] providedSlots = itemDef.ProvidedSlots;
                           for ( int j = 0 ; j < providedSlots.Length ; j++ ) {
                              foreach ( ItemDef itemDef2 in otherUsedItems ) {
                                 if ( !( itemDef2 == itemDef ) && itemDef2.IsCompatibleWithSlotFrom( itemDef ) ) {
                                    Info( "Add 2 {0}", itemDef2.name );
                                    __result.Add( itemDef2 );
                                 }
                              }
                           }
                           Info( "return B {0}", __result.Count );
                           return false;
                        }
                     }
                  }
               }
            }
            Info( "return C" );
            __result = null;
            return false;
         }
         InventoryListFilterDef filter = inventoryDef.Filter;
         int lostHands = lostHand ? 1 : 0;
         FilterData filterData = new FilterData { AddedItem = addedItem, CurrentItems = otherUsedItems, LostHands = lostHands };
         if ( filter == null || filter.CanAddItem( filterData ) ) {
            Info( "return D" );
            return false;
         }
         foreach ( ItemDef itemDef3 in otherUsedItems ) {
            filterData.RemovedItem = itemDef3;
            if ( filter.CanSwapItems( filterData ) ) {
               __result.Add( itemDef3 );
               Info( "return E" );
               return false;
            }
         }
         Info( "return F" );
         __result = null;
         return false;
      }

      private static void PopulateProvidedUsedSlots ( AddonsManagerDef managerDef, IEnumerable<ItemDef> otherUsedItems, out HashSet<AddonSlotPair> providedAddonSlotPairs, out Dictionary<AddonSlotPair, int> usedSlots ) {
         providedAddonSlotPairs = new HashSet<AddonSlotPair>();
         usedSlots = new Dictionary<AddonSlotPair, int>();
         foreach ( AddonSlotPair item in CommonCharacterUtils.GetDefaultAddonSlotPairs( managerDef ) ) {
            //Info( "Default slot A={0} S={1}", item.Addon?.name, item.Slot?.name );
            providedAddonSlotPairs.Add( item );
         }
         foreach ( ItemDef itemDef in otherUsedItems ) {
            foreach ( AddonDef.ProvidedSlotBind providedSlotBind in itemDef.ProvidedSlots ) {
               providedAddonSlotPairs.Add( new AddonSlotPair( itemDef, providedSlotBind.ProvidedSlot ) );
            }
            AddonDef.RequiredSlotBind[] requiredSlotBinds = itemDef.RequiredSlotBinds;
            int i = 0;
            while ( i < requiredSlotBinds.Length ) {
               AddonDef.RequiredSlotBind requiredSlotBind = requiredSlotBinds[i];
               AddonSlotPair addonSlotPair = providedAddonSlotPairs.FirstOrDefault((AddonSlotPair pair) => pair.Slot == requiredSlotBind.RequiredSlot && requiredSlotBind.IsCompatibleWith(pair.Addon));
               if ( addonSlotPair != null ) {
                  if ( !usedSlots.ContainsKey( addonSlotPair ) ) {
                     usedSlots.Add( addonSlotPair, 1 );
                     break;
                  }
                  Dictionary<AddonSlotPair, int> dictionary = usedSlots;
                  AddonSlotPair key = addonSlotPair;
                  int value = dictionary[key] + 1;
                  dictionary[ key ] = value;
                  break;
               } else {
                  i++;
               }
            }
         }
      }

      /*
  addonsManagerDef = PhoenixPoint.Common.Entities.CharacterAddonsManagerDef
  addedItem = AN_Berserker_Watcher_Torso_BodyPartDef
   
  at PhoenixPoint.Common.Entities.Addons.AddonDef.IsCompatibleWithSlotFrom_Patch1 (System.Object , PhoenixPoint.Common.Entities.Addons.AddonDef ) [0x00000] in <78261ddd35f34f58bcb82d8970cb19c6>:0 
  at PhoenixPoint.Common.Utils.CommonCharacterUtils.CanSwapItem (PhoenixPoint.Common.Entities.Addons.AddonsManagerDef addonsManagerDef, PhoenixPoint.Common.Entities.Items.ItemDef addedItem, System.Collections.Generic.IEnumerable`1[T] otherUsedItems, PhoenixPoint.Common.Entities.Items.InventoryComponentDef inventoryDef, System.Boolean lostHand) [0x00000] in <78261ddd35f34f58bcb82d8970cb19c6>:0 
  at PhoenixPoint.Geoscape.View.ViewModules.UIModuleMutate.OnAugmentClicked (PhoenixPoint.Geoscape.View.ViewModules.UIModuleMutationsSlot slotClicked, PhoenixPoint.Geoscape.View.ViewModules.UIModuleMutationSection owningSection) [0x00000] in <78261ddd35f34f58bcb82d8970cb19c6>:0 
  at PhoenixPoint.Geoscape.View.ViewModules.UIModuleMutationSection.SelectMutation (PhoenixPoint.Geoscape.View.ViewModules.UIModuleMutationsSlot slot) [0x00000] in <78261ddd35f34f58bcb82d8970cb19c6>:0 
  at PhoenixPoint.Geoscape.View.ViewModules.UIModuleMutationSection.<Awake>b__48_2 (PhoenixPoint.Geoscape.View.ViewModules.UIModuleMutationsSlot slot) [0x00000] in <78261ddd35f34f58bcb82d8970cb19c6>:0 
  at PhoenixPoint.Geoscape.View.ViewModules.UIModuleMutationsSlot.OnMutationClick () [0x00000] in <78261ddd35f34f58bcb82d8970cb19c6>:0 
  at PhoenixPoint.Common.View.ViewControllers.PhoenixGeneralButton.OnPointerClick (UnityEngine.EventSystems.PointerEventData eventData) [0x00000] in <78261ddd35f34f58bcb82d8970cb19c6>:0 
  at UnityEngine.EventSystems.ExecuteEvents.Execute (UnityEngine.EventSystems.IPointerClickHandler handler, UnityEngine.EventSystems.BaseEventData eventData) [0x00000] in <2e199f8f52044434913571c3679efdb2>:0 
  at UnityEngine.EventSystems.ExecuteEvents.Execute[T] (UnityEngine.GameObject target, UnityEngine.EventSystems.BaseEventData eventData, UnityEngine.EventSystems.ExecuteEvents+EventFunction`1[T1] functor) [0x00000] in <2e199f8f52044434913571c3679efdb2>:0 
  at UnityEngine.EventSystems.StandaloneInputModule.ProcessMousePress (UnityEngine.EventSystems.PointerInputModule+MouseButtonEventData data) [0x00000] in <2e199f8f52044434913571c3679efdb2>:0 
  at UnityEngine.EventSystems.StandaloneInputModule.ProcessMouseEvent (System.Int32 id) [0x00000] in <2e199f8f52044434913571c3679efdb2>:0 
  at UnityEngine.EventSystems.StandaloneInputModule.ProcessMouseEvent () [0x00000] in <2e199f8f52044434913571c3679efdb2>:0 
  at UnityEngine.EventSystems.StandaloneInputModule.Process () [0x00000] in <2e199f8f52044434913571c3679efdb2>:0 
  at UnityEngine.EventSystems.EventSystem.Update () [0x00000] in <2e199f8f52044434913571c3679efdb2>:0 
      */

      private static void AfterSetupResearch_AddRewards ( Research __instance ) {
         try {
            if ( !( __instance.Faction is GeoPhoenixFaction ) ) return;
            var factions = new List<GeoFactionDef>{ __instance.Faction.Def };

            var techs = new HashSet<string>();
            if ( Config.Manufacture_Unlock != null )
               techs.AddRange( Config.Manufacture_Unlock?.Values );
            if ( !string.IsNullOrWhiteSpace( Config.Mutation_Uncap ) ) techs.Add( Config.Mutation_Uncap );
            if ( !string.IsNullOrWhiteSpace( Config.Bionics_Uncap ) ) techs.Add( Config.Bionics_Uncap );
            MutateUncapTech = BionicsUncapTech = null;

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
                  if ( def.Id == Config.Mutation_Uncap || def.Guid == Config.Mutation_Uncap ) {
                     Verbo( "Marked mutation uncap tech {0} ({1})", Config.Mutation_Uncap, (Func<string>) tech.GetLocalizedName );
                     MutateUncapTech = tech;
                  }
                  if ( def.Id == Config.Bionics_Uncap || def.Guid == Config.Bionics_Uncap ) {
                     Verbo( "Marked bionics uncap tech {0} ({1})", Config.Bionics_Uncap, (Func<string>) tech.GetLocalizedName );
                     BionicsUncapTech = tech;
                  }
               }
            }
            Info( "{0} items added to research tree.", unlockCount );
         } catch ( Exception ex ) { Error( ex ); }
      }

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
         Verbo( "Adding {0} to {1} ({2})", item.name, tech.ResearchDef.name, (Func<string>) tech.GetLocalizedName );

         if ( Shared == null ) Shared = GameUtl.GameComponent<SharedData>();
         if ( !item.Tags.Any( e => e == Shared.SharedGameTags.ManufacturableTag ) )
            item.Tags.Add( Shared.SharedGameTags.ManufacturableTag );

         var rewards = tech.Rewards.ToList();
         var items = new List<ItemDef>{ item };
         if ( item.CompatibleAmmunition != null )
            items.AddRange( item.CompatibleAmmunition );
         rewards.Add( new ManufactureResearchReward( new ManufactureResearchRewardDef() { Items = items.ToArray(), ValidForFactions = factions } ) );
         typeof( ResearchElement ).GetProperty( "Rewards" ).SetValue( tech, rewards.ToArray() );
      }

      #region Augmentation Cap
      private static ResearchElement MutateUncapTech, BionicsUncapTech;
      private static IPatch MutUncapPatch, BioUncapPatch;

      private static void BeforeAugInit_UncapAugment () {
         try {
            CheckUncapTech( typeof( UIModuleMutate ), MutateUncapTech, ref MutUncapPatch );
            CheckUncapTech( typeof( UIModuleBionics ), BionicsUncapTech, ref BioUncapPatch );
         } catch ( Exception ex ) { Error( ex ); }
      }

      private static void CheckUncapTech ( Type screen, ResearchElement tech, ref IPatch patch ) {
         //Verbo( "Checking {0} for uncap", tech?.ResearchDef.name );
         if ( tech?.IsCompleted == true ) {
            if ( patch == null ) patch = PatchAugUncap( screen, tech );
         } else
            Unpatch( ref patch );
      }

      private static IPatch PatchAugUncap ( Type screen, ResearchElement tech ) {
         var patch = ME.TryPatch( screen, "InitCharacterInfo", transpiler: nameof( TranspileInitCharacterInfo ) );
         if ( patch != null ) Info( "Tech {0} is researched, uncapped augmentations", tech.ResearchDef.name );
         return patch;
      }

      private static IEnumerable<CodeInstruction> TranspileInitCharacterInfo ( IEnumerable<CodeInstruction> instr ) {
         var allCount = instr.Count( e => e.opcode == OpCodes.Ldc_I4_2 );
         if ( allCount < 3 || allCount > 4 ) { // Pre-Blood and Titanium Mutation = 3, Post-B&T Mutation = 4, Post-B&T Bionics = 3
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
            if ( i > 0 && codes[ i - 1 ].opcode != OpCodes.Stloc_S )
               code.opcode = OpCodes.Ldc_I4_3;
         }
         return instr.ToList();
      }
      #endregion
   }
}