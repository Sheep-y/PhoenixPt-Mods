using Base.Core;
using Base.UI;
using Harmony;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.View.ViewControllers.HavenDetails;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Sheepy.PhoenixPt.RecruitInfo {

   internal class ModConfig {
      public ListConfig Skills = new ListConfig();
      public ListConfig Grafts = new ListConfig();
      public ListConfig Equipments = new ListConfig{ Name = "<size=36>...</size>" };
      public int Config_Version = 20200416;
   }

   public class ListConfig {
      public bool Enabled = true;
      public bool Mouse_Over_Desc = true;
      public string Name = "<size=42>...</size>";
      public string Desc_Title = "<size=36><b>...</b></size>";
      public string Desc_Body = "<size=30>...</size>";
   }

   public class Mod : ZyMod {
      internal static ModConfig Config;

      public static void Init () => new Mod().MainMod();

      public void MainMod ( Func< string, object, object > api = null ) {
         SetApi( api, out Config );
         if ( Config.Grafts.Enabled ) {
            var sharedData = GameUtl.GameComponent<SharedData>();
            GraftInfo.AnuMutation = sharedData.SharedGameTags.AnuMutationTag;
            GraftInfo.BioAugTag = sharedData.SharedGameTags.BionicalTag;
         }
         if ( Config.Skills.Enabled || Config.Grafts.Enabled || Config.Equipments.Enabled )
            Patch( typeof( HavenFacilityItemController ), "SetRecruitmentGroup", postfix: nameof( AfterSetRecruitment_ListPerks ) );
      }

      private static GameObject CreateNameText ( HavenFacilityItemController ctl, string name, bool hasHint ) {
         var owner = ctl.RecruitName?.transform?.parent?.parent;
         if ( owner == null ) throw new InvalidOperationException( "Recruit group not found, cannot add recruit info text" );
         var infoText = owner.Find( name )?.gameObject;
         if ( infoText != null ) return infoText;

         Verbo( "Creating {0} Text", name );
         var copyFrom = owner.Find( "RecruitCost" )?.Find( "Name" )?.GetComponent<UnityEngine.UI.Text>() ?? ctl.RecruitName;
         infoText = new GameObject( name );
         infoText.transform.SetParent( owner );

         var text = infoText.AddComponent<UnityEngine.UI.Text>();
         text.font = copyFrom.font;
         text.fontStyle = copyFrom.fontStyle; // RecruitName Bold, Cost normal
         text.fontSize = 42; // RecruitName 52, Cost 42
         text.resizeTextForBestFit = true;
         text.resizeTextMinSize = 10;

         var rect = infoText.GetComponent<Transform>();
         //rect.localPosition = new Vector3( 200, 0, 0 );
         rect.localScale = new Vector3( 1, 1, 1 );

         var outline = infoText.AddComponent<UnityEngine.UI.Outline>();
         outline.effectColor = Color.black;
         outline.effectDistance = new Vector2( 2, 2 );

         if ( hasHint ) infoText.AddComponent<UITooltipText>();
         infoText.AddComponent<CanvasRenderer>();
         return infoText;
      }

      private static string BuildHint ( KeyValuePair< string, string > pair, string[] tags ) =>
         $"{tags[0]}{pair.Key}{tags[1]}\n{tags[2]}{pair.Value}{tags[3]}";

      private static void AfterSetRecruitment_ListPerks ( HavenFacilityItemController __instance, GeoHaven ____haven ) { try {
         Info( "Creating Info Panel" );
         var recruit = ____haven?.AvailableRecruit;
         if ( Config.Skills.Enabled )
            ShowRecruitInfo( __instance, "PersonalSkills", new PersonalSkillInfo( recruit ) );
         if ( Config.Grafts.Enabled )
            ShowRecruitInfo( __instance, "GraftList", new GraftInfo( recruit ) );
         if ( Config.Equipments.Enabled )
            ShowRecruitInfo( __instance, "EquipmentList", new EquipmentInfo( recruit ) );
         //ModnixApi?.Invoke( "zy.ui.dump", __instance.gameObject );
      } catch ( Exception ex ) { Error( ex ); } }

      private static void ShowRecruitInfo ( HavenFacilityItemController __instance, string id, RecruitInfo info ) { try {
         var names = info.GetNames();
         GameObject infoText = CreateNameText( __instance, id, info.Config.Mouse_Over_Desc );
         if ( ! names.Any() ) {
            infoText.SetActive( false );
            return;
         }
         infoText.GetComponent<UnityEngine.UI.Text>().text = "     " + info.NameTags[0] + names.Join() + info.NameTags[1];
         if ( info.Config.Mouse_Over_Desc ) {
            info.SetTags();
            infoText.GetComponent<UITooltipText>().TipText = info.GetDesc().Join( e => BuildHint( e, info.DescTags ), "\n\n" );
         }
      } catch ( Exception ex ) { Error( ex ); } }
   }

   internal abstract class RecruitInfo {
      protected readonly GeoCharacter recruit;

      protected RecruitInfo ( GeoCharacter recruit ) {
         this.recruit = recruit;
         NameTags = new string[] { Prefix( Config.Name ), Postfix( Config.Name ) };
      }

      protected internal abstract ListConfig Config { get; }

      #region Tags
      internal string[] NameTags { get; }
      internal string[] DescTags { get; private set; }

      internal void SetTags () {
         if ( DescTags == null )
            DescTags = new string[] { Prefix( Config.Desc_Title ), Postfix( Config.Desc_Title ), Prefix( Config.Desc_Body ), Postfix( Config.Desc_Body ) };
      }

      private string Prefix ( string txt ) {
         if ( string.IsNullOrEmpty( txt ) ) return "";
         var pos = txt.IndexOf( "..." );
         return pos <= 0 ? "" : txt.Substring( 0, pos );
      }

      private string Postfix ( string txt ) {
         if ( string.IsNullOrEmpty( txt ) ) return "";
         var pos = txt.LastIndexOf( "..." );
         return pos < 0 ? "" : txt.Substring( pos+3 );
      }

      protected KeyValuePair< string, string > Pair ( string title, string body ) => new KeyValuePair< string, string >( title, body );
      protected KeyValuePair< string, string > Pair ( LocalizedTextBind title, LocalizedTextBind body ) => Pair( title.Localize(), body.Localize() );
      #endregion

      protected abstract IEnumerable< ViewElementDef > Items { get; }

      protected IEnumerable< ViewElementDef > Map ( IEnumerable< GeoItem > items, Func< ItemDef, bool > filter = null ) {
         var defs = items.Select( e => e.ItemDef );
         if ( filter != null ) defs = defs.Where( filter );
         return defs.Select( e => e.ViewElementDef ).Where( e => e != null );
      }

      internal virtual IEnumerable<string> GetNames () => GetDesc().Select( e => e.Key );

      internal virtual IEnumerable<KeyValuePair<string,string>> GetDesc() {
         foreach ( var e in Items ) yield return Pair( e.DisplayName1, e.Description );
      }
   }

   internal class PersonalSkillInfo : RecruitInfo {

      internal PersonalSkillInfo ( GeoCharacter recruit ) : base( recruit ) { }

      private ViewElementDef MainClass => recruit.Progression.MainSpecDef.ViewElementDef;

      protected override IEnumerable< ViewElementDef > Items =>
         recruit.Progression?.AbilityTracks?.Where( e => e?.Source == AbilityTrackSource.Personal )
         ?.SelectMany( e => e.AbilitiesByLevel?.Select( a => a?.Ability?.ViewElementDef ) ).Where( e => e != null );

      protected internal override ListConfig Config => Mod.Config.Skills;

      internal override IEnumerable<string> GetNames () => base.GetDesc().Select( e => e.Key );

      internal override IEnumerable<KeyValuePair<string, string>> GetDesc () => ClassDesc().Concat( base.GetDesc() );

      private IEnumerable<KeyValuePair<string, string>> ClassDesc () { yield return Pair( MainClass.DisplayName1, MainClass.DisplayName2 ); }
   }

   internal class GraftInfo : RecruitInfo {

      internal static GameTagDef AnuMutation, BioAugTag;

      internal GraftInfo ( GeoCharacter recruit ) : base( recruit ) { }

      protected internal override ListConfig Config => Mod.Config.Skills;

      protected override IEnumerable< ViewElementDef > Items => Map( recruit.ArmourItems, IsGraft );

      internal static bool IsGraft ( ItemDef def ) => def.Tags.Any( tag => tag == AnuMutation || tag == BioAugTag );
   }

   internal class EquipmentInfo : RecruitInfo {

      internal EquipmentInfo ( GeoCharacter recruit ) : base( recruit ) { }

      protected internal override ListConfig Config => Mod.Config.Equipments;

      protected override IEnumerable< ViewElementDef > Items => Equipments.Concat( Armours );

      private IEnumerable< ViewElementDef > Equipments => Map( recruit.EquipmentItems.Concat( recruit.InventoryItems ) );

      private IEnumerable< ViewElementDef > Armours => Map( recruit.ArmourItems, Mod.Config.Grafts.Enabled ?
         ( Func< ItemDef, bool > ) ( e => ! GraftInfo.IsGraft( e ) ) : null );

      internal override IEnumerable<KeyValuePair<string, string>> GetDesc () {
         foreach ( var e in Equipments ) yield return Pair( e.DisplayName1.Localize(), e.Description.Localize() );
         foreach ( var e in Armours ) yield return Pair( e.Name, e.Description.Localize() );
      }
   }
}