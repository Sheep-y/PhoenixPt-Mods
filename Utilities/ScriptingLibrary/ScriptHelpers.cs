using Base.Core;
using Base.Defs;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Sheepy.PhoenixPt.ScriptingLibrary {
   public static class LogHelper {
      public static void Error ( object msg, params object[] args ) => ZyMod.Error( msg, args );
      public static void Warn ( object msg, params object[] args ) => ZyMod.Warn( msg, args );
      public static void Warning ( object msg, params object[] args ) => Warn( msg, args );
      public static void Info ( object msg, params object[] args ) => ZyMod.Info( msg, args );
      public static void Verbo ( object msg, params object[] args ) => ZyMod.Verbo( msg, args );
   }

   public static class ApiHelper {
      public static object Call ( string action, object param = null ) => ZyMod.Api( action, param );
      public static T Call < T > ( string action, object param = null ) {
         var result = Call ( action, param );
         if ( result is Exception ex ) throw ex;
         return (T) result;
      }
   }

   public static class RepoHelper {
      private static DefRepository _Repo;
      public static DefRepository Repo => _Repo ?? ( _Repo = GameUtl.GameComponent< DefRepository >() );

      public static BaseDef Get ( string key ) => Get< BaseDef >( key );

      public static T Get< T > ( string key ) where T : BaseDef => (T) DataCache.API_PP_Def( null, key );

      public static IEnumerable< BaseDef > GetAll ( object nameOrType ) => DataCache.API_PP_Defs( null, nameOrType );

      public static IEnumerable< T > GetAll< T > ( object nameOrType = null ) where T : BaseDef => DataCache.API_PP_Defs( null, nameOrType ).OfType<T>();
   }

   public static class DamageHelpers {
      private static SharedDamageKeywordsDataDef DmgType;

      public static DamageKeywordDef Parse ( string type ) {
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
            case "normal" : case "" :
               return DmgType.DamageKeyword;
         }
         throw new ArgumentException( "Unknown damage type: " + type );
      }

      private static float Modify ( this WeaponDef weapon, DamageKeywordDef damageType, Func<float,float> handler ) { lock ( weapon ) {
         if ( weapon == null ) throw new ArgumentNullException( nameof( weapon ) );
         if ( damageType == null ) throw new ArgumentNullException( nameof( damageType ) );
         var list = weapon.DamagePayload.DamageKeywords;
         var pair = list.FirstOrDefault( e => e.DamageKeywordDef == damageType );
         var orig = pair?.Value ?? 0;
         if ( handler == null ) return orig;
         var value = handler( orig );
         if ( value == orig ) return orig;
         object[] logArg = new object[]{ weapon.name, damageType.name, orig, value };
         if ( value > 0 ) {
            if ( pair == null ) {
               ZyMod.ApiLog( TraceEventType.Start, "Adding {1} {3} to {0}", logArg );
               list.Add( pair = new DamageKeywordPair { DamageKeywordDef = damageType, Value = value } );
            } else {
               ZyMod.ApiLog( TraceEventType.Start, "Update {0} {1} {2} to {3}", logArg );
               pair.Value = value;
            }
         } else if ( pair != null ) {
            ZyMod.ApiLog( TraceEventType.Start, "Removing {1} {2} from {0}", logArg );
            list.Remove( pair );
         }
         return orig;
      } }

      public static float Get ( this WeaponDef weapon, string type ) => Get( weapon, Parse( type ) );

      public static float Get ( this WeaponDef weapon, DamageKeywordDef type ) => Modify( weapon, type, null );

      public static WeaponDef Set ( this WeaponDef weapon, string type, float value ) => Set( weapon, Parse( type ), value );

      public static WeaponDef Set ( this WeaponDef weapon, DamageKeywordDef type, float value ) {
         Exchange( weapon, type, value );
         return weapon;
      }

      public static float Add ( this WeaponDef weapon, string type, float value ) => Add( weapon, Parse( type ), value );

      public static float Add ( this WeaponDef weapon, DamageKeywordDef type, float value ) => Modify( weapon, type, ( orig ) => orig + value );

      public static float Exchange ( this WeaponDef weapon, string type, float value ) => Exchange( weapon, Parse( type ), value );

      public static float Exchange ( this WeaponDef weapon, DamageKeywordDef type, float value ) => Modify( weapon, type, ( _ ) => value );

      public static float CompareExchange ( this WeaponDef weapon, string type, float ifMatch, float setTo ) => CompareExchange( weapon, Parse( type ), ifMatch, setTo );

      public static float CompareExchange ( this WeaponDef weapon, DamageKeywordDef type, float ifMatch, float setTo ) =>
         Modify( weapon, type, ( orig ) => orig == ifMatch ? setTo : orig );

      public static float Acid ( this WeaponDef weapon ) => Get( weapon, "acid" );
      public static WeaponDef Acid ( this WeaponDef weapon, float value ) => Set( weapon, "acid", value );
      public static float Blast ( this WeaponDef weapon ) => Get( weapon, "blast" );
      public static WeaponDef Blast ( this WeaponDef weapon, float value ) => Set( weapon, "blast", value );
      public static float Bleed ( this WeaponDef weapon ) => Get( weapon, "bleed" );
      public static WeaponDef Bleed ( this WeaponDef weapon, float value ) => Set( weapon, "bleed", value );
      public static float Damage ( this WeaponDef weapon ) => Get( weapon, "" );
      public static WeaponDef Damage ( this WeaponDef weapon, float value ) => Set( weapon, "", value );
      public static float Fire ( this WeaponDef weapon ) => Get( weapon, "fire" );
      public static WeaponDef Fire ( this WeaponDef weapon, float value ) => Set( weapon, "fire", value );
      public static float Goo ( this WeaponDef weapon ) => Get( weapon, "goo" );
      public static WeaponDef Goo ( this WeaponDef weapon, float value ) => Set( weapon, "goo", value );
      public static float Mist ( this WeaponDef weapon ) => Get( weapon, "mist" );
      public static WeaponDef Mist ( this WeaponDef weapon, float value ) => Set( weapon, "mist", value );
      public static float Paralyze ( this WeaponDef weapon ) => Get( weapon, "paralyse" );
      public static WeaponDef Paralyze ( this WeaponDef weapon, float value ) => Set( weapon, "paralyse", value );
      public static float Pierce ( this WeaponDef weapon ) => Get( weapon, "pierce" );
      public static WeaponDef Pierce ( this WeaponDef weapon, float value ) => Set( weapon, "pierce", value );
      public static float Poison ( this WeaponDef weapon ) => Get( weapon, "poison" );
      public static WeaponDef Poison ( this WeaponDef weapon, float value ) => Set( weapon, "poison", value );
      public static float Psychic ( this WeaponDef weapon ) => Get( weapon, "psychic" );
      public static WeaponDef Psychic ( this WeaponDef weapon, float value ) => Set( weapon, "psychic", value );
      public static float Shred ( this WeaponDef weapon ) => Get( weapon, "shred" );
      public static WeaponDef Shred ( this WeaponDef weapon, float value ) => Set( weapon, "shred", value );
      public static float Sonic ( this WeaponDef weapon ) => Get( weapon, "sonic" );
      public static WeaponDef Sonic ( this WeaponDef weapon, float value ) => Set( weapon, "sonic", value );
      public static float Vampiric ( this WeaponDef weapon ) => Get( weapon, "vampiric" );
      public static WeaponDef Vampiric ( this WeaponDef weapon, float value ) => Set( weapon, "vampiric", value );
      public static float Viral ( this WeaponDef weapon ) => Get( weapon, "viral" );
      public static WeaponDef Viral ( this WeaponDef weapon, float value ) => Set( weapon, "viral", value );
      public static float Virophage ( this WeaponDef weapon ) => Get( weapon, "virophage" );
      public static WeaponDef Virophage ( this WeaponDef weapon, float value ) => Set( weapon, "virophage", value );
   }
}
