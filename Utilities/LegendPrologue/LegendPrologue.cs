using System;
using PhoenixPoint.Common.Levels.Params;
using PhoenixPoint.Home.View.ViewControllers;
using PhoenixPoint.Home.View.ViewModules;
using PhoenixPoint.Home.View.ViewStates;

namespace Sheepy.PhoenixPt.LegendPrologue {

   public class Mod : ZyMod {
      public static void Init () => new Mod().MainMod();

      public void MainMod ( Func< string, object, object > api = null ) => HomeMod( api );

      private IPatch TickboxPatch, BackPatch, FlagPatch;

      public void HomeMod ( Func< string, object, object > api ) {
         SetApi( api );
         TickboxPatch = TryPatch( typeof( UIModuleGameSettings ), "MainOptions_OnElementSelected", postfix: nameof( AfterOptionSelected_ShowTickbox ) );
         if ( TickboxPatch != null ) {
            BackPatch = TryPatch( typeof( UIStateNewGeoscapeGameSettings ), "OnSettingsBackClicked", nameof( AfterBack_ClearFlag ) );
            FlagPatch = TryPatch( typeof( UIStateNewGeoscapeGameSettings ), "CreateSceneBinding", nameof( BeforeSceneBind_CheckFlag ) );
            if ( FlagPatch == null ) Unpatch( ref TickboxPatch );
         }
      }

      public void UnloadMod () { Unpatch( ref TickboxPatch ); Unpatch( ref BackPatch ); Unpatch( ref FlagPatch ); }

      private static GameOptionViewController TutorialBox;

      private static void AfterOptionSelected_ShowTickbox ( UIModuleGameSettings __instance ) { try {
         TutorialBox = __instance?.SecondaryOptions?.Elements?[ 0 ];
         TutorialBox.gameObject.SetActive( true );
      } catch ( Exception ex ) { Api( "log e", ex ); } }

      private static void AfterBack_ClearFlag () { TutorialBox = null; }

      private static void BeforeSceneBind_CheckFlag ( GeoscapeGameParams gameParams ) { try {
         if ( TutorialBox?.IsSelected == true ) gameParams.TutorialEnabled = true;
         TutorialBox = null;
      } catch ( Exception ex ) { Api( "log e", ex ); } }
   }
}