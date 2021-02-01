using Base.Core;
using Base.Defs;
using Base.Utils.GameConsole;
using Microsoft.ClearScript;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.Reflection.BindingFlags;

namespace Sheepy.PhoenixPt.ScriptingLibrary {
   public static class ConsoleHelper {
      public static void assert ( bool assertion, params object[] args ) { if ( ! assertion ) error( "Assertion failed:", args ); }

      public static void clear () =>
         typeof( GameConsoleWindow ).GetMethod( "ClearScreen", NonPublic | Public | Static | Instance )?.Invoke( GameConsoleWindow.Create(), Array.Empty<object>() );

      public static void dir ( object obj ) => ScriptingExt.WriteToConsole( obj );

      public static void debug ( object msg, params object[] args ) => ZyMod.Verbo( msg, args );
      public static void error ( object msg, params object[] args ) => ZyMod.Error( msg, args );
      public static void info ( object msg, params object[] args ) => ZyMod.Info( msg, args );
      public static void log ( object msg, params object[] args ) => ZyMod.Info( msg, args );
      public static void trace ( object msg, params object[] args ) => ZyMod.Trace( msg, args );
      public static void warn ( object msg, params object[] args ) => ZyMod.Warn( msg, args );
   }

   public static class LogHelper {
      public static void Error ( object msg, params object[] args ) => error( msg, args );
      public static void Warn ( object msg, params object[] args ) => warn( msg, args );
      public static void Warning ( object msg, params object[] args ) => warn( msg, args );
      public static void Info ( object msg, params object[] args ) => info( msg, args );
      public static void Verbo ( object msg, params object[] args ) => verbo( msg, args );
      public static void Verbose ( object msg, params object[] args ) => verbo( msg, args );
      public static void error ( object msg, params object[] args ) => ZyMod.Error( msg, args );
      public static void warn ( object msg, params object[] args ) => ZyMod.Warn( msg, args );
      public static void warning ( object msg, params object[] args ) => warn( msg, args );
      public static void info ( object msg, params object[] args ) => ZyMod.Info( msg, args );
      public static void verbo ( object msg, params object[] args ) => ZyMod.Verbo( msg, args );
      public static void verbose ( object msg, params object[] args ) => ZyMod.Verbo( msg, args );
   }

   public static class ApiHelper {
      public static object call ( string action, object param = null ) => ZyMod.Api( action, param );
      public static object Call ( string action, object param = null ) => call( action, param );
      public static T Call < T > ( string action, object param = null ) => call<T>( action, param );
      public static T call < T > ( string action, object param = null ) {
         var result = Call( action, param );
         if ( result is Exception ex ) throw ex;
         return (T) result;
      }
   }

   public static class PatchHelper {
      public static void Run ( ScriptObject callback ) {
         Action action = () => callback.Invoke( false );
         ZyMod.Info( action.Method );
      }
   }

   public static class RepoHelper {
      private static DefRepository _Repo;
      public static DefRepository repo => _Repo ?? ( _Repo = GameUtl.GameComponent< DefRepository >() );
      public static DefRepository Repo => repo;

      public static BaseDef get ( string key ) => Get< BaseDef >( key );
      public static BaseDef Get ( string key ) => get( key );

      public static T get< T > ( string key ) where T : BaseDef => (T) DataCache.API_PP_Def( null, key );
      public static T Get< T > ( string key ) where T : BaseDef => get< T >( key );

      public static IEnumerable< BaseDef > getAll ( object nameOrType ) => DataCache.API_PP_Defs( null, nameOrType );
      public static IEnumerable< BaseDef > GetAll ( object nameOrType ) => getAll( nameOrType );

      public static IEnumerable< T > getAll< T > ( object nameOrType = null ) where T : BaseDef => DataCache.API_PP_Defs( null, nameOrType ).OfType<T>();
      public static IEnumerable< T > GetAll< T > ( object nameOrType = null ) where T : BaseDef => getAll< T >( nameOrType );
   }

   public static class DamageHelpers {
      private static SharedDamageKeywordsDataDef DmgType;

      public static DamageKeywordDef Parse ( string type = "normal" ) => parse( type );
      public static DamageKeywordDef parse ( string type = "normal" ) {
         if ( DmgType == null ) DmgType = SharedData.GetSharedDataFromGame().SharedDamageKeywords;
         switch ( type ) {
            case "acid" : return DmgType.AcidKeyword;
            case "blast" : return DmgType.BlastKeyword;
            case "bleed" : return DmgType.BleedingKeyword;
            case "fire" : return DmgType.BurningKeyword;
            case "goo" : return DmgType.GooKeyword;
            case "mist" : return DmgType.MistKeyword;
            case "paralyse" : case "paralyze" : return DmgType.ParalysingKeyword;
            case "pierce" : return DmgType.PiercingKeyword;
            case "poison" : return DmgType.PoisonousKeyword;
            case "psychic" : return DmgType.PsychicKeyword;
            case "shock" : return DmgType.ShockKeyword;
            case "shred" : return DmgType.ShreddingKeyword;
            case "sonic" : return DmgType.SonicKeyword;
            case "syphon" : case "vampiric" : return DmgType.SyphonKeyword;
            case "viral" : return DmgType.ViralKeyword;
            case "virophage" : return (DamageKeywordDef) RepoHelper.Repo.GetDef( "c968f22f-392d-1964-68cd-edd14655082d" );
            case "normal" : case "" : case null :
               return DmgType.DamageKeyword;
         }
         throw new ArgumentException( "Unknown damage type: " + type );
      }

      private static float Modify ( this WeaponDef weapon, DamageKeywordDef damageType, Func<float,float> handler ) { lock ( weapon ) {
         if ( weapon == null ) throw new ArgumentNullException( nameof( weapon ) );
         if ( damageType == null ) throw new ArgumentNullException( nameof( damageType ) );
         var list = weapon.DamagePayload.DamageKeywords;
         var pair = list.Find( e => e.DamageKeywordDef == damageType );
         var orig = pair?.Value ?? 0;
         if ( handler == null ) return orig;
         var value = handler( orig );
         if ( value == orig ) return orig;
         object[] logArg = new object[]{ weapon.name, damageType.name, orig, value };
         if ( value > 0 ) {
            if ( pair == null ) {
               ZyMod.Verbo( "Adding {1} {3} to {0}", logArg );
               list.Add( pair = new DamageKeywordPair { DamageKeywordDef = damageType, Value = value } );
            } else {
               ZyMod.Verbo( "Update {0} {1} {2} to {3}", logArg );
               pair.Value = value;
            }
         } else if ( pair != null ) {
            ZyMod.Verbo( "Removing {1} {2} from {0}", logArg );
            list.Remove( pair );
         }
         return orig;
      } }

      public static float getDamage ( this WeaponDef weapon, string type = "normal" ) => GetDamage( weapon, Parse( type ) );
      public static float GetDamage ( this WeaponDef weapon, string type = "normal" ) => getDamage( weapon, type );

      public static float getDamage ( this WeaponDef weapon, DamageKeywordDef type ) => Modify( weapon, type, null );
      public static float GetDamage ( this WeaponDef weapon, DamageKeywordDef type ) => getDamage( weapon, type );

      public static WeaponDef setDamage ( this WeaponDef weapon, float value ) => SetDamage( weapon, DmgType.DamageKeyword, value );
      public static WeaponDef SetDamage ( this WeaponDef weapon, float value ) => setDamage( weapon, value );

      public static WeaponDef setDamage ( this WeaponDef weapon, string type, float value ) => SetDamage( weapon, Parse( type ), value );
      public static WeaponDef SetDamage ( this WeaponDef weapon, string type, float value ) => setDamage( weapon, type, value );

      public static WeaponDef SetDamage ( this WeaponDef weapon, DamageKeywordDef type, float value ) => setDamage( weapon, type, value );
      public static WeaponDef setDamage ( this WeaponDef weapon, DamageKeywordDef type, float value ) {
         ExchangeDamage( weapon, type, value );
         return weapon;
      }

      public static float addDamage ( this WeaponDef weapon, string type, float value ) => AddDamage( weapon, Parse( type ), value );
      public static float AddDamage ( this WeaponDef weapon, string type, float value ) => addDamage( weapon, type, value );

      public static float addDamage ( this WeaponDef weapon, DamageKeywordDef type, float value ) => Modify( weapon, type, ( orig ) => orig + value ) + value;
      public static float AddDamage ( this WeaponDef weapon, DamageKeywordDef type, float value ) => addDamage( weapon, type, value );

      public static float exchangeDamage ( this WeaponDef weapon, string type, float value ) => ExchangeDamage( weapon, Parse( type ), value );
      public static float ExchangeDamage ( this WeaponDef weapon, string type, float value ) => exchangeDamage( weapon, type, value );

      public static float exchangeDamage ( this WeaponDef weapon, DamageKeywordDef type, float value ) => Modify( weapon, type, ( _ ) => value );
      public static float ExchangeDamage ( this WeaponDef weapon, DamageKeywordDef type, float value ) => exchangeDamage( weapon, type, value );

      public static float compareExchangeDamage ( this WeaponDef weapon, string type, float ifMatch, float setTo ) => CompareExchangeDamage( weapon, Parse( type ), ifMatch, setTo );
      public static float CompareExchangeDamage ( this WeaponDef weapon, string type, float ifMatch, float setTo ) => compareExchangeDamage( weapon, type, ifMatch, setTo );

      public static float compareExchangeDamage ( this WeaponDef weapon, DamageKeywordDef type, float ifMatch, float setTo ) => Modify( weapon, type, ( orig ) => orig == ifMatch ? setTo : orig );
      public static float CompareExchangeDamage ( this WeaponDef weapon, DamageKeywordDef type, float ifMatch, float setTo ) => compareExchangeDamage( weapon, type, ifMatch, setTo );

      public static float Acid ( this WeaponDef weapon ) => acid( weapon );
      public static WeaponDef Acid ( this WeaponDef weapon, float value ) => acid( weapon, value );
      public static float Blast ( this WeaponDef weapon ) => blast( weapon );
      public static WeaponDef Blast ( this WeaponDef weapon, float value ) => blast( weapon, value );
      public static float Bleed ( this WeaponDef weapon ) => bleed( weapon );
      public static WeaponDef Bleed ( this WeaponDef weapon, float value ) => bleed( weapon, value );
      public static float Damage ( this WeaponDef weapon ) => damage( weapon );
      public static WeaponDef Damage ( this WeaponDef weapon, float value ) => damage( weapon, value );
      public static float Fire ( this WeaponDef weapon ) => fire( weapon );
      public static WeaponDef Fire ( this WeaponDef weapon, float value ) => fire( weapon, value );
      public static float Goo ( this WeaponDef weapon ) => goo( weapon );
      public static WeaponDef Goo ( this WeaponDef weapon, float value ) => goo( weapon, value );
      public static float Mist ( this WeaponDef weapon ) => mist( weapon );
      public static WeaponDef Mist ( this WeaponDef weapon, float value ) => mist( weapon, value );
      public static float Paralyze ( this WeaponDef weapon ) => paralyze( weapon );
      public static WeaponDef Paralyze ( this WeaponDef weapon, float value ) => paralyze( weapon, value );
      public static float Pierce ( this WeaponDef weapon ) => pierce( weapon );
      public static WeaponDef Pierce ( this WeaponDef weapon, float value ) => pierce( weapon, value );
      public static float Poison ( this WeaponDef weapon ) => poison( weapon );
      public static WeaponDef Poison ( this WeaponDef weapon, float value ) => poison( weapon, value );
      public static float Psychic ( this WeaponDef weapon ) => psychic( weapon );
      public static WeaponDef Psychic ( this WeaponDef weapon, float value ) => psychic( weapon, value );
      public static float Shred ( this WeaponDef weapon ) => shred( weapon );
      public static WeaponDef Shred ( this WeaponDef weapon, float value ) => shred( weapon, value );
      public static float Sonic ( this WeaponDef weapon ) => sonic( weapon );
      public static WeaponDef Sonic ( this WeaponDef weapon, float value ) => sonic( weapon, value );
      public static float Vampiric ( this WeaponDef weapon ) => vampiric( weapon );
      public static WeaponDef Vampiric ( this WeaponDef weapon, float value ) => vampiric( weapon, value );
      public static float Viral ( this WeaponDef weapon ) => viral( weapon );
      public static WeaponDef Viral ( this WeaponDef weapon, float value ) => viral( weapon, value );
      public static float Virophage ( this WeaponDef weapon ) => virophage( weapon );
      public static WeaponDef Virophage ( this WeaponDef weapon, float value ) => virophage( weapon, value );

      public static float acid ( this WeaponDef weapon ) => GetDamage( weapon, "acid" );
      public static WeaponDef acid ( this WeaponDef weapon, float value ) => SetDamage( weapon, "acid", value );
      public static float blast ( this WeaponDef weapon ) => GetDamage( weapon, "blast" );
      public static WeaponDef blast ( this WeaponDef weapon, float value ) => SetDamage( weapon, "blast", value );
      public static float bleed ( this WeaponDef weapon ) => GetDamage( weapon, "bleed" );
      public static WeaponDef bleed ( this WeaponDef weapon, float value ) => SetDamage( weapon, "bleed", value );
      public static float damage ( this WeaponDef weapon ) => GetDamage( weapon, "" );
      public static WeaponDef damage ( this WeaponDef weapon, float value ) => SetDamage( weapon, "", value );
      public static float fire ( this WeaponDef weapon ) => GetDamage( weapon, "fire" );
      public static WeaponDef fire ( this WeaponDef weapon, float value ) => SetDamage( weapon, "fire", value );
      public static float goo ( this WeaponDef weapon ) => GetDamage( weapon, "goo" );
      public static WeaponDef goo ( this WeaponDef weapon, float value ) => SetDamage( weapon, "goo", value );
      public static float mist ( this WeaponDef weapon ) => GetDamage( weapon, "mist" );
      public static WeaponDef mist ( this WeaponDef weapon, float value ) => SetDamage( weapon, "mist", value );
      public static float paralyze ( this WeaponDef weapon ) => GetDamage( weapon, "paralyse" );
      public static WeaponDef paralyze ( this WeaponDef weapon, float value ) => SetDamage( weapon, "paralyse", value );
      public static float pierce ( this WeaponDef weapon ) => GetDamage( weapon, "pierce" );
      public static WeaponDef pierce ( this WeaponDef weapon, float value ) => SetDamage( weapon, "pierce", value );
      public static float poison ( this WeaponDef weapon ) => GetDamage( weapon, "poison" );
      public static WeaponDef poison ( this WeaponDef weapon, float value ) => SetDamage( weapon, "poison", value );
      public static float psychic ( this WeaponDef weapon ) => GetDamage( weapon, "psychic" );
      public static WeaponDef psychic ( this WeaponDef weapon, float value ) => SetDamage( weapon, "psychic", value );
      public static float shred ( this WeaponDef weapon ) => GetDamage( weapon, "shred" );
      public static WeaponDef shred ( this WeaponDef weapon, float value ) => SetDamage( weapon, "shred", value );
      public static float sonic ( this WeaponDef weapon ) => GetDamage( weapon, "sonic" );
      public static WeaponDef sonic ( this WeaponDef weapon, float value ) => SetDamage( weapon, "sonic", value );
      public static float vampiric ( this WeaponDef weapon ) => GetDamage( weapon, "vampiric" );
      public static WeaponDef vampiric ( this WeaponDef weapon, float value ) => SetDamage( weapon, "vampiric", value );
      public static float viral ( this WeaponDef weapon ) => GetDamage( weapon, "viral" );
      public static WeaponDef viral ( this WeaponDef weapon, float value ) => SetDamage( weapon, "viral", value );
      public static float virophage ( this WeaponDef weapon ) => GetDamage( weapon, "virophage" );
      public static WeaponDef virophage ( this WeaponDef weapon, float value ) => SetDamage( weapon, "virophage", value );
   }
}
