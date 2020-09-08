using Base.Core;
using Base.Defs;
using Harmony;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sheepy.PhoenixPt.IndiGear {

   internal class ModConfig {

      public List<string> Unlock = new List<string>{
         // Indepent guns
         "NE_Pistol_WeaponDef",
         "NE_AssaultRifle_WeaponDef",
         "NE_SniperRifle_WeaponDef",
         "NE_MachineGun_WeaponDef",
         // Indepent armours
         "NEU_Heavy_Helmet_BodyPartDef",
         "NEU_Heavy_Torso_BodyPartDef",
         "NEU_Heavy_Legs_ItemDef",
         "NEU_Sniper_Helmet_BodyPartDef",
         "NEU_Sniper_Torso_BodyPartDef",
         "NEU_Sniper_Legs_ItemDef",
         //"NEU_Assault_Helmet_BodyPartDef", // Not exists?
         "NEU_Assault_Torso_BodyPartDef",
         "NEU_Assault_Legs_ItemDef",
      };

      public uint Config_Version = 20200522;
   }

   public class Mod : ZyMod {

      private static ModConfig Config;

      public static void Init () => new Mod().MainMod();

      public void MainMod ( Func<string, object, object> api = null ) {
         SetApi( api, out Config );
         Patch( typeof( GeoMissionScheduler ), "Init", postfix: nameof( AfterSetupResearch_UnlockItems ) );
         TryPatch( typeof( GeoPhoenixFaction ), "OnNewManufacturableItemsAdded", prefix: nameof( BeforeOnNewManufacturableItemsAdded_Abort ) );
      }

      public static void GeoscapeOnShow ( Func<string, object, object> api = null ) {
         if ( ! HasApi ) SetApi( api, out Config );
         AfterSetupResearch_UnlockItems();
      }

      private static bool Unlocking;
      private static int UnlockCount;

      [ HarmonyPriority( Priority.Low ) ]
      private static bool BeforeOnNewManufacturableItemsAdded_Abort () {
         return ! Unlocking;
      }

      private static void AfterSetupResearch_UnlockItems () { try {
         if ( Config.Unlock == null ) {
            Warn( "Unlock list is null.  Nothing to unlock." );
            return;
         }
         Unlocking = true;
         UnlockCount = 0;
         foreach ( var id in Config.Unlock ) {
            var def = Api( "\v pp.def", id ) as TacticalItemDef ?? FindTacItem( id );
            if ( def == null )
               Warn( "Not found: {0}", id );
            else
               UnlockItem( def );
         }
         Info( "Unlocked {0} items", UnlockCount );
      } catch ( Exception ex ) {
         Error( ex );
      } finally {
         Unlocking = false;
      } }


      private static void UnlockItem ( TacticalItemDef item ) {
         if ( Shared == null ) Shared = GameUtl.GameComponent<SharedData>();
         var manufacturableTag = Shared.SharedGameTags.ManufacturableTag;
         if ( ! item.Tags.Any( e => e == manufacturableTag ) )
            item.Tags.Add( manufacturableTag );
         if ( Manufacture.ManufacturableItems.Any( e => e.RelatedItemDef == item ) ) {
            Verbo( "Already unlocked: {0}", item.name );
            return;
         } else
            AddItem( item );
         if ( item.CompatibleAmmunition?.Length > 0 && item.CompatibleAmmunition[0] != item )
            UnlockItem( item.CompatibleAmmunition[ 0 ] );
      }

      private static void AddItem ( TacticalItemDef item ) {
         Verbo( "Unlock item: {0}", item.name );
         Manufacture.AddAvailableItem( item );
         UnlockCount++;
      }

      private static ItemManufacturing Manufacture => GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction.Manufacture;

      private static DefRepository Repo;
      private static SharedData Shared;

      private static TacticalItemDef FindTacItem ( string key ) {
         if ( Repo == null ) Repo = GameUtl.GameComponent<DefRepository>();
         return Repo.GetDef( key ) as TacticalItemDef ?? Repo.GetAllDefs<TacticalItemDef>().FirstOrDefault( e => e.name == key );
      }
   }
}
