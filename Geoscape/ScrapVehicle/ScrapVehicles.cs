using Base.Core;
using Base.Defs;
using Base.UI.MessageBox;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewControllers.Manufacturing;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static System.Reflection.BindingFlags;

namespace Sheepy.PhoenixPt.ScrapVehicle {

   public class Mod : ZyMod {
      public static void Init () => new Mod().GeoscapeMod();

      public void MainMod ( Func< string, object, object > api ) => GeoscapeMod( api );

      public void GeoscapeMod ( Func< string, object, object > api = null ) {
         SetApi( api );
         StartPatch( "scrap vehicle" );
         var UiType = typeof( UIModuleManufacturing );
         Patch( UiType, "SetupClassFilter", postfix: nameof( AfterSetupClassFilter_CheckScrapMode ) );
         Patch( UiType, "SetupQueue", nameof( BeforeSetupQueue_AddVehicleToScrap ) );
         Patch( UiType, "RefreshFilters", postfix: nameof( AfterRefreshFilters_EnableVehicleTab ) );
         Patch( UiType, "RefreshItemList", nameof( BeforeRefreshItemList_FillWithVehicle ) );
         Patch( UiType, "OnItemAction", nameof( BeforeOnItemAction_ConfirmScrap ) );
         Patch( UiType, "Close", postfix: nameof( AfterClose_Cleanup ) );
         var GeoItemInits = typeof( GeoManufactureItem ).GetMethods().Where(
               e => e.Name == "Init" && e.GetParameters().Any( p => p.Name == "item" && p.ParameterType == typeof( IManufacturable ) ) );
         if ( ! GeoItemInits.Any() ) {
            RollbackPatch( "GeoManufactureItem.Init not found" );
            return;
         }
         foreach ( var method in GeoItemInits )
            Patch( method, postfix: nameof( AftereInit_SetName ) );
         CommitPatch();
         // This one is relatively minor, so putting out of transaction.
         TryPatch( typeof( ItemDef ), "get_ScrapPrice", postfix: nameof( AftereScrapPrice_AddMutagen ) );
         TryPatch( typeof( UIModuleManufacturing ), "OnEnterSlot", postfix: nameof( AfterEnterSlot_UpdateName ) );
      }

      private static void AfterEnterSlot_UpdateName ( UIModuleManufacturing __instance, IManufacturableUIElement item ) {
         if ( ! ( item is GeoManufactureItem mItem ) ) return;
         if ( ! ( mItem.Manufacturable is GeoUnitWrapper geoUnit ) ) return ;
         __instance.Tooltip.InfoPanel.ItemNameTextComp.text = geoUnit.GetName();
         GeoSite site = null;
         if ( geoUnit is GeoPlaneWrapper plane ) site = plane.Vehicle.CurrentSite;
         else if ( geoUnit is GeoTankWrapper tank ) site = TankSites?[ tank.GroundVehicle ];
         if ( site != null ) {
            var region = site.GetComponent<GeoPhoenixBase>()?.LocationDescription.Localize();
            if ( ! string.IsNullOrWhiteSpace( region ) ) region = ", " + region;
            __instance.Tooltip.InfoPanel.ItemDescriptionTextComp.text += "\n" + site.Name + region;
         }
      }

      #region General helpers
      private static bool IsScrapping ( UIModuleManufacturing __instance ) {
         return __instance.Mode == UIModuleManufacturing.UIMode.Scrap;
      }

      private static bool IsScrappingVehicles ( UIModuleManufacturing __instance, PhoenixGeneralButton _activeFilterButton ) {
         return IsScrapping( __instance ) && _activeFilterButton == __instance.VehiclesFilterButton;
      }

      private static bool CanScrap ( GeoVehicle plane, bool checkVehicleBay ) {
         GeoSite site = plane.CurrentSite;
         if ( plane.Travelling || site == null || site.Type != GeoSiteType.PhoenixBase ) return false;
         if ( ! ( site.GetComponent<GeoPhoenixBase>() is GeoPhoenixBase pxBase ) ) return false;
         return ! checkVehicleBay || CanScrapVehicles( pxBase );
      }
      private static bool CanScrapVehicles ( GeoPhoenixBase pxBase ) {
         return pxBase.Stats.RepairVehiclesHP > 0;
      }
      #endregion

      private static bool NeedToAddVehicles = false;
      private static Dictionary<GeoTacUnit,GeoSite> TankSites;

      // Check whether scrap list need to be populated
      private static void AfterSetupClassFilter_CheckScrapMode ( UIModuleManufacturing __instance, ItemStorage ____scrapStorage ) { try {
         NeedToAddVehicles = IsScrapping( __instance ) && ____scrapStorage.IsEmpty;
      } catch ( Exception ex ) { Error( ex ); } }

      // If scrap list need to be populated, do it
      private static void BeforeSetupQueue_AddVehicleToScrap ( GeoscapeViewContext ____context, ItemStorage ____scrapStorage ) { try {
         if ( ! NeedToAddVehicles ) return;
         LoadVehicleDefs();

         GeoPhoenixFaction faction = ____context.ViewerFaction as GeoPhoenixFaction;
         foreach ( GeoVehicle v in faction.Vehicles ) {
            Verbo( "Add {0} to item list", v.Name );
            ____scrapStorage.AddItem( new GeoPlaneWrapper( v ) );
         }

         NeedToAddVehicles = false;
      } catch ( Exception ex ) { Error( ex ); } }

      // Enable vehicle scrap tab
      private static void AfterRefreshFilters_EnableVehicleTab ( UIModuleManufacturing __instance, PhoenixGeneralButton ____activeFilterButton ) { try {
         if ( __instance.Mode != UIModuleManufacturing.UIMode.Scrap ) return;
         __instance.VehiclesFilterButton.SetInteractable( true );
         __instance.VehiclesFilterButton.GetComponent<UITooltipText>().TipKey = __instance.VehiclesTooltipText;
         /* // Show / hide class filters as appropiate - disabled because filter state is not restored correctly after toggle
         UiType.GetMethod( "SetClassFiltersAvailability", NonPublic | Instance )
            .Invoke( __instance, new object[]{ ____activeFilterButton != __instance.VehiclesFilterButton } ); */
      } catch ( Exception ex ) { Error( ex ); } }

      // Replace scrap list with individual vehicles when applicable
      private static void BeforeRefreshItemList_FillWithVehicle ( UIModuleManufacturing __instance, ref IEnumerable<IManufacturable> availableItemRecipes,
                                                                 PhoenixGeneralButton ____activeFilterButton, GeoscapeViewContext ____context ) { try {
         if ( availableItemRecipes == null || availableItemRecipes.GetType() == typeof( List<IManufacturable> ) ) return;
         if ( ! IsScrappingVehicles( __instance, ____activeFilterButton ) ) return;

         List<IManufacturable> vList = new List<IManufacturable>();
         GeoPhoenixFaction faction = ____context.ViewerFaction as GeoPhoenixFaction;
         TankSites = new Dictionary<GeoTacUnit, GeoSite>();

         if ( faction.Vehicles.Count() > 1 ) {
            foreach ( GeoVehicle plane in faction.Vehicles )
               if ( CanScrap( plane, true ) ) {
                  Verbo( "Can scrap airplane {0}", plane.Name );
                  vList.Add( new GeoPlaneWrapper( plane ) );
                  foreach ( var chr in plane.Characters )
                     if ( chr.ClassDef.IsVehicle || chr.ClassDef.IsMutog )
                        TankSites.Add( chr, plane.CurrentSite );
               }
         }
         foreach ( GeoPhoenixBase pxbase in faction.Bases ) {
            var units = pxbase.Site.TacUnits;
            if ( ! units.Any() ) continue;
            foreach ( var chr in units )
               if ( chr.ClassDef.IsMutog )
                  TankSites.Add( chr, pxbase.Site );
            if ( CanScrapVehicles( pxbase ) ) {
               Verbo( "Can scrap tanks at {0}", pxbase.Site.Name );
               foreach ( var chr in units )
                  if ( chr.ClassDef.IsVehicle )
                     TankSites.Add( chr, pxbase.Site );
            }
         }
         foreach ( GeoCharacter tank in faction.GroundVehicles ) {
            if ( ! TankSites.ContainsKey( tank ) ) continue;
            Verbo( "Can scrap tank {0}", tank.DisplayName );
            vList.Add( new GeoTankWrapper( tank ) );
         }
         Info( "Can scrap {0} vehicles", vList.Count );
         availableItemRecipes = from t in vList select t;
      } catch ( Exception ex ) { Error( ex ); } }

      // Show confirmation popup which callback OnScrapConfirmation
      private static bool BeforeOnItemAction_ConfirmScrap ( UIModuleManufacturing __instance, GeoManufactureItem item,
                                                           MessageBox ____confirmationBox, GeoscapeViewContext ____context ) { try {
         if ( item.Manufacturable is GeoUnitWrapper unit ) {
            Verbo( "Confirming scraping of {0}", unit.GetName() );
            string scrapTxt = TitleCase( __instance.ScrapModeButton.GetComponentInChildren<Text>()?.text ?? "Scrap" );
            if ( scrapTxt == "Scrap Item" ) scrapTxt = "Scrap";
            string translation = scrapTxt + " " + unit.GetName() + "?";
            ____confirmationBox.ShowSimplePrompt(translation, MessageBoxIcon.Warning, MessageBoxButtons.YesNo,
               answer => OnScrapConfirmation( __instance, answer, unit, ____context ),
               __instance, MessageBox.DialogMode.DialogBox);
            return false;
         }
         return true;
      } catch ( Exception ex ) { return Error( ex ); } }

      // Do the scrap after user confirmation
      private static void OnScrapConfirmation ( UIModuleManufacturing me, MessageBoxCallbackResult answer, IManufacturable item, GeoscapeViewContext context ) { try {
         if ( answer.DialogResult != MessageBoxResult.Yes ) return;
         GeoFaction faction = context.ViewerFaction;
         GeoLevelController geoLevel = context.Level;

         if ( item is GeoUnitWrapper ) {
            if ( item is GeoPlaneWrapper plane ) {
               Info( "Scraping airplane {0}", plane.GetName() );
               GeoVehicle vehicle = plane.Vehicle;
               GeoSite site = vehicle.CurrentSite;
               foreach ( GeoCharacter chr in vehicle.Characters.ToList() ) {
                  Info( "Moving {0} to {1}", chr.DisplayName, site.Name );
                  vehicle.RemoveCharacter( chr );
                  site.AddCharacter( chr );
               }
               faction.ScrapItem( plane );
               vehicle.Travelling = true; // Unset vehicle.CurrentSite and triggers site.VehicleLeft
               vehicle.Destroy();
            } else if ( item is GeoTankWrapper tank ) {
               Info( "Scraping tank {0}", tank.GetName() );
               faction.ScrapItem( tank );
               faction.RemoveCharacter( tank.GroundVehicle );
               geoLevel.DestroyTacUnit( tank.GroundVehicle );
            }
         }
         Info( "Scrap done, refreshing list" );
         typeof( UIModuleManufacturing ).GetMethod( "DoFilter", NonPublic | Instance ).Invoke( me, new object[]{ null, null } );
      } catch ( Exception ex ) { Error( ex ); } }

      // Show vehicle name on scrap list
      private static void AftereInit_SetName ( GeoManufactureItem __instance, IManufacturable item ) { try {
         if ( item is GeoUnitWrapper unit ) {
            __instance.ItemName.text = unit.GetName();
            __instance.CurrentlyOwnedQuantityText.transform.parent.gameObject.SetActive( false );
         } else {
            __instance.CurrentlyOwnedQuantityText.transform.parent.gameObject.SetActive( true );
         }
      } catch ( Exception ex ) { Error( ex ); } }

      // Add mutagen to scrap value
      private static void AftereScrapPrice_AddMutagen ( ItemDef __instance, ResourcePack __result ) { try {
         if ( __instance.ManufactureMutagen <= 0 ) return;
         ResourceUnit res = __result.ByResourceType( ResourceType.Mutagen );
         if ( res.Value > 0 ) return;
         Verbo( "Awarding mutagen" );
         __result.Add( new ResourceUnit( ResourceType.Mutagen, Mathf.Floor( __instance.ManufactureMutagen / 2f ) ) );
      } catch ( Exception ex ) { Error( ex ); } }

      // Clear ItemDef mappings on close
      private static void AfterClose_Cleanup () {
         planeDefs = null;
         tankDefs = null;
         TankSites = null;
      }

      #region ItemDef mappings and Wrapper classes
      private static Dictionary<GeoVehicleDef, VehicleItemDef> planeDefs;
      private static Dictionary<TacUnitClassDef, GroundVehicleItemDef> tankDefs;

      private static void LoadVehicleDefs () { try {
         if ( planeDefs != null ) return;
         planeDefs = new Dictionary<GeoVehicleDef, VehicleItemDef>( 3 );
         tankDefs = new Dictionary<TacUnitClassDef, GroundVehicleItemDef>( 3 );

         Verbo( "Loading vehicles for scrap screen" );
         DefRepository defRepo = GameUtl.GameComponent<DefRepository>();
         foreach ( BaseDef def in defRepo.GetAllDefs<ItemDef>() ) {
            if ( def is GroundVehicleItemDef tankDef ) {
               TacUnitClassDef chrDef = tankDef.VehicleClassDef;
               if ( chrDef != null && ( chrDef.IsVehicle || chrDef.IsMutog ) )
                  tankDefs[ chrDef ] = tankDef;
            } else if ( def is VehicleItemDef planeDef ) {
               GeoVehicleDef vDef = planeDef.ComponentSetDef?.GetComponentDef<GeoVehicleDef>();
               if ( vDef != null )
                  planeDefs[ vDef ] = planeDef;
            }
         }
         Verbo( "Mapped {0} types of airplanes, {1} types of tanks", planeDefs.Count, tankDefs.Count );
      } catch ( Exception ex ) { Error( ex ); } }

      // General wrapper class that backs the scrap list
      private abstract class GeoUnitWrapper : GeoItem, IManufacturable {
         internal GeoUnitWrapper ( ItemDef def ) : base( def ) {}
         public ResourcePack ManufacturePrice => ItemDef.ManufacturePrice;
         public ItemDef RelatedItemDef => ItemDef;
         public Sprite SmallIcon => ItemDef.GetSmallIcon();
         public Sprite DetailedImage => ItemDef.GetDetailedImage();
         public bool IsInstant => false;
         public List<GameTagDef> RequiredTagDefs => new List<GameTagDef>();
         public ItemManufacturing.ManufactureFailureReason CanManufacture ( GeoFaction faction ) => ItemManufacturing.ManufactureFailureReason.NotManufacturable;
         public void OnManufacture ( GeoFaction faction ) => ZyMod.Warn( "Attempting to manufacture {0}", GetName() );
         public float GetCostInManufacturePoints ( GeoFaction faction ) => faction.Def.UseHavenManufacturing ? GetCostInManufacturePoints( faction ) : GetFactoryManufactureCost();
         protected abstract float GetFactoryManufactureCost();
         public abstract string GetName();
         public ItemManufacturing.ManufactureFailureReason CanFinishManufacture ( GeoFaction faction ) => ItemManufacturing.ManufactureFailureReason.NotManufacturable;
         public bool CanProgressManufacture ( GeoFaction faction ) => false;
      }

      private class GeoPlaneWrapper : GeoUnitWrapper {
         public readonly GeoVehicle Vehicle;
         public GeoPlaneWrapper ( GeoVehicle vehicle ) : base( planeDefs[ vehicle.VehicleDef ] ) { this.Vehicle = vehicle; }
         public override string GetName () => Vehicle.Name;
         protected override float GetFactoryManufactureCost () => ( ItemDef as VehicleItemDef ).FactoryManufactureCost;
      }

      private class GeoTankWrapper : GeoUnitWrapper {
         public readonly GeoCharacter GroundVehicle;
         public GeoTankWrapper ( GeoCharacter vehicle ) : base( tankDefs[ vehicle.ClassDef ] ) { this.GroundVehicle = vehicle; }
         public override string GetName () => GroundVehicle.DisplayName;
         protected override float GetFactoryManufactureCost () => ( ItemDef as VehicleItemDef ).FactoryManufactureCost;
      }
      #endregion
   }
}