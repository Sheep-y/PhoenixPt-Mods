using System;
using System.Collections.Generic;
using System.Linq;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;

namespace Sheepy.PhoenixPt.LootEverything {

   internal class ModConfig {
      public bool Drop_All_Item = true;
      public bool Drop_All_Armour = false;
      //public bool Loot_All_Missions = false; // GeoMission.ManageGear
      //public bool Loot_On_Loss = false; // GeoMission.ManageGear
      //public bool Stript_Survivors = true;
      public int Config_Version = 20200630;
   }

   public class Mod : ZyMod {
      private static ModConfig Config;

      public static void Init () => new Mod().MainMod();

      public void MainMod ( Func< string, object, object > api = null ) => TacticalMod( api );

      public void TacticalMod ( Func< string, object, object > api ) {
         SetApi( api, out Config );
         if ( Config.Drop_All_Item )
            TryPatch( typeof( DieAbility ), "ShouldDestroyItem", postfix: nameof( AfterShouldDestroyItem_KeepItem ) );
         if ( Config.Drop_All_Armour ) {
            StartPatch();
            TryPatch( typeof( GeoMission ), "GetDeadSquadMembersArmour", nameof( OverrideGetDeadSquadMembersArmour_Disable ) );
            TryPatch( typeof( DieAbility ), "DropItems", postfix: nameof( AfterDropItems_DropArmour ) );
            CommitPatch();
         }
      }

      private static void AfterShouldDestroyItem_KeepItem ( ref bool __result, TacticalItem item ) { try {
         if ( ! __result || item == null ) return;
         __result = false;
         Verbo( "Force dropping {0}", item.TacticalItemDef?.ViewElementDef?.Name );
      } catch ( Exception ex ) { Error( ex ); } }

      // Override self dead recover to not clone geosacpe armour copy.
      private static bool OverrideGetDeadSquadMembersArmour_Disable ( ref IEnumerable<GeoItem> __result ) {
         __result = Enumerable.Empty<GeoItem>();
         return false;
      }

      private static void AfterDropItems_DropArmour ( DieAbility __instance ) { try {
         var actor = __instance.TacticalActor;
         var items = actor?.BodyState?.GetArmourItems();
         if ( items?.Any() != true ) return;

         var shared = SharedData.GetSharedDataFromGame();
         var gameTags = shared.SharedGameTags;
         GameTagDef armour = gameTags.ArmorTag, manu = gameTags.ManufacturableTag, mount = gameTags.MountedTag;

         int count = 0;
         foreach ( var item in items ) {
            var def = item.ItemDef as TacticalItemDef;
            var tags = def?.Tags;
            if ( tags == null || tags.Count == 0 || ! tags.Contains( manu ) || def.IsPermanentAugment ) continue;
            if ( tags.Contains( armour ) || tags.Contains( mount ) ) {
               Verbo( "Force dropping {0}", def.ViewElementDef.Name );
               item.Drop( shared.FallDownItemContainerDef, actor );
               count++;
            }
         }
         if ( count > 0 )
            Info( "Force dropped {0} pieces from {1} of {2}", count, actor.ViewElementDef?.Name, actor.TacticalFaction?.Faction?.FactionDef?.GetName() );
      } catch ( Exception ex ) { Error( ex ); } }
   }
}