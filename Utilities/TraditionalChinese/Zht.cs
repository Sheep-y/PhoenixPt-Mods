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
         lock ( Translated ) {
            if ( Translated.Contains( term ) ) return;
            Translated.Add( term );
         }

         var trans = __result?.Languages;
         if ( trans == null ) { Info( "Empty translation {0}", term ); return; }
         var idx = __instance.GetLanguageIndex( "Chinese (Simplified)", true, false );

         var result = TextMap( term );
         if ( result == null ) {
            var text = idx >= 0 && idx < trans.Length ? trans[ idx ] : null;
            if ( string.IsNullOrEmpty( text ) ) { Info( "Untranslated {0} {1}", term, __result.Languages[0] ); return; }
            result = Convert( term, text );
            if ( result.Equals( text ) ) return;
            Verbo( "Translated {0} {1} => {2}", term, text, result );
         }
         trans[ idx ] = result;
      } catch ( Exception ex ) { Error( ex ); } }

      #region Translation
      internal const int LOCALE_SYSTEM_DEFAULT = 0x0800;
      internal const int LCMAP_SIMPLIFIED_CHINESE = 0x02000000;
      internal const int LCMAP_TRADITIONAL_CHINESE = 0x04000000;

      [ DllImport( "kernel32", CharSet = CharSet.Unicode, SetLastError = true ) ]
      internal static extern int LCMapString ( int Locale, int dwMapFlags, string lpSrcStr, int cchSrc, [Out] string lpDestStr, int cchDest );

      private static string ToTraditional ( string simp ) {
         var trad = new String( ' ', simp.Length );
         _ = LCMapString( LOCALE_SYSTEM_DEFAULT, LCMAP_TRADITIONAL_CHINESE, simp, simp.Length, trad, simp.Length );
         Correct( ref trad, '么', '麽' );
         Correct( ref trad, '并', '並' );
         Correct( ref trad, '圣', '聖' );
         Correct( ref trad, '于', '於' );
         Correct( ref trad, "，", "，​" );
         Correct( ref trad, "。", "。​" );
         Correct( ref trad, "飛行器", "航空器" );
         Correct( ref trad, "次級兵種", "兼職" );
         Correct( ref trad,  "兵種", "職業" );
         return trad;
      }

      private static void Correct ( ref string txt, char from, char to ) {
         if ( txt.IndexOf( from ) < 0 ) return;
         txt = txt.Replace( from, to );
      }

      private static void Correct ( ref string txt, string from, string to ) {
         if ( txt.IndexOf( from, StringComparison.Ordinal ) < 0 ) return;
         txt = txt.Replace( from, to );
      }

      internal static string Convert ( string term, string text ) {
         var result = ToTraditional( text );
         if ( term.StartsWith( "KEY_TUTORIAL_", StringComparison.Ordinal ) ) {
            Correct( ref result, "手柄", "控制器" );
         }
         return result;
      }

      internal static string TextMap ( string term ) { switch ( term ) {
         case "Base Screen/KEY_BASE_AIRCRAFT" : return "航空器";
         case "Base Screen/KEY_BASE_CURRENT_BASE_NAME" : return "基地";
         case "Base Screen/KEY_BASE_OPERATION_REQUIREMENTS" : return "耗電";
         case "Base Screen/KEY_BASE_PERSONNEL" : return "駐員";
         case "Base Screen/KEY_BASE_VEHICLES" : return "戰車";
         case "Base Screen/KEY_GEOSCAPE_AIRCRAFTS" : return "航空器";
         case "Base Screen/KEY_GEOSCAPE_BASES" : return "基地";
         case "Character Progression/KEY_NEW_CLASS_HEADER" : return "選擇兼職";
         case "Character Progression/KEY_PROGRESSION_COST" : return "使用技能點";
         case "Controls/KEY_CONTROLS_BINDINGS_COMMON" : return "通用指令";
         case "Controls/KEY_CONTROLS_BINDINGS_GEOSCAPE" : return "世界指令";
         case "Controls/KEY_CONTROLS_BINDINGS_REVERT_TO_DEFAULT" : return "預設值";
         case "Controls/KEY_CONTROLS_BINDINGS_TACTICAL" : return "戰術指令";
         case "Diplomacy and Factions/KEY_DIPLOMACY_CONTACT" : return "接觸派系，就能建立聯繫";
         case "Equipping Screen/KEY_GEOSCAPE_DRAG_MANUFACTURE" : return "拖到此處製作";
         case "Equipping Screen/KEY_GEOSCAPE_DRAG_SCRAP" : return "拖到此處回收";
         case "Equipping Screen/KEY_GEOSCAPE_REPLENISH_ALL" : return "全部補給";
         case "Equipping Screen/KEY_NOT_PROFICIENT_WARNING" : return "不熟練";
         case "Geoscape/KEY_GEOSCAPE_CHAIN_DESTINATIONS" : return "Shift + 鼠標右鍵：設定中途站";
         case "Geoscape/KEY_GEOSCAPE_EXIT" : return "離開";
         case "Geoscape/KEY_QUICK_LOAD_NAME" : return "快速載入";
         case "Geoscape/KEY_QUICK_SAVE_NAME" : return "快速儲存";
         case "Geoscape/KEY_RADAR_USAGE" : return "區域掃描可以找出附近區域裡的可探索地點。";
         case "Geoscape/KEY_SECTION_GEOSCAPE" : return "世界";
         case "KEY_ALN_CRABMAN_NAME" : return "蟹人";
         case "KEY_ASSAULT_PROFICIENCY_DESCRIPTION" : return "熟練使用 突擊步槍 和 霰彈槍。";
         case "KEY_BASE_FACILITY_ACCESS_LIFT_NAME" : return "出入口";
         case "KEY_BASE_FACILITY_FABRICATION_PLANT_NAME" : return "工房";
         case "KEY_BASE_FACILITY_LAB_NAME" : return "研究所";
         case "KEY_BASE_FACILITY_SATELLITE_UPLINK_NAME" : return "衛星通訊器";
         case "KEY_BASE_FACILITY_VEHICLE_BAY_NAME" : return "機庫";
         case "KEY_CONTROLS_ACTION_ABILITY_CONFIRM" : return "使用技能";
         case "KEY_CONTROLS_ACTION_ABILITY_OVERWATCH" : return "進入警戒模式";
         case "KEY_CONTROLS_ACTION_ABILITY_RELOAD" : return "裝填彈藥";
         case "KEY_CONTROLS_ACTION_ASCEND_LEVEL" : return "提高所示樓層";
         case "KEY_CONTROLS_ACTION_CHANGE_OVERWATCH_CONE" : return "擴大/縮小警戒區域";
         case "KEY_CONTROLS_ACTION_CYCLE_WEAPONS" : return "下一項裝備";
         case "KEY_CONTROLS_ACTION_DESCEND_LEVEL" : return "降低所示樓層";
         case "KEY_CONTROLS_ACTION_DISCRETE_ZOOM_IN" : return "拉近";
         case "KEY_CONTROLS_ACTION_DISCRETE_ZOOM_OUT" : return "拉遠";
         case "KEY_CONTROLS_ACTION_FREE_AIM" : return "進入自由瞄準模式";
         case "KEY_CONTROLS_ACTION_GEOSCAPE_PAUSE" : return "恢復/暫停時間";
         case "KEY_CONTROLS_ACTION_GEOSCAPE_SHOW_LOG" : return "顯示事件紀錄";
         case "KEY_CONTROLS_ACTION_GEOSCAPE_TIME_DECREMENT" : return "減慢時間流逝";
         case "KEY_CONTROLS_ACTION_GEOSCAPE_TIME_INCREMENT" : return "加快時間流逝";
         case "KEY_CONTROLS_ACTION_HIDE_UI" : return "隱藏用戶介面";
         case "KEY_CONTROLS_ACTION_NEXT_ABILITY" : return "下一項技能";
         case "KEY_CONTROLS_ACTION_NEXT_CHARACTER" : return "下一角色/載具/目標";
         case "KEY_CONTROLS_ACTION_OPEN_OPTIONS_MENU" : return "打開游戲選單";
         case "KEY_CONTROLS_ACTION_PREV_ABILITY" : return "上一項技能";
         case "KEY_CONTROLS_ACTION_PREV_CHARACTER" : return "上一角色/載具/目標";
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
         case "KEY_CONTROLS_GEOSCAPE_LTHUMB_PRESS" : return "航空器資訊";
         case "KEY_CONTROLS_GEOSCAPE_RB" : return "上一輛航天器";
         case "KEY_CONTROLS_GEOSCAPE_RT" : return "拉近";
         case "KEY_CONTROLS_GEOSCAPE_RTHUMB" : return "轉動地球";
         case "KEY_CONTROLS_GEOSCAPE_RTHUMB_PRESS" : return "置中到所選目標";
         case "KEY_CONTROLS_TACTICAL_BACK" : return "打開游戲選單";
         case "KEY_CONTROLS_TACTICAL_DPAD_DOWN" : return "降低所示樓層";
         case "KEY_CONTROLS_TACTICAL_DPAD_UP" : return "提高所示樓層";
         case "KEY_CONTROLS_TACTICAL_LT" : return "拉遠";
         case "KEY_CONTROLS_TACTICAL_LTHUMB_PRESS" : return "角色資訊";
         case "KEY_CONTROLS_TACTICAL_PC_MOUSE_LK_HOLD_DESCRIPTION" : return "改變視角";
         case "KEY_CONTROLS_TACTICAL_PC_MOUSE_LK_HOLD_NAME" : return "滑鼠左鍵拖動";
         case "KEY_CONTROLS_TACTICAL_PC_MOUSE_LK_NAME" : return "滑鼠左鍵";
         case "KEY_CONTROLS_TACTICAL_PC_MOUSE_RK_NAME" : return "滑鼠右鍵";
         case "KEY_CONTROLS_TACTICAL_PC_MOUSE_WHEEL_DESCRIPTION" : return "拉遠";
         case "KEY_CONTROLS_TACTICAL_PC_MOUSE_WHEEL_NAME" : return "滑鼠滾輪";
         case "KEY_CONTROLS_TACTICAL_PC_MOUSE_WHEEL_SCROLL_DESCRIPTION" : return "提高/降低所示樓層";
         case "KEY_CONTROLS_TACTICAL_PC_MOUSE_WHEEL_SCROLL_NAME" : return "滾動滑鼠滾輪";
         case "KEY_CONTROLS_TACTICAL_X" : return "下一項裝備";
         case "KEY_CONTROLS_TACTICAL_Y" : return "進入警戒模式";
         case "KEY_DIFFICULTY_VERY_DIFFICULT_DESCRIPTION" : return "戰術和策略的終極考驗";
         case "KEY_DLC_BLOOD_AND_TITANIUM" : return "血與鈦";
         case "KEY_DLC_FESTERING_SKIES" : return "失落瘡天";
         case "KEY_GEOSCAPE_AVAILABLE" : return "待機";
         case "KEY_GEOSCAPE_LOADING_TIP_3" : return "當派系關係提升到 25 時，他們會支持你，並提供其所有避難所的位置。";
         case "KEY_GEOSCAPE_LOADING_TIP_4" : return "當派系關系提升到 50 時，他們願跟你同心合力，並會向你分享技術。";
         case "KEY_GEOSCAPE_LOADING_TIP_8" : return "你可以竊取其他派系的研究來快速推進科技水平。\n派員到有研究中心的避難所，就可以進行竊取研究任務。";
         case "KEY_MISSION_OBJECTIVE_ENEMIES_NAME" : return "消滅所有敵人";
         case "KEY_NEW_GAME" : return "開新遊戲";
         case "KEY_OBJECTIVE_TUTORIAL1_MOVE" : return "移動到指定地點";
         case "KEY_OPTIONS_CHOOSE_DISPLAY" : return "螢幕 {0}";
         case "KEY_OPTIONS_FULL_SCREEN_EXCLUSIVE" : return "全螢幕";
         case "KEY_OPTIONS_QUIT_CONFIRM" : return "未儲存的游戲進度將會丟失。確認離開？";
         case "KEY_SELECT_TUTORIAL" : return "新手教學";
         case "KEY_TUTORIAL_TACTICAL1_AP_DESCRIPTION" : return "每名士兵每回合有 4 點行動點 (AP) 用以射擊或移動。每使用一點 AP 可移動若干方格，\n具體格數取決於地圖和士兵的速度。\n\n藍色輪廓標記出士兵能夠移動後射擊的範圍。\n\n如果所選位置能射擊到敵人，會顯示出一條藍色的射擊線。\n\n移動至高亮方格。";
         case "KEY_TUTORIAL_TACTICAL1_SELECTION_DESCRIPTION" : return "選擇你的士兵。用 :LMB: 點選士兵，\n或用控制器移動選擇標記至士兵身上并按 :A:";
         case "KEY_TUTORIAL_TACTICAL1_SHOOTING_DESCRIPTION" : return "血條表示當前生命值 (HP)。右邊的護甲值表示每次命中減免的傷害。\n\n瞄準敵人時，預測的傷害範圍將顯示在血條上。預測值顏色越白，可能性就越大。\n\n選擇“發射武器”。\n\n用 :LMB: 點選“發射武器”圖標，\n或按手柄 :RT: 向敵人開火。";
         case "KEY_TUTORIAL_TACTICAL1_SPOTTING_DESCRIPTION" : return "行動欄上方會以圖標顯示所有已經發現的敵人。\n\n紅色圖標表示目前所選士兵能直接目視的敵人。";
         case "Missions/KEY_MISSION_WP" : return "意志";
         case "Opening Screen/KEY_CONTINUE_GAME" : return "繼續遊戲";
         case "Opening Screen/KEY_CREDITS" : return "制作人員";
         case "Opening Screen/KEY_DLC_ACTIVATE" : return "啓用 DLC";
         case "Opening Screen/KEY_LOADING" : return "載入中…";
         case "Options Screen/KEY_AUDIO" : return "聲音";
         case "Options Screen/KEY_OPTIONS_BLOOM" : return "泛光";
         case "Options Screen/KEY_OPTIONS_CHOOSE_MONITOR" : return "使用螢幕";
         case "Options Screen/KEY_OPTIONS_CONTROLLER_SCHEME" : return "控制器配置";
         case "Options Screen/KEY_OPTIONS_EXIT_NAME" : return "回到主選單";
         case "Options Screen/KEY_OPTIONS_GAMEPLAY" : return "其他";
         case "Options Screen/KEY_OPTIONS_GRAPHICS" : return "畫質";
         case "Options Screen/KEY_OPTIONS_INTERFACE" : return "介面";
         case "Options Screen/KEY_OPTIONS_INTERFACE_VOLUME" : return "介面音量";
         case "Options Screen/KEY_OPTIONS_KEYBOARD_BINDINGS" : return "按鍵配置";
         case "Options Screen/KEY_OPTIONS_LOAD_GAME_NAME" : return "載入遊戲";
         case "Options Screen/KEY_OPTIONS_LOAD_NAME" : return "載入";
         case "Options Screen/KEY_OPTIONS_QUIT_NAME" : return "離開遊戲";
         case "Options Screen/KEY_OPTIONS_RESOLUTION" : return "解像度";
         case "Options Screen/KEY_OPTIONS_SAVE_GAME_DESCRIPTION" : return "儲存遊戲";
         case "Options Screen/KEY_OPTIONS_SCREEN_MODE" : return "顯示模式";
         case "Options Screen/KEY_OPTIONS_SCREEN_SPACE_REFLECTION" : return "空間反射";
         case "Options Screen/KEY_OPTIONS_SHOW_SCHEME" : return "顯示配置";
         case "Options Screen/KEY_OPTIONS_SOUND_ENABLED" : return "啓用聲音";
         case "Options Screen/KEY_OPTIONS_VIDEO" : return "畫面";
         case "Options Screen/KEY_SELECT_DIFFICULTY" : return "難易度";
         case "PH_Localization/KEY_GEOSCAPE_NO_PRODUCTION" : return "沒有可以生產的裝備";
         case "Phoenixpedia/KEY_PHOENIXPEDIA_GUIDE_SEARCH" : return "搜索";
         case "Research Screen/KEY_RESEARCH_NOT_AVAILABLE" : return "沒有可以進行的研究";
         case "Roster Screen/KEY_DEPLOY_SQUAD" : return "部署";
         case "Roster Screen/KEY_GEOROSTER_MOUNTS" : return "附加";
         case "Tactical/KEY_ACCURACY_COLON" : return "精準：";
         case "KEY_ITEM_STAT_STEALTH" : return "隱匿";
         case "KEY_PROGRESSION_ACCURACY" : return "精準";
         case "Tactical/KEY_ACTION_POINTS_CLEAN" : return "AP";
         case "Tactical/KEY_HIT_POINTS_CLEAN" : return "HP";
         case "Tactical/KEY_STEALTH_COLON" : return "隱匿：";
         case "Tactical/KEY_WILL_POINTS_CLEAN" : return "意志";
         case "KEY_PHOENIXPEDIA_GUIDE_ATTRIBUTES_NAME" : return "角色能力值";
         case "KEY_PHOENIXPEDIA_GUIDE_DAMAGE_SYSTEM_NAME" : return "普通傷害";
         case "KEY_PHOENIXPEDIA_GUIDE_SHOCK_NAME" : return "衝擊傷害";
         case "KEY_PHOENIXPEDIA_GUIDE_SHRED_NAME" : return "破甲傷害";
         case "KEY_PHOENIXPEDIA_GUIDE_PSYCHIC_NAME" : return "精神傷害";
         case "KEY_ITEM_STAT_AMMO_CAPACITY" : return "裝彈數";
         case "KEY_SCARAB_NAME" : return "鳳凰甲蟲戰車";
         case "KEY_PX_AIRSHIP_DESCRIPTION" : return "鳳凰航空器";
         case "KEY_MANUFACTURE_TAB_EQUIPMENT_TT" : return "製作裝備";
         case "KEY_MEDKIT_DESCRIPTION" : return "恢復 {GeneralHealAmount} HP，并移除“流血”和“中毒”效果";

         case "KEY_PX_GRENADE_DESCRIPTION" : return "鳳凰手榴彈";
         case "KEY_PX_GRENADE_NAME" : return "奧丁手榴彈";
         case "KEY_PX_HEAVY_BODY_NAME2" : return "哥林 B 型軀體護甲";
         case "KEY_PX_HEAVY_HELMET_NAME2" : return "哥林 B 型頭盔";
         case "KEY_PX_HEAVY_LEGS_NAME2" : return "哥林 B 型腿部護甲";
         case "KEY_PX_SNIPER_BODY_NAME2" : return "幽靈軀體護甲";
         case "KEY_PX_SNIPER_HELMET_NAME2" : return "幽靈頭盔";
         case "KEY_PX_SNIPER_LEGS_NAME2" : return "幽靈腿部護甲";

         case "KEY_INTROVOICE1_01" : return "今年可能是有史以來最熱的一年";
         case "KEY_INTROVOICE1_02" : return "科學家觀察到南極的永凍土發生劇烈變化";
         case "KEY_INTROVOICE2_01" : return "“潘多拉病毒”是一種巨型病毒，它的基因組大於所有已知病毒";
         case "KEY_INTROVOICE3_01" : return "這些螃蟹變得具攻擊性，也不怕比自己巨大的捕食者";
         case "KEY_INTROVOICE4_01" : return "一種新型異常天氣席捲世界各地的多處海岸";
         case "KEY_INTROVOICE4_02" : return "美國太空總署正在研究這些迷霧";
         case "KEY_INTROVOICE5_01" : return "我們發現裡面含有大量有機化合物";
         case "KEY_INTROVOICE5_02" : return "但這些樣本無法匹配任何已知的 DNA";
         case "KEY_INTROVOICE6_01" : return "我們都看見了！那些生物從海里衝上那個鑽油平臺！";
         case "KEY_INTROVOICE7_01" : return "總統宣布全國進入緊急狀態";
         case "KEY_INTROVOICE8_01" : return "很明顯，我們正在面對一種生物武器";
         case "KEY_INTROVOICE8_02" : return "從今天開始，我們進入戰爭！";
         case "KEY_INTROVOICE9_01" : return "大家都被迷惑了！我親眼看見他們走向大海！";
         case "KEY_INTROVOICE9_02" : return "成千上萬的人啊！不計其數！";
         case "KEY_INTROVOICE10_01": return "迷霧已散，但城市已經死亡。道路也被毀壞。";
         case "KEY_INTROVOICE10_02": return "你必須加入避難所。別試著獨自活下去。";

         case "KEY_SYMESTUTORIAL1_01" : return "我是 倫道夫·賽姆斯。鳳凰計劃的最後一任領袖。";
         case "KEY_SYMESTUTORIAL1_02" : return "當你聽到這段錄音，我很可能已經過身。";
         case "KEY_SYMESTUTORIAL1_03" : return "好消息是，一輛甲蟲戰車會來接你。";
         case "KEY_SYMESTUTORIAL1_04" : return "車載的人工智能會帶你會合鳳凰先鋒。";
         case "KEY_SYMESTUTORIAL1_05" : return "快馬加鞭、一路順風。";

         case "KEY_PP_INTRO1_01": return "鳳凰計劃創立於 1945 年 10 月 24 日。";
         case "KEY_PP_INTRO1_02": return "終結戰爭的大戰，剛完了第二次。";
         case "KEY_PP_INTRO1_04": return "有些人明白我們不能再受國家、帝國的視角所限。";
         case "KEY_PP_INTRO2_01": return "曾幾何時，鳳凰計劃成功駕馭了那個時代的政治衝突。";
         case "KEY_PP_INTRO2_02": return "那是我們的黃金時代。鳳凰計劃的特工遊遍世界尋找蜘絲馬跡。";
         case "KEY_PP_INTRO2_03": return "我們在二十多個國家設立基地。連天空都攔不住我們。";
         case "KEY_PP_INTRO3_01": return "然而，就在遙遠的月球背面，我們的命運逆轉了。";
         case "KEY_PP_INTRO3_02": return "“鳳凰２號”任務失敗，聯合國中的敵人乘虛而入。";
         case "KEY_PP_INTRO3_03": return "資源被奪，人員四散。";
         case "KEY_PP_INTRO3_04": return "我們變成了一個秘密…一段回憶。";
         case "KEY_PP_INTRO4_01": return "當潘多拉病毒蘇醒時，我們本應是第一道防線。";
         case "KEY_PP_INTRO4_02": return "當海上出現巨大迷霧，當人們開始消失，";
         case "KEY_PP_INTRO4_03": return "我們本應能發現真相。";
         case "KEY_PP_INTRO4_04": return "而當那些人回歸，但不再是人，不再友善時，";
         case "KEY_PP_INTRO4_05": return "我們也本應該準備好迎戰。";
         case "KEY_PP_INTRO5_01": return "但我們都沒做到。";
         case "KEY_PP_INTRO6_01": return "生態系統開始變化，起初暗藏不露，隨後愈演愈烈。";
         case "KEY_PP_INTRO6_02": return "三個派系從亂世崛起：新耶利哥，堅持恢復秩序與純正；";
         case "KEY_PP_INTRO6_03": return "平衡議會，希望建立一個無階級之分的世界；";
         case "KEY_PP_INTRO6_04": return "以及安努門徒教會，致力適應地球變化的融合宗教。";
         case "KEY_PP_INTRO7_01": return "他們一邊對抗世界，一邊又相互為敵，";
         case "KEY_PP_INTRO7_02": return "這些派系無法開拓前路。";
         case "KEY_PP_INTRO7_03": return "現在，迷霧即將回歸大地，敵軍正從海洋出兵。";
         case "KEY_PP_INTRO7_04": return "失去鳳凰計劃，人類就會滅亡。";
         case "KEY_PP_INTRO8_01": return "是時候浴火重生了。";
         default: return null;
      } }
      #endregion
   }
}
