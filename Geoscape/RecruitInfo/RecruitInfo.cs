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

      public void Init () => new Mod().MainMod();

      public void MainMod ( Func< string, object, object > api = null ) {
         SetApi( api, out Config );
         Config.Display.Validate();
         Patch( typeof( HavenFacilityItemController ), "SetRecruitmentGroup", null, "AfterSetRecruitment_ListPerks" );
      }

      private static GameObject CreateNameText ( HavenFacilityItemController ctl, string name ) {
         Transform owner = ctl.RecruitName?.transform?.parent?.parent;
         if ( owner == null ) throw new InvalidOperationException( "Recruit group not found, cannot add recruit info text" );
         GameObject skillObj = owner.Find( name )?.gameObject;

         if ( skillObj != null ) return skillObj;
         Info( "Create {0} Text", name );

         UnityEngine.UI.Text copyFrom = owner.Find( "RecruitCost" )?.Find( "Name" )?.GetComponent<UnityEngine.UI.Text>();
         if ( copyFrom == null ) copyFrom = ctl.RecruitName;
         skillObj = new GameObject( name );
         skillObj.transform.SetParent( owner );

         UnityEngine.UI.Text text = skillObj.AddComponent<UnityEngine.UI.Text>();
         text.font = copyFrom.font;
         text.fontStyle = copyFrom.fontStyle; // RecruitName Bold, Cost normal
         text.fontSize = Config.Font_Sizes.Names; // RecruitName 52, Cost 42
         text.resizeTextForBestFit = true;
         text.resizeTextMinSize = Config.Font_Sizes.Names_Min;

         Transform rect = skillObj.GetComponent<Transform>();
         //rect.localPosition = new Vector3( 200, 0, 0 );
         rect.localScale = new Vector3( 1, 1, 1 );

         UnityEngine.UI.Outline outline = skillObj.AddComponent<UnityEngine.UI.Outline>();
         outline.effectColor = Color.black;
         outline.effectDistance = new Vector2( 2, 2 );

         if ( Config.Display.Any_Desc )
            skillObj.AddComponent<UITooltipText>();
         skillObj.AddComponent<CanvasRenderer>();
         return skillObj;
      }

      private static string BuildHint ( LocalizedTextBind title, LocalizedTextBind desc ) {
         return $"<size={Config.Font_Sizes.Desc_Title}><b>{title.Localize()}</b></size>\n" +
                $"<size={Config.Font_Sizes.Desc_Body}>{desc.Localize()}</size>";
      }

      public static void AfterSetRecruitment_ListPerks ( HavenFacilityItemController __instance, GeoHaven ____haven ) { try {
         var recruit = ____haven?.AvailableRecruit;
         if ( Config.Display.Personal_Skill_Names ) ShowPersonalSkills( __instance, recruit );
         //ModnixApi?.Invoke( "zy.ui.dump", __instance.gameObject );
      } catch ( Exception ex ) { Error( ex ); } }

      public static void ShowPersonalSkills ( HavenFacilityItemController __instance, GeoCharacter recruit ) { try {
         var tracks = recruit?.Progression?.AbilityTracks;
         GameObject skillText = CreateNameText( __instance, "PersonalSkills" );
         if ( tracks == null ) {
            skillText.SetActive( false );
            return;
         }
         foreach ( var track in tracks ) {
            if ( track == null || track.Source != AbilityTrackSource.Personal ) continue;
            ViewElementDef mainClass = recruit.Progression.MainSpecDef.ViewElementDef;
            IEnumerable<ViewElementDef> abilities = track.AbilitiesByLevel.Where( e => e?.Ability != null ).Select( e => e.Ability.ViewElementDef );
            string list = abilities.Select( e => TitleCase( e.DisplayName1.Localize() ) ).Join();
            skillText.GetComponent<UnityEngine.UI.Text>().text = "     " + list;
            if ( Config.Display.Any_Desc ) {
               var tipText = "";
               if ( Config.Display.Main_Class_Desc ) tipText += BuildHint( mainClass.DisplayName1, mainClass.DisplayName2 ) + "\n\n";
               if ( Config.Display.Personal_Skill_Desc ) tipText += abilities.Select( e => BuildHint( e.DisplayName1, e.Description ) ).Join( null, "\n\n" );
               skillText.GetComponent<UITooltipText>().TipText = tipText.Trim();
            }
            skillText.SetActive( true );
            //skillText.GetComponent<UITooltipText>().UpdateText( hint );
            break;
         }
      } catch ( Exception ex ) { Error( ex ); } }
   }
}