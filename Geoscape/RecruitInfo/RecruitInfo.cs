using Base.UI;
using Harmony;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.View.ViewControllers.HavenDetails;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static System.Diagnostics.TraceEventType;

namespace Sheepy.PhoenixPt.RecruitInfo {

   internal class ModConfig {
      public DisplayConfig Display = new DisplayConfig();
      public FontSizeConfig Font_Sizes = new FontSizeConfig();
      public int Config_Version = 20200413;
   }

   public class DisplayConfig {
      public bool Main_Class_Desc = true;
      public bool Personal_Skill_Names = true;
      public bool Personal_Skill_Desc = true;

      public bool Any_Desc => Main_Class_Desc || Personal_Skill_Desc;
      internal void Validate () {
         if ( ! Personal_Skill_Names && Main_Class_Desc )
            Mod.Logger?.Invoke( Warning, "Main_Class_Desc will not work without Personal_Skill_Names", null );
         if ( ! Personal_Skill_Names && Personal_Skill_Desc )
            Mod.Logger?.Invoke( Warning, "Personal_Skill_Desc will not work without Personal_Skill_Names", null );
      }
   }

   public class FontSizeConfig {
      public int Names = 42;
      public int Names_Min = 10;
      public int Desc_Title = 36;
      public int Desc_Body = 30;
   }

   public class Mod : ZyMod {
      internal static ModConfig Config;

      public static void Init () => new Mod().MainMod();

      public void MainMod ( Func< string, object, object > api = null ) {
         SetApi( api, out Config );
         Config.Display.Validate();
         Patch( typeof( HavenFacilityItemController ), "SetRecruitmentGroup", null, "AfterSetRecruitment_ListPerks" );
      }

      private static GameObject CreateNameText ( HavenFacilityItemController ctl, string name, bool hasHint ) {
         var owner = ctl.RecruitName?.transform?.parent?.parent;
         if ( owner == null ) throw new InvalidOperationException( "Recruit group not found, cannot add recruit info text" );
         var infoText = owner.Find( name )?.gameObject;
         if ( infoText != null ) return infoText;
         Info( "Create {0} Text", name );

         var copyFrom = owner.Find( "RecruitCost" )?.Find( "Name" )?.GetComponent<UnityEngine.UI.Text>() ?? ctl.RecruitName;
         infoText = new GameObject( name );
         infoText.transform.SetParent( owner );

         var text = infoText.AddComponent<UnityEngine.UI.Text>();
         text.font = copyFrom.font;
         text.fontStyle = copyFrom.fontStyle; // RecruitName Bold, Cost normal
         text.fontSize = Config.Font_Sizes.Names; // RecruitName 52, Cost 42
         text.resizeTextForBestFit = true;
         text.resizeTextMinSize = Config.Font_Sizes.Names_Min;

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
         if ( Config.Display.Personal_Skill_Names ) ShowRecruitInfo( __instance, "PersonalSkills", new PersonalSkillInfo( recruit ) );
         //ModnixApi?.Invoke( "zy.ui.dump", __instance.gameObject );
      } catch ( Exception ex ) { Error( ex ); } }

      private static void ShowRecruitInfo ( HavenFacilityItemController __instance, string id, RecruitInfo info ) { try {
         var names = info.GetNames();
         GameObject infoText = CreateNameText( __instance, id, info.ShowDesc );
         if ( ! names.Any() ) {
            infoText.SetActive( false );
            return;
         }
         infoText.GetComponent<UnityEngine.UI.Text>().text = "     " + names.Join();
         if ( info.ShowDesc )
            infoText.GetComponent<UITooltipText>().TipText = info.GetDesc().Join( BuildHint, "\n\n" );
      } catch ( Exception ex ) { Error( ex ); } }
   }

   internal abstract class RecruitInfo {
      protected readonly GeoCharacter recruit;
      protected RecruitInfo ( GeoCharacter recruit ) => this.recruit = recruit;
      internal abstract IEnumerable<string> GetNames();
      internal abstract bool ShowDesc { get; }
      internal abstract IEnumerable<KeyValuePair<string,string>> GetDesc();
      protected KeyValuePair< string, string > Pair ( string title, string body ) => new KeyValuePair< string, string >( title, body );
      protected KeyValuePair< string, string > Pair ( LocalizedTextBind title, LocalizedTextBind body ) => Pair( title.Localize(), body.Localize() );
   }

   internal class PersonalSkillInfo : RecruitInfo {

      internal PersonalSkillInfo ( GeoCharacter recruit ) : base( recruit ) { }

      private ViewElementDef MainClass => recruit.Progression.MainSpecDef.ViewElementDef;

      private IEnumerable< ViewElementDef > Abilities =>
         recruit.Progression?.AbilityTracks?.Where( e => e?.Source == AbilityTrackSource.Personal )
         ?.SelectMany( e => e.AbilitiesByLevel?.Select( a => a?.Ability?.ViewElementDef ) ).Where( e => e != null );

      internal override IEnumerable<string> GetNames () => Abilities.Select( e => ZyMod.TitleCase( e.DisplayName1?.Localize() ) );

      internal override bool ShowDesc => Mod.Config.Display.Main_Class_Desc || Mod.Config.Display.Personal_Skill_Desc;

      internal override IEnumerable<KeyValuePair<string, string>> GetDesc () {
         if ( Mod.Config.Display.Main_Class_Desc )
            yield return Pair( MainClass.DisplayName1, MainClass.DisplayName2 );
         if ( Mod.Config.Display.Personal_Skill_Desc )
            foreach ( var e in Abilities )
               yield return Pair( e.DisplayName1, e.Description );
      }
   }
}