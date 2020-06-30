using System;
using Base.Core;
using Base.Defs;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.Entities.Equipments;

namespace Sheepy.PhoenixPt.LootEverything {

   internal class ModConfig {
      public ushort Min_Item_Drop_Percentage = 100;
      public ushort Armour_Drop_Percentage = 100;
      public ushort Mount_Drop_Percentage = 100;
      //public bool Stript_Survivors = true;
      public int Config_Version = 20200630;
   }

   public class Mod : ZyMod {
      private static ModConfig Config;

      public static void Init () => new Mod().TacticalMod();

      public void MainMod ( Func< string, object, object > api ) => TacticalMod( api );

      public void TacticalMod ( Func< string, object, object > api = null ) {
         SetApi( api, out Config );
         MakeItemDroppable();
      }

      private static void MakeItemDroppable () {
         var gameTags = SharedData.GetSharedDataFromGame().SharedGameTags;
         GameTagDef armour = gameTags.ArmorTag, manu = gameTags.ManufacturableTag, mount = gameTags.MountedTag;
         int maxItemDestroyChance = Math.Min( Math.Max( 0, 100 - Config.Min_Item_Drop_Percentage ), 100 );

         foreach ( var item in GameUtl.GameComponent< DefRepository >().GetAllDefs<TacticalItemDef>() ) {
            var tags = item.Tags;
            if ( tags == null || tags.Count == 0 || ! tags.Contains( manu ) || item.IsPermanentAugment ) continue;
            if ( tags.Contains( armour ) )
               MakeItemDroppable( item, item.IsMountedToBody ? Config.Mount_Drop_Percentage : Config.Armour_Drop_Percentage );
            else if ( tags.Contains( mount ) )
               MakeItemDroppable( item, Config.Mount_Drop_Percentage );
            else if ( item.DropOnActorDeath && item.DestroyOnActorDeathPerc > maxItemDestroyChance )
               MakeItemDroppable( item, Config.Min_Item_Drop_Percentage );
         }
      }

      private static void MakeItemDroppable ( TacticalItemDef item, int perc ) {
         int killPerc = Math.Max( 0, 100 - perc );
         if ( killPerc >= 100 ) return;
         item.DropOnActorDeath = true;
         if ( item.DestroyOnActorDeathPerc > killPerc )
            item.DestroyOnActorDeathPerc = killPerc;
         Api( "log v", $"Set {item.name} drop% = {item.DestroyOnActorDeathPerc}" );
      }

      /*
      TryPatch( typeof( DieAbility ), "ShouldDestroyItem", postfix: nameof( AfterShouldDestroyItem_KeepItem ) );
      public static void AfterShouldDestroyItem_KeepItem ( ref bool __result, TacticalItem item ) {
         if ( ! __result ) return;
         __result = false;
         try {
            Api( "log v", $"Force dropping {item.TacticalItemDef.ViewElementDef.Name}" );
         } catch ( Exception ) { }
      }

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