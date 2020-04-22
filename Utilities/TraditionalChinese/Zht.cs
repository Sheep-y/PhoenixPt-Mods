using Harmony;
using I2.Loc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Sheepy.PhoenixPt.Zht {
   public class Mod : ZyMod {
      internal static volatile Mod Instance;

      public static void Init () => new Mod().SplashMod();

      public void SplashMod ( Func<string, object, object> api = null ) {
         Instance = this;
         SetApi( api );
         Patch( typeof( LocalizationManager ), "set_CurrentLanguage", postfix: nameof( AfterSetCurrentLanguage_Patch ) );
         AfterSetCurrentLanguage_Patch();
      }

      internal static IPatch LangPatch;

      private static void AfterSetCurrentLanguage_Patch () { try {
         string lang = LocalizationManager.CurrentLanguageCode;
         Info( "Game language is {0}", lang );
         if ( "zh-CN".Equals( lang ) ) DoLangPatch();
         else UndoLangPatch();
      } catch ( Exception ex ) { Error( ex ); } }

      private static void DoLangPatch () { lock ( _Lock ) {
         if ( LangPatch != null ) return;
         LangPatch = Instance.Patch( typeof( LanguageSourceData ), "GetTermData", postfix: nameof( Translate ) );
      } }

      private static void UndoLangPatch () { lock ( _Lock ) {
         if ( LangPatch == null ) return;
         LangPatch.Unpatch();
         LangPatch = null;
      } }

      private static readonly HashSet<string> Translated = new HashSet<string>();

      private static void Translate ( LanguageSourceData __instance, string term, TermData __result ) { try {
         if ( string.IsNullOrEmpty( term ) ) return;
         var trans = __result?.Languages;
         if ( trans == null ) { Info( "Empty translation {0}", term ); return; }

         lock ( Translated ) {
            if ( Translated.Contains( term ) ) return;
            Translated.Add( term );
         }
         var idx = __instance.GetLanguageIndex( "Chinese (Simplified)", true, false );
         var text = idx >= 0 && idx < trans.Length ? trans[ idx ] : null;
         if ( string.IsNullOrEmpty( text ) ) { Info( "Untranslated {0} {1}", term, __result.Languages[0] ); return; }

         var result = TextMap( term ) ?? ToTraditional( text );
         trans[ idx ] = result;
         Verbo( "Translated {0} {1} => {2}", term, text, result );
      } catch ( Exception ex ) { Error( ex ); } }

      #region Translation
      internal const int LOCALE_SYSTEM_DEFAULT = 0x0800;
      internal const int LCMAP_SIMPLIFIED_CHINESE = 0x02000000;
      internal const int LCMAP_TRADITIONAL_CHINESE = 0x04000000;

      [ DllImport( "kernel32", CharSet = CharSet.Unicode, SetLastError = true ) ]
      internal static extern int LCMapString ( int Locale, int dwMapFlags, string lpSrcStr, int cchSrc, [Out] string lpDestStr, int cchDest );

      public static string ToTraditional ( string simp ) {
         var trad = new String( ' ', simp.Length );
         _ = LCMapString( LOCALE_SYSTEM_DEFAULT, LCMAP_TRADITIONAL_CHINESE, simp, simp.Length, trad, simp.Length );
         return trad;
      }

      public static string TextMap ( string term ) { switch ( term ) {
         case "Character Progression/KEY_NEW_CLASS_HEADER" : return "選擇副職業";
         case "Character Progression/KEY_PROGRESSION_COST" : return "使用技能點";
         case "Controls/KEY_CONTROLS_BINDINGS_COMMON" : return "通用指令";
         case "Controls/KEY_CONTROLS_BINDINGS_GEOSCAPE" : return "世界指令";
         case "Controls/KEY_CONTROLS_BINDINGS_REVERT_TO_DEFAULT" : return "預設值";
         case "Controls/KEY_CONTROLS_BINDINGS_TACTICAL" : return "戰術指令";
         case "Equipping Screen/KEY_GEOSCAPE_DRAG_MANUFACTURE" : return "拖到此處製作";
         case "Equipping Screen/KEY_GEOSCAPE_DRAG_SCRAP" : return "拖到此處回收";
         case "Geoscape/KEY_GEOSCAPE_EXIT" : return "離開";
         case "Geoscape/KEY_QUICK_LOAD_NAME" : return "快速載入";
         case "Geoscape/KEY_QUICK_SAVE_NAME" : return "快速儲存";
         case "KEY_CONTROLS_ACTION_ABILITY_CONFIRM" : return "使用技能";
         case "KEY_CONTROLS_ACTION_ABILITY_OVERWATCH" : return "進入警戒模式";
         case "KEY_CONTROLS_ACTION_ABILITY_RELOAD" : return "裝填彈藥";
         case "KEY_CONTROLS_ACTION_ASCEND_LEVEL" : return "提高樓層";
         case "KEY_CONTROLS_ACTION_CHANGE_OVERWATCH_CONE" : return "擴大/縮小警戒區域";
         case "KEY_CONTROLS_ACTION_CYCLE_WEAPONS" : return "下一項裝備";
         case "KEY_CONTROLS_ACTION_DESCEND_LEVEL" : return "降低樓層";
         case "KEY_CONTROLS_ACTION_DISCRETE_ZOOM_IN" : return "拉近";
         case "KEY_CONTROLS_ACTION_DISCRETE_ZOOM_OUT" : return "拉遠";
         case "KEY_CONTROLS_ACTION_FREE_AIM" : return "進入自由瞄準模式";
         case "KEY_CONTROLS_ACTION_GEOSCAPE_PAUSE" : return "恢復/暫停時間";
         case "KEY_CONTROLS_ACTION_GEOSCAPE_SHOW_LOG" : return "顯示事件紀錄";
         case "KEY_CONTROLS_ACTION_GEOSCAPE_TIME_DECREMENT" : return "減慢時間流逝";
         case "KEY_CONTROLS_ACTION_GEOSCAPE_TIME_INCREMENT" : return "加快時間流逝";
         case "KEY_CONTROLS_ACTION_NEXT_ABILITY" : return "下一項技能";
         case "KEY_CONTROLS_ACTION_NEXT_CHARACTER" : return "下一士兵/載具/目標";
         case "KEY_CONTROLS_ACTION_OPEN_OPTIONS_MENU" : return "打開游戲選單";
         case "KEY_CONTROLS_ACTION_PREV_ABILITY" : return "上一項技能";
         case "KEY_CONTROLS_ACTION_PREV_CHARACTER" : return "上一士兵/載具/目標";
         case "KEY_CONTROLS_ACTION_ROTATE_CAMERA_LEFT" : return "逆時針旋轉視角";
         case "KEY_CONTROLS_ACTION_ROTATE_CAMERA_RIGHT" : return "順時針旋轉視角";
         case "KEY_CONTROLS_ACTION_SELECT_ITEM_1" : return "選擇裝備１";
         case "KEY_CONTROLS_ACTION_SELECT_ITEM_2" : return "選擇裝備２";
         case "KEY_CONTROLS_ACTION_SELECT_ITEM_3" : return "選擇裝備３";
         case "KEY_CONTROLS_ACTION_SELECT_ITEM_4" : return "選擇裝備４";
         case "KEY_CONTROLS_ACTION_SHOW_ITEM_LABELS" : return "顯示物品名稱";
         case "KEY_CONTROLS_GEOSCAPE_BACK" : return "打開游戲選單";
         case "KEY_CONTROLS_GEOSCAPE_DPAD_DOWN" : return "減慢時間流逝";
         case "KEY_CONTROLS_GEOSCAPE_DPAD_UP" : return "加快時間流逝";
         case "KEY_CONTROLS_GEOSCAPE_LB" : return "下一輛航天器";
         case "KEY_CONTROLS_GEOSCAPE_LT" : return "拉遠";
         case "KEY_CONTROLS_GEOSCAPE_LTHUMB" : return "移動光標";
         case "KEY_CONTROLS_GEOSCAPE_RB" : return "上一輛航天器";
         case "KEY_CONTROLS_GEOSCAPE_RT" : return "拉近";
         case "KEY_CONTROLS_GEOSCAPE_RTHUMB" : return "轉動地球";
         case "KEY_CONTROLS_GEOSCAPE_RTHUMB_PRESS" : return "置中到所選目標";
         case "KEY_CONTROLS_TACTICAL_PC_MOUSE_LK_HOLD_DESCRIPTION" : return "改變視角";
         case "KEY_CONTROLS_TACTICAL_PC_MOUSE_LK_HOLD_NAME" : return "滑鼠左鍵拖動";
         case "KEY_CONTROLS_TACTICAL_PC_MOUSE_LK_NAME" : return "滑鼠左鍵";
         case "KEY_CONTROLS_TACTICAL_PC_MOUSE_RK_NAME" : return "滑鼠右鍵";
         case "KEY_CONTROLS_TACTICAL_PC_MOUSE_WHEEL_DESCRIPTION" : return "拉遠";
         case "KEY_CONTROLS_TACTICAL_PC_MOUSE_WHEEL_NAME" : return "滑鼠滾輪";
         case "KEY_CONTROLS_TACTICAL_PC_MOUSE_WHEEL_SCROLL_DESCRIPTION" : return "提高/降低樓層";
         case "KEY_CONTROLS_TACTICAL_PC_MOUSE_WHEEL_SCROLL_NAME" : return "滾動滑鼠滾輪";
         case "KEY_NEW_GAME" : return "開新遊戲";
         case "KEY_OPTIONS_CHOOSE_DISPLAY" : return "螢幕 {0}";
         case "KEY_OPTIONS_FULL_SCREEN_EXCLUSIVE" : return "全螢幕";
         case "Opening Screen/KEY_CONTINUE_GAME" : return "繼續遊戲";
         case "Opening Screen/KEY_CREDITS" : return "制作人員";
         case "Opening Screen/KEY_DLC_ACTIVATE" : return "啓用 DLC";
         case "Opening Screen/KEY_LOADING" : return "載入中…";
         case "Opening Screen/KEY_OPTIONS_BLOOM" : return "泛光";
         case "Opening Screen/KEY_OPTIONS_SCREEN_SPACE_REFLECTION" : return "空間反射";
         case "Options Screen/KEY_AUDIO" : return "聲音";
         case "Options Screen/KEY_OPTIONS_CHOOSE_MONITOR" : return "使用螢幕";
         case "Options Screen/KEY_OPTIONS_CONTROLLER_SCHEME" : return "控制器配置";
         case "Options Screen/KEY_OPTIONS_GAMEPLAY" : return "其他";
         case "Options Screen/KEY_OPTIONS_GRAPHICS" : return "畫質";
         case "Options Screen/KEY_OPTIONS_INTERFACE" : return "介面";
         case "Options Screen/KEY_OPTIONS_INTERFACE_VOLUME" : return "介面音量";
         case "Options Screen/KEY_OPTIONS_KEYBOARD_BINDINGS" : return "按鍵配置";
         case "Options Screen/KEY_OPTIONS_LOAD_GAME_NAME" : return "載入遊戲";
         case "Options Screen/KEY_OPTIONS_LOAD_NAME" : return "載入";
         case "Options Screen/KEY_OPTIONS_RESOLUTION" : return "解像度";
         case "Options Screen/KEY_OPTIONS_SCREEN_MODE" : return "顯示模式";
         case "Options Screen/KEY_OPTIONS_SHOW_SCHEME" : return "顯示配置";
         case "Options Screen/KEY_OPTIONS_SOUND_ENABLED" : return "啓用聲音";
         case "Options Screen/KEY_OPTIONS_VIDEO" : return "畫面";
         case "Options Screen/KEY_SELECT_DIFFICULTY" : return "難易度";
         case "Roster Screen/KEY_GEOROSTER_MOUNTS" : return "附加";
         default: return null;
      } }
      #endregion
   }
}
