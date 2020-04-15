using Base.Core;
using Base.UI;
using Harmony;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.View.ViewControllers.HavenDetails;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Sheepy.PhoenixPt.RecruitInfo {

   internal class ModConfig {
      public DisplayConfig Display = new DisplayConfig();
      public FontSizeConfig Font_Sizes = new FontSizeConfig();
      public int Config_Version = 20200415;
   }

   public class DisplayConfig {
      public bool Personal_Skill_Names = true;
      public bool Personal_Skill_Desc = true;
      public bool Main_Class_Desc = true;
      public bool Graft_Names = true;
      public bool Graft_Desc = true;
      public bool Equipment_Names = true;
      public bool Equipment_Desc = true;

      public bool Any_Desc => Main_Class_Desc || Personal_Skill_Desc;
      internal void Validate () {
         if ( ! Personal_Skill_Names && Main_Class_Desc )
            ZyMod.Warn( "Main_Class_Desc will not work without Personal_Skill_Names" );
         if ( ! Personal_Skill_Names && Personal_Skill_Desc )
            ZyMod.Warn( "Personal_Skill_Desc will not work without Personal_Skill_Names" );
         if ( ! Graft_Names && Graft_Desc )
            ZyMod.Warn( "Graft_Desc will not work without Graft_Names" );
         if ( ! Equipment_Names && Equipment_Desc )
            ZyMod.Warn( "Equipment_Desc will not work without Equipment_Names" );
      }
   }

   public class FontSizeConfig {
      public int Names = 42;
      public int Desc_Title = 36;
      public int Desc_Body = 30;
   }

   public class Mod : ZyMod {
      internal static ModConfig Config;

      public static void Init () => new Mod().MainMod();

      public void MainMod ( Func< string, object, object > api = null ) {
         SetApi( api, out Config );
         Config.Display.Validate();
         if ( Config.Display.Graft_Names ) {
            var sharedData = GameUtl.GameComponent<SharedData>();
            GraftSkillInfo.AnuMutation = sharedData.SharedGameTags.AnuMutationTag;
            GraftSkillInfo.BioAugTag = sharedData.SharedGameTags.BionicalTag;
         }
         Patch( typeof( HavenFacilityItemController ), "SetRecruitmentGroup", postfix: nameof( AfterSetRecruitment_ListPerks ) );
      }

      private static GameObject CreateNameText ( HavenFacilityItemController ctl, string name, bool hasHint ) {
         var owner = ctl.RecruitName?.transform?.parent?.parent;
         if ( owner == null ) throw new InvalidOperationException( "Recruit group not found, cannot add recruit info text" );
         var infoText = owner.Find( name )?.gameObject;
         if ( infoText != null ) return infoText;

         Verbo( "Creating {0} text", name );
         var copyFrom = owner.Find( "RecruitCost" )?.Find( "Name" )?.GetComponent<UnityEngine.UI.Text>() ?? ctl.RecruitName;
         infoText = new GameObject( name );
         infoText.transform.SetParent( owner );

         var text = infoText.AddComponent<UnityEngine.UI.Text>();
         text.font = copyFrom.font;
         text.fontStyle = copyFrom.fontStyle; // RecruitName Bold, Cost normal
         text.fontSize = Config.Font_Sizes.Names; // RecruitName 52, Cost 42
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

      private static string BuildHint ( KeyValuePair< string, string > pair ) {
         return $"<size={Config.Font_Sizes.Desc_Title}><b>{pair.Key}</b></size>\n" +
                $"<size={Config.Font_Sizes.Desc_Body}>{pair.Value}</size>";
      }

      private static void AfterSetRecruitment_ListPerks ( HavenFacilityItemController __instance, GeoHaven ____haven ) { try {
         var recruit = ____haven?.AvailableRecruit;
         Info( "Creating Info Text" );
         if ( Config.Display.Personal_Skill_Names )
            ShowRecruitInfo( __instance, "PersonalSkills", new PersonalSkillInfo( recruit ) );
         if ( Config.Display.Graft_Names )
            ShowRecruitInfo( __instance, "GraftList", new GraftSkillInfo( recruit ) );
         if ( Config.Display.Equipment_Names )
            ShowRecruitInfo( __instance, "EquipmentList", new EquipmentInfo( recruit ) );
         //ModnixApi?.Invoke( "zy.ui.dump", __instance.gameObject );
      } catch ( Exception ex ) { Error( ex ); } }

      private static void ShowRecruitInfo ( HavenFacilityItemController __instance, string id, RecruitInfo info ) { try {
         var names = info.GetNames();
         GameObject infoText = CreateNameText( __instance, id, info.ShouldShowDesc );
         if ( ! names.Any() ) {
            infoText.SetActive( false );
            return;
         }
         infoText.GetComponent<UnityEngine.UI.Text>().text = "     " + names.Join();
         if ( info.ShouldShowDesc )
            infoText.GetComponent<UITooltipText>().TipText = info.GetDesc().Join( BuildHint, "\n\n" );
      } catch ( Exception ex ) { Error( ex ); } }
   }

   internal abstract class RecruitInfo {
      protected readonly GeoCharacter recruit;

      protected RecruitInfo ( GeoCharacter recruit ) => this.recruit = recruit;

      internal abstract bool ShouldShowDesc { get; }

      protected abstract IEnumerable< ViewElementDef > Items { get; }

      internal virtual IEnumerable<string> GetNames () => Items.Select( e => ZyMod.TitleCase( e.DisplayName1?.Localize() ) );

      internal virtual IEnumerable<KeyValuePair<string,string>> GetDesc() {
         foreach ( var e in Items ) yield return Pair( e.DisplayName1, e.Description );
      }

      protected KeyValuePair< string, string > Pair ( string title, string body ) => new KeyValuePair< string, string >( title, body );
      protected KeyValuePair< string, string > Pair ( LocalizedTextBind title, LocalizedTextBind body ) => Pair( title.Localize(), body.Localize() );
   }

   internal class PersonalSkillInfo : RecruitInfo {

      internal PersonalSkillInfo ( GeoCharacter recruit ) : base( recruit ) { }

      private ViewElementDef MainClass => recruit.Progression.MainSpecDef.ViewElementDef;

      protected override IEnumerable< ViewElementDef > Items =>
         recruit.Progression?.AbilityTracks?.Where( e => e?.Source == AbilityTrackSource.Personal )
         ?.SelectMany( e => e.AbilitiesByLevel?.Select( a => a?.Ability?.ViewElementDef ) ).Where( e => e != null );

      internal override bool ShouldShowDesc => Mod.Config.Display.Main_Class_Desc || Mod.Config.Display.Personal_Skill_Desc;

      internal override IEnumerable<KeyValuePair<string, string>> GetDesc () {
         if ( Mod.Config.Display.Main_Class_Desc )
            yield return Pair( MainClass.DisplayName1, MainClass.DisplayName2 );
         if ( Mod.Config.Display.Personal_Skill_Desc )
            foreach ( var e in Items )
               yield return Pair( e.DisplayName1, e.Description );
      }
   }

   internal class GraftSkillInfo : RecruitInfo {

      internal static GameTagDef AnuMutation, BioAugTag;

      internal GraftSkillInfo ( GeoCharacter recruit ) : base( recruit ) { }

      protected override IEnumerable< ViewElementDef > Items => recruit.ArmourItems.Select( e => e.ItemDef )
         .Where( e => e.Any( tag => tag == AnuMutation || tag == BioAugTag  ) ).Select( e => e.ViewElementDef );

      internal override bool ShouldShowDesc => Mod.Config.Display.Graft_Desc;
   }

   internal class EquipmentInfo : RecruitInfo {

      internal EquipmentInfo ( GeoCharacter recruit ) : base( recruit ) { }

      protected override IEnumerable< ViewElementDef > Items =>
         recruit.EquipmentItems.Concat( recruit.ArmourItems ).Concat( recruit.InventoryItems ).Select( e => e.ItemDef )
         .Where( e => e.All( tag => tag != GraftSkillInfo.AnuMutation && tag != GraftSkillInfo.BioAugTag  ) )
         .Select( e => e.ViewElementDef );

      internal override bool ShouldShowDesc => Mod.Config.Display.Equipment_Desc;
   }
}