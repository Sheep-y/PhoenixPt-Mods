using System;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;

namespace Sheepy.PhoenixPt.LootEverything {

   public class Mod : ZyMod {

      public static void Init () => new Mod().TacticalMod();

      public void MainMod ( Func< string, object, object > api ) => TacticalMod( api );

      public void TacticalMod ( Func< string, object, object > api = null ) {
         SetApi( api );
         Patch( typeof( DieAbility ), "ShouldDestroyItem", postfix: nameof( AfterShouldDestroyItem_KeepItem ) );
      }

      public static void AfterShouldDestroyItem_KeepItem ( ref bool __result, TacticalItem item ) {
         if ( ! __result ) return;
         __result = false;
         try {
            Api( "log v", $"Force dropping {item.TacticalItemDef.ViewElementDef.Name}" );
         } catch ( Exception ) { }
      }
   }
}