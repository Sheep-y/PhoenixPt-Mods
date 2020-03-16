using System;
using System.Linq;
using System.Reflection;

namespace Sheepy.PhoenixPt_ScrapVehicle {
   using Base.Core;
   using Base.Defs;
   using Base.UI.MessageBox;
   using Harmony;
   using PhoenixPoint.Common.Core;
   using PhoenixPoint.Common.Entities;
   using PhoenixPoint.Common.Entities.Items;
   using PhoenixPoint.Common.View.ViewControllers;
   using PhoenixPoint.Geoscape.Entities;
   using PhoenixPoint.Geoscape.Entities.Sites;
   using PhoenixPoint.Geoscape.Levels;
   using PhoenixPoint.Geoscape.Levels.Factions;
   using PhoenixPoint.Geoscape.View;
   using PhoenixPoint.Geoscape.View.ViewControllers.Manufacturing;
   using PhoenixPoint.Geoscape.View.ViewModules;
   using PhoenixPoint.Tactical.Entities;
   using System.Collections.Generic;
   using UnityEngine;
   using UnityEngine.UI;
   using static System.Reflection.BindingFlags;

   public class Mod {
      //private static Logging.Logger Log = new Logging.Logger( "Mods/SheepyMods.log" );

      private static Type UiType;

      public static void Init () {
         HarmonyInstance harmony = HarmonyInstance.Create( typeof( Mod ).Namespace );

         UiType = typeof( UIModuleManufacturing );
         Patch( harmony, UiType, "SetupClassFilter", null, "AfterSetupClassFilter_CheckScrapMode" );
         Patch( harmony, UiType, "SetupQueue", "BeforeSetupQueue_AddVehicleToScrap" );
         Patch( harmony, UiType, "RefreshFilters", null, "AfterRefreshFilters_EnableVehicleTab" );
         Patch( harmony, UiType, "RefreshItemList", "BeforeRefreshItemList_FillWithVehicle" );
         Patch( harmony, UiType, "OnItemAction", "BeforeOnItemAction_ConfirmScrap" );
         Patch( harmony, UiType, "Close", null, "AfterClose_Cleanup" );
         Patch( harmony, typeof( GeoManufactureItem ), "Init", null, "AftereInit_SetName" );
         Patch( harmony, typeof( ItemDef ), "get_ScrapPrice", null, "AftereScrapPrice_AddMutagen" );
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

      private static void LogError ( Exception ex ) {
         //Log.Error( ex );
      }
      #endregion

      #region General helpers
      private static bool IsScrapping ( UIModuleManufacturing __instance ) {
         return __instance.Mode == UIModuleManufacturing.UIMode.Scrap;
      }

      private static bool IsScrappingVehicles ( UIModuleManufacturing __instance, PhoenixGeneralButton _activeFilterButton ) {
         return IsScrapping( __instance ) && _activeFilterButton == __instance.VehiclesFilterButton;
      }

      private static string TitleCase ( string txt ) {
         return txt.Split( new char[]{ ' ' } ).Join( e => e.Substring(0,1).ToUpper() + e.Substring(1).ToLower(), " " );
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

      // Check whether scrap list need to be populated
      public static void AfterSetupClassFilter_CheckScrapMode ( UIModuleManufacturing __instance, ItemStorage ____scrapStorage ) { try {
         NeedToAddVehicles = IsScrapping( __instance ) && ____scrapStorage.IsEmpty;
      } catch ( Exception ex ) { LogError( ex ); } }

      // If scrap list need to be populated, do it
      public static void BeforeSetupQueue_AddVehicleToScrap ( GeoscapeViewContext ____context, ItemStorage ____scrapStorage ) { try {
         if ( ! NeedToAddVehicles ) return;
         LoadVehicleDefs();

         GeoPhoenixFaction faction = ____context.ViewerFaction as GeoPhoenixFaction;
         foreach ( GeoVehicle v in faction.Vehicles ) {
            // Log.Info( "Add {0} to scrap storage", v.Name );
            ____scrapStorage.AddItem( new GeoVehicleWrapper( v ) );
         }

         NeedToAddVehicles = false;
      } catch ( Exception ex ) { LogError( ex ); } }

      // Enable vehicle scrap tab
      public static void AfterRefreshFilters_EnableVehicleTab ( UIModuleManufacturing __instance, PhoenixGeneralButton ____activeFilterButton ) { try {
         if ( __instance.Mode != UIModuleManufacturing.UIMode.Scrap ) return;
         __instance.VehiclesFilterButton.SetInteractable( true );
         __instance.VehiclesFilterButton.GetComponent<UITooltipText>().TipKey = __instance.VehiclesTooltipText;
         /* // Show / hide class filters as appropiate - disabled because filter state is not restored correctly after toggle
         UiType.GetMethod( "SetClassFiltersAvailability", NonPublic | Instance )
            .Invoke( __instance, new object[]{ ____activeFilterButton != __instance.VehiclesFilterButton } ); */
      } catch ( Exception ex ) { LogError( ex ); } }

      // Replace scrap list with individual vehicles when applicable
      public static void BeforeRefreshItemList_FillWithVehicle ( UIModuleManufacturing __instance, ref IEnumerable<IManufacturable> availableItemRecipes,
                                                                 PhoenixGeneralButton ____activeFilterButton, GeoscapeViewContext ____context ) { try {
         if ( availableItemRecipes == null || availableItemRecipes.GetType() == typeof( List<IManufacturable> ) ) return;
         if ( ! IsScrappingVehicles( __instance, ____activeFilterButton ) ) return;

         List<IManufacturable> vList = new List<IManufacturable>();
         GeoPhoenixFaction faction = ____context.ViewerFaction as GeoPhoenixFaction;

         List<GeoTacUnit> scrappableTanks = new List<GeoTacUnit>();
         foreach ( GeoVehicle plane in faction.Vehicles )
            if ( CanScrap( plane, true ) ) {
               vList.Add( new GeoVehicleWrapper( plane ) );
               scrappableTanks.AddRange( plane.Characters.Where( e => e.ClassDef.IsVehicle || e.ClassDef.IsMutog ) );
            }
         foreach ( GeoPhoenixBase pxbase in faction.Bases ) {
            scrappableTanks.AddRange( pxbase.Site.TacUnits.Where( e => e.ClassDef.IsMutog ) );
            if ( CanScrapVehicles( pxbase ) )
               scrappableTanks.AddRange( pxbase.Site.TacUnits.Where( e => e.ClassDef.IsVehicle ) );
         }
         foreach ( GeoCharacter tank in faction.GroundVehicles )
            if ( scrappableTanks.Contains( tank ) )
               vList.Add( new GeoGroundVehicleWrapper( tank ) );

         availableItemRecipes = from t in vList select t;
      } catch ( Exception ex ) { LogError( ex ); } }

      // Show confirmation popup which callback OnScrapConfirmation
      public static bool BeforeOnItemAction_ConfirmScrap ( UIModuleManufacturing __instance, GeoManufactureItem item,
                                                           MessageBox ____confirmationBox, GeoscapeViewContext ____context ) { try {
         if ( item.Manufacturable is GeoUnitWrapper ) {
            string scrapTxt = TitleCase( __instance.ScrapModeButton.GetComponentInChildren<Text>()?.text ?? "Scrap" );
            if ( scrapTxt == "Scrap Item" ) scrapTxt = "Scrap";
            string translation = scrapTxt + " " + item.ItemName.text + "?";
            ____confirmationBox.ShowModal(translation, MessageBoxIcon.Warning, MessageBoxButtons.YesNo,
               answer => OnScrapConfirmation( __instance, answer, item.Manufacturable, ____context ),
               __instance, MessageBox.DialogMode.DialogBox);
            return false;
         }
         return true;
      } catch ( Exception ex ) { LogError( ex ); return true; } }

      // Do the scrap after user confirmation
      private static void OnScrapConfirmation ( UIModuleManufacturing me, MessageBoxCallbackResult answer, IManufacturable item, GeoscapeViewContext context ) { try {
         if ( answer.DialogResult != MessageBoxResult.Yes ) return;
         GeoFaction faction = context.ViewerFaction;
         GeoLevelController geoLevel = context.Level;

         if ( item is GeoUnitWrapper ) {
            if ( item is GeoVehicleWrapper plane ) {
               GeoVehicle vehicle = plane.Vehicle;
               GeoSite site = vehicle.CurrentSite;
               foreach ( GeoCharacter chr in vehicle.Characters.ToList() ) {
                  vehicle.RemoveCharacter( chr );
                  site.AddCharacter( chr );
               }
               faction.ScrapItem( plane );
               vehicle.Destroy();
            } else if ( item is GeoGroundVehicleWrapper tank ) {
               faction.ScrapItem( tank );
               faction.RemoveCharacter( tank.GroundVehicle );
               geoLevel.DestroyTacUnit( tank.GroundVehicle );
            }
         }
         UiType.GetMethod( "DoFilter", NonPublic | Instance ).Invoke( me, new object[]{ null, null } );
      } catch ( Exception ex ) { LogError( ex ); } }

      // Show vehicle name on scrap list
      public static void AftereInit_SetName ( GeoManufactureItem __instance, IManufacturable item ) { try {
         if ( item is GeoUnitWrapper unit ) {
            __instance.ItemName.text = unit.GetName();
            __instance.CurrentlyOwnedQuantityText.transform.parent.gameObject.SetActive( false );
         } else {
            __instance.CurrentlyOwnedQuantityText.transform.parent.gameObject.SetActive( true );
         }
      } catch ( Exception ex ) { LogError( ex ); } }

      // Add mutagen to scrap value
      public static void AftereScrapPrice_AddMutagen ( ItemDef __instance, ResourcePack __result ) { try {
         if ( __instance.ManufactureMutagen <= 0 ) return;
         ResourceUnit res = __result.ByResourceType( ResourceType.Mutagen );
         if ( res != null && res.Value > 0 ) return;
         __result.Add( new ResourceUnit( ResourceType.Mutagen, Mathf.Floor( __instance.ManufactureMutagen / 2f ) ) );
      } catch ( Exception ex ) { LogError( ex ); } }

      // Clear ItemDef mappings on close
      public static void AfterClose_Cleanup () {
         planeDefs = null;
         tankDefs = null;
      }

      #region ItemDef mappings and Wrapper classes
      private static Dictionary<GeoVehicleDef, VehicleItemDef> planeDefs;
      private static Dictionary<TacUnitClassDef, GroundVehicleItemDef> tankDefs;

      private static void LoadVehicleDefs () { try {
         if ( planeDefs != null ) return;
         planeDefs = new Dictionary<GeoVehicleDef, VehicleItemDef>( 3 );
         tankDefs = new Dictionary<TacUnitClassDef, GroundVehicleItemDef>( 3 );

         DefRepository defRepo = GameUtl.GameComponent<DefRepository>();
         foreach ( BaseDef def in defRepo.GetAllDefs<BaseDef>() ) {
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
      } catch ( Exception ex ) { LogError( ex ); } }

      // General wrapper class that backs the scrap list
      public abstract class GeoUnitWrapper : GeoItem, IManufacturable {
         protected GeoUnitWrapper ( ItemDef def ) : base( def ) {}
         private GeoUnitWrapper ( ItemUnit itemUnit ) : base( itemUnit ) { throw new NotSupportedException(); }
         private GeoUnitWrapper ( ItemData data ) : base( data ) { throw new NotSupportedException(); }
         private GeoUnitWrapper ( ItemDef def, CommonItemData commonData ) : base( def, commonData ) { throw new NotSupportedException(); }
         private GeoUnitWrapper ( ItemDef def, int count = 1, int charges = -1, AmmoManager ammo = null ) : base( def, count, charges, ammo ) { throw new NotSupportedException(); }
         public ResourcePack ManufacturePrice => ItemDef.ManufacturePrice;
         public ItemDef RelatedItemDef => ItemDef;
         public Sprite SmallIcon => ItemDef.GetSmallIcon();
         public Sprite DetailedImage => ItemDef.GetDetailedImage();
         public bool IsInstant => false;
         public ItemManufacturing.ManufactureFailureReason CanManufacture ( GeoFaction faction ) => ItemManufacturing.ManufactureFailureReason.NotManufacturable;
         public void OnManufacture ( GeoFaction faction ) => throw new NotImplementedException();
         public float GetCostInManufacturePoints ( GeoFaction faction ) {
            return faction.Def.UseHavenManufacturing ? GetCostInManufacturePoints( faction ) : GetFactoryManufactureCost();
         }
         protected abstract float GetFactoryManufactureCost();
         public abstract string GetName();
      }

      public class GeoVehicleWrapper : GeoUnitWrapper {
         public readonly GeoVehicle Vehicle;
         public GeoVehicleWrapper ( GeoVehicle vehicle ) : base( planeDefs[ vehicle.VehicleDef ] ) { this.Vehicle = vehicle; }
         public override string GetName () => Vehicle.Name;
         protected override float GetFactoryManufactureCost () => ( ItemDef as VehicleItemDef ).FactoryManufactureCost;
      }

      public class GeoGroundVehicleWrapper : GeoUnitWrapper {
         public readonly GeoCharacter GroundVehicle;
         public GeoGroundVehicleWrapper ( GeoCharacter vehicle ) : base( tankDefs[ vehicle.ClassDef ] ) { this.GroundVehicle = vehicle; }
         public override string GetName () => GroundVehicle.DisplayName;
         protected override float GetFactoryManufactureCost () => ( ItemDef as VehicleItemDef ).FactoryManufactureCost;
      }
      #endregion
   }
}