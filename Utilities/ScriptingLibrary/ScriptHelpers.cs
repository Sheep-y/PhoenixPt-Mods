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
   public static class ScriptHelpers {

      public static object Api ( string action ) => Api( action, null );

      public static object Api < T > ( string action ) => Api< T >( action, null );

      public static object Api ( string action, object param ) => ZyMod.Api( action, param );

      public static T Api < T > ( string action, object param ) {
         var result = Api ( action, param );
         if ( result is Exception ex ) throw ex;
         return (T) result;
      }

      private static DefRepository _Repo;
      public static DefRepository Repo => _Repo ?? ( _Repo = GameUtl.GameComponent< DefRepository >() );

      public static BaseDef GetDef ( string key ) => GetDef< BaseDef >( key );

      public static T GetDef< T > ( string key ) where T : BaseDef => (T) DataCache.API_PP_Def( null, key );

      public static IEnumerable< BaseDef > GetDefs ( object nameOrType ) => DataCache.API_PP_Defs( null, nameOrType );

      public static IEnumerable< T > GetDefs< T > ( object nameOrType ) where T : BaseDef => DataCache.API_PP_Defs( null, nameOrType ).OfType<T>();

      private static SharedDamageKeywordsDataDef DmgType;

      public static DamageKeywordDef GetDamageType ( string type ) {
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
            case "virophage" : return (DamageKeywordDef) Repo.GetDef( "c968f22f-392d-1964-68cd-edd14655082d" );
            case "normal" : case "" :
               return DmgType.DamageKeyword;
         }
         throw new ArgumentException( "Unknown damage type: " + type );
      }

      private static float SyncDamage ( this WeaponDef weapon, DamageKeywordDef damageType, Func<float,float> handler ) { lock ( weapon ) {
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

      public static float GetDamage ( this WeaponDef weapon, string type ) => GetDamage( weapon, GetDamageType( type ) );

      public static float GetDamage ( this WeaponDef weapon, DamageKeywordDef type ) => SyncDamage( weapon, type, null );

      public static WeaponDef SetDamage ( this WeaponDef weapon, string type, float value ) => SetDamage( weapon, GetDamageType( type ), value );

      public static WeaponDef SetDamage ( this WeaponDef weapon, DamageKeywordDef type, float value ) {
         ExchangeDamage( weapon, type, value );
         return weapon;
      }

      public static float AddDamage ( this WeaponDef weapon, string type, float value ) => AddDamage( weapon, GetDamageType( type ), value );

      public static float AddDamage ( this WeaponDef weapon, DamageKeywordDef type, float value ) => SyncDamage( weapon, type, ( orig ) => orig + value );

      public static float ExchangeDamage ( this WeaponDef weapon, string type, float value ) => ExchangeDamage( weapon, GetDamageType( type ), value );

      public static float ExchangeDamage ( this WeaponDef weapon, DamageKeywordDef type, float value ) => SyncDamage( weapon, type, ( _ ) => value );

      public static float CompareExchangeDamage ( this WeaponDef weapon, string type, float ifMatch, float setTo ) => CompareExchangeDamage( weapon, GetDamageType( type ), ifMatch, setTo );

      public static float CompareExchangeDamage ( this WeaponDef weapon, DamageKeywordDef type, float ifMatch, float setTo ) =>
         SyncDamage( weapon, type, ( orig ) => orig == ifMatch ? setTo : orig );

      public static float Acid ( this WeaponDef weapon ) => GetDamage( weapon, "acid" );
      public static WeaponDef Acid ( this WeaponDef weapon, float value ) => SetDamage( weapon, "acid", value );
      public static float Blast ( this WeaponDef weapon ) => GetDamage( weapon, "blast" );
      public static WeaponDef Blast ( this WeaponDef weapon, float value ) => SetDamage( weapon, "blast", value );
      public static float Bleed ( this WeaponDef weapon ) => GetDamage( weapon, "bleed" );
      public static WeaponDef Bleed ( this WeaponDef weapon, float value ) => SetDamage( weapon, "bleed", value );
      public static float Damage ( this WeaponDef weapon ) => GetDamage( weapon, "" );
      public static WeaponDef Damage ( this WeaponDef weapon, float value ) => SetDamage( weapon, "", value );
      public static float Fire ( this WeaponDef weapon ) => GetDamage( weapon, "fire" );
      public static WeaponDef Fire ( this WeaponDef weapon, float value ) => SetDamage( weapon, "fire", value );
      public static float Goo ( this WeaponDef weapon ) => GetDamage( weapon, "goo" );
      public static WeaponDef Goo ( this WeaponDef weapon, float value ) => SetDamage( weapon, "goo", value );
      public static float Mist ( this WeaponDef weapon ) => GetDamage( weapon, "mist" );
      public static WeaponDef Mist ( this WeaponDef weapon, float value ) => SetDamage( weapon, "mist", value );
      public static float Paralyze ( this WeaponDef weapon ) => GetDamage( weapon, "paralyse" );
      public static WeaponDef Paralyze ( this WeaponDef weapon, float value ) => SetDamage( weapon, "paralyse", value );
      public static float Pierce ( this WeaponDef weapon ) => GetDamage( weapon, "pierce" );
      public static WeaponDef Pierce ( this WeaponDef weapon, float value ) => SetDamage( weapon, "pierce", value );
      public static float Poison ( this WeaponDef weapon ) => GetDamage( weapon, "poison" );
      public static WeaponDef Poison ( this WeaponDef weapon, float value ) => SetDamage( weapon, "poison", value );
      public static float Psychic ( this WeaponDef weapon ) => GetDamage( weapon, "psychic" );
      public static WeaponDef Psychic ( this WeaponDef weapon, float value ) => SetDamage( weapon, "psychic", value );
      public static float Shred ( this WeaponDef weapon ) => GetDamage( weapon, "shred" );
      public static WeaponDef Shred ( this WeaponDef weapon, float value ) => SetDamage( weapon, "shred", value );
      public static float Sonic ( this WeaponDef weapon ) => GetDamage( weapon, "sonic" );
      public static WeaponDef Sonic ( this WeaponDef weapon, float value ) => SetDamage( weapon, "sonic", value );
      public static float Vampiric ( this WeaponDef weapon ) => GetDamage( weapon, "vampiric" );
      public static WeaponDef Vampiric ( this WeaponDef weapon, float value ) => SetDamage( weapon, "vampiric", value );
      public static float Viral ( this WeaponDef weapon ) => GetDamage( weapon, "viral" );
      public static WeaponDef Viral ( this WeaponDef weapon, float value ) => SetDamage( weapon, "viral", value );
      public static float Virophage ( this WeaponDef weapon ) => GetDamage( weapon, "virophage" );
      public static WeaponDef Virophage ( this WeaponDef weapon, float value ) => SetDamage( weapon, "virophage", value );
   }
}
