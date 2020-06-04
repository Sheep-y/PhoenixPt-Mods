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
using System.Text.RegularExpressions;
using UnityEngine;

namespace Sheepy.PhoenixPt.RecruitInfo {

   internal class ModConfig {
      public ListConfig Skills = new ListConfig{ List_Names = true };
      public ListConfig Grafts { internal get; set; }
      public ListConfig Augments = new ListConfig();
      public ListConfig Equipments = new ListConfig{ Name = "<size=36>...</size>" };
      public int Config_Version = 20200604;

      internal void Upgrade () {
         if ( Config_Version < 20200604 ) {
            if ( Grafts != null ) Augments = Grafts;
            Config_Version = 20200604;
            ZyMod.Api( "config save", this );
         }
         if ( Skills == null ) Skills = new ListConfig{ Enabled = false };
         if ( Augments == null ) Augments = new ListConfig{ Enabled = false };
         if ( Equipments == null ) Equipments = new ListConfig{ Enabled = false };
      }
   }

   internal class ListConfig {
      public bool Enabled = true;
      public bool List_Names = false;
      public bool Mouse_Over_Popup = true;
      public string Name = "<size=42>...</size>";
      public string Popup_Title = "<size=36><b>...</b></size>";
      public string Popup_Body = "<size=30>...</size>";
   }

   public class Mod : ZyMod {
      internal static ModConfig Config;

      public static void Init () => new Mod().GeoscapeMod();

      public void MainMod ( Func< string, object, object > api ) => GeoscapeMod( api );

      public void GeoscapeMod ( Func< string, object, object > api = null ) {
         SetApi( api, out Config ).Upgrade();
         if ( Config.Augments.Enabled ) {
            var sharedData = GameUtl.GameComponent<SharedData>();
            AugInfo.AnuMutation = sharedData.SharedGameTags.AnuMutationTag;
            AugInfo.BioAugTag = sharedData.SharedGameTags.BionicalTag;
         }
         if ( Config.Skills.Enabled || Config.Augments.Enabled || Config.Equipments.Enabled )
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
         var recruit = ____haven?.AvailableRecruit;
         if ( recruit == null ) return;
         Info( "Showing info of {0}", recruit.DisplayName );
         if ( Config.Skills.Enabled )
            ShowRecruitInfo( __instance, "PersonalSkills", new PersonalSkillInfo( recruit ) );
         if ( Config.Augments.Enabled )
            ShowRecruitInfo( __instance, "AugmentList", new AugInfo( recruit ) );
         if ( Config.Equipments.Enabled )
            ShowRecruitInfo( __instance, "EquipmentList", new EquipmentInfo( recruit ) );
         //ModnixApi?.Invoke( "zy.ui.dump", __instance.gameObject );
      } catch ( Exception ex ) { Error( ex ); } }

      private static void ShowRecruitInfo ( HavenFacilityItemController __instance, string id, RecruitInfo info ) { try {
         var names = info.GetNames();
         GameObject infoText = CreateNameText( __instance, id, info.Config.Mouse_Over_Popup );
         if ( ! names.Any() ) {
            infoText.SetActive( false );
            return;
         }
         var text = "     " + info.NameTags[0];
         if ( info.Config.List_Names || names.Count() == 1 )
            text += names.Join();
         else
            text += TitleCase( new LocalizedTextBind( info.Count_Label ).Localize() ) + " x" + names.Count();
         infoText.GetComponent<UnityEngine.UI.Text>().text = text + info.NameTags[1];
         if ( info.Config.Mouse_Over_Popup ) {
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

      protected internal abstract string Count_Label { get; }

      #region Tags
      internal string[] NameTags { get; }
      internal string[] DescTags { get; private set; }

      internal void SetTags () {
         if ( DescTags == null )
            DescTags = new string[] { Prefix( Config.Popup_Title ), Postfix( Config.Popup_Title ), Prefix( Config.Popup_Body ), Postfix( Config.Popup_Body ) };
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
      #endregion

      #region Helpers
      private static readonly Regex RegexBodyParts = new Regex( "^KEY_(TORSO|HEAD|LEGS)_NAME$", RegexOptions.Compiled );

      protected KeyValuePair< string, string > Stringify ( ViewElementDef view ) {
         string title = view.DisplayName1.Localize(), desc = view.Description.Localize(), key = view.DisplayName1.LocalizationKey;
         if ( view is MutationViewElementDef mut ) { // Mutations
            title = view.DisplayName2.Localize();
            desc = mut.MutationDescription.Localize();
         } else if ( key.IndexOf( "_PERSONALTRACK_", StringComparison.Ordinal ) >= 0 || key.IndexOf( "KEY_CLASS_", StringComparison.Ordinal ) == 0 ) { // Class & Skills
            title = ZyMod.TitleCase( title );
         } else if ( RegexBodyParts.IsMatch( key ) ) // Armours
            title = view.DisplayName2.Localize();
         return new KeyValuePair<string, string>( title, desc );
      }

      protected IEnumerable< ViewElementDef > Map ( IEnumerable< GeoItem > items, Func< ItemDef, bool > filter = null ) {
         var defs = items.Select( e => e.ItemDef );
         if ( filter != null ) defs = defs.Where( filter );
         return defs.Select( e => e.ViewElementDef ).Where( e => e != null );
      }
      #endregion

      protected abstract IEnumerable< ViewElementDef > Items { get; }

      internal virtual IEnumerable<string> GetNames () => GetDesc().Select( e => e.Key ).Where( e => ! string.IsNullOrEmpty( e ) );

      internal virtual IEnumerable<KeyValuePair<string,string>> GetDesc() {
         if ( Items == null ) yield break;
         foreach ( var e in Items ) yield return Stringify( e );
      }
   }

   internal class PersonalSkillInfo : RecruitInfo {

      internal PersonalSkillInfo ( GeoCharacter recruit ) : base( recruit ) { }

      private ViewElementDef MainClass => recruit.Progression.MainSpecDef.ViewElementDef;

      protected override IEnumerable< ViewElementDef > Items =>
         recruit.Progression?.AbilityTracks?.Where( e => e?.Source == AbilityTrackSource.Personal )
         ?.SelectMany( e => e.AbilitiesByLevel?.Select( a => a?.Ability?.ViewElementDef ) ).Where( e => e != null );

      protected internal override ListConfig Config => Mod.Config.Skills;

      protected internal override string Count_Label => "Roster Screen/KEY_GEOROSTER_PROGRESS"; // "Training"

      internal override IEnumerable<string> GetNames () => base.GetDesc().Select( e => e.Key );

      internal override IEnumerable<KeyValuePair<string, string>> GetDesc () => ClassDesc().Concat( base.GetDesc() );

      private IEnumerable<KeyValuePair<string, string>> ClassDesc () { yield return Stringify( MainClass ); }
   }

   internal class AugInfo : RecruitInfo {

      internal static GameTagDef AnuMutation, BioAugTag;

      internal AugInfo ( GeoCharacter recruit ) : base( recruit ) { }

      protected internal override ListConfig Config => Mod.Config.Augments;

      protected internal override string Count_Label => "Blood Titanium/KEY_AUGMENT"; // "Augmentation"

      protected override IEnumerable< ViewElementDef > Items => Map( recruit.ArmourItems, IsAugment );

      internal static bool IsAugment ( ItemDef def ) => def.Tags.Any( tag => tag == AnuMutation || tag == BioAugTag );
   }

   internal class EquipmentInfo : RecruitInfo {

      internal EquipmentInfo ( GeoCharacter recruit ) : base( recruit ) { }

      protected internal override ListConfig Config => Mod.Config.Equipments;

      protected internal override string Count_Label => "Equipping Screen/KEY_GEOSCAPE_EQUIPMENT"; // Equipment

      protected override IEnumerable< ViewElementDef > Items => Equipments.Concat( Armours );

      private IEnumerable< ViewElementDef > Equipments => Map( recruit.EquipmentItems.Concat( recruit.InventoryItems ) );

      private IEnumerable< ViewElementDef > Armours => Map( recruit.ArmourItems, Mod.Config.Augments.Enabled ?
         ( Func< ItemDef, bool > ) ( e => ! AugInfo.IsAugment( e ) ) : null );

      internal override IEnumerable<KeyValuePair<string, string>> GetDesc () {
         foreach ( var e in Equipments ) yield return Stringify( e );
         foreach ( var e in Armours ) yield return Stringify( e );
      }
   }
}