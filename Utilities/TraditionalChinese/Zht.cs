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
         if ( __result == null || string.IsNullOrEmpty( term ) ) return;
         lock ( Translated ) {
            if ( Translated.Contains( term ) ) return;
            Translated.Add( term );
         }
         int idx = __instance.GetLanguageIndex( "Chinese (Simplified)", true, false );
         if ( idx < 0 ) return;
         var text = __result.Languages[ idx ];
         if ( string.IsNullOrEmpty( text ) ) return;
         var result = TextMap( term ) ?? ToTraditional( text );
         __result.Languages[ idx ] = result;
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

      public static string TextMap ( string term ) {
         switch ( term ) {
            case "" : return "";
            default: return null;
         }
      }
      #endregion
   }
}
