using System;
using System.Collections.Generic;
using System.Linq;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;

namespace Sheepy.PhoenixPt.LootEverything {

   internal class ModConfig {
      public bool Drop_All_Item = true;
      public bool Drop_All_Armour = true;
      //public bool Stript_Survivors = true;
      public int Config_Version = 20200630;
   }

   public class Mod : ZyMod {
      private static ModConfig Config;

      public static void Init () => new Mod().TacticalMod();

      public void MainMod ( Func< string, object, object > api ) => TacticalMod( api );

      public void TacticalMod ( Func< string, object, object > api = null ) {
         SetApi( api, out Config );
         TryPatch( typeof( DieAbility ), "ShouldDestroyItem", postfix: nameof( AfterShouldDestroyItem_KeepItem ) );
         if ( Config.Drop_All_Armour ) {
            TryPatch( typeof( DieAbility ), "DropItems", postfix: nameof( AfterDropItems_DropArmour ) );
            TryPatch( typeof( GeoMission ), "GetDeadSquadMembersArmour", nameof( OverrideGetDeadSquadMembersArmour_Disable ) );
         }
      }

      public static void AfterShouldDestroyItem_KeepItem ( ref bool __result, TacticalItem item ) {
         if ( ! __result ) return;
         if ( Config.Drop_All_Item ) try {
            __result = false;
            Api( "log", $"Force dropping {item.TacticalItemDef.ViewElementDef.Name}" );
         } catch ( Exception ) { }
      }

      public static bool OverrideGetDeadSquadMembersArmour_Disable ( ref IEnumerable<GeoItem> __result ) {
         __result = Enumerable.Empty<GeoItem>();
         return false;
      }

      public static void AfterDropItems_DropArmour ( DieAbility __instance ) {
         var shared = SharedData.GetSharedDataFromGame();
         var gameTags = shared.SharedGameTags;
         var actor = __instance.TacticalActor;
         GameTagDef armour = gameTags.ArmorTag, manu = gameTags.ManufacturableTag, mount = gameTags.MountedTag;

         foreach ( var item in actor.BodyState.GetArmourItems() ) {
            var def = item.ItemDef as TacticalItemDef;
            var tags = def?.Tags;
            if ( tags == null || tags.Count == 0 || ! tags.Contains( manu ) || def.IsPermanentAugment ) continue;
            if ( tags.Contains( armour ) || tags.Contains( mount ) ) {
               Api( "log", $"Force dropping {def.ViewElementDef.Name}" );
               item.Drop( shared.FallDownItemContainerDef, actor );
            }
         }
      }

      /*
      if ( Config.Stript_Survivors ) TryPatch( typeof( GeoMission ), "ManageGear", nameof( BeforeManageGear_StriptItems ) );
      public static void BeforeManageGear_StriptItems ( GeoMission __instance, TacMissionResult result ) { try {
         var shared = SharedData.GetSharedDataFromGame();
         var pp = shared.PhoenixFactionDef;
         var pvTag = __instance.Site.GeoLevel.AlienFaction.FactionDef.RaceTagDef;

         var items = __instance.Reward.Items;
         foreach ( var byFaction in result.FactionResults ) {
            if ( byFaction.FactionDef == pp ) continue;
            foreach ( var unit in byFaction.UnitResults ) {
               var data = unit.Data as TacActorUnitResult;
               if ( data == null ) continue;
               if ( ! data.IsAlive ) continue;
               if ( Config.Armour_Drop_Chance > 0 ) {
                  foreach ( var armour in data.BodypartItems ) {
                     // Copy check from GetItemsOnTheGround
                     items.AddItem( new GeoItem( armour ) );
                  }
               }
            }
         }
      } catch ( Exception ex ) { Api( "log e", ex ); } }
      */
   }
}