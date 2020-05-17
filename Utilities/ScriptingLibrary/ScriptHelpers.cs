using Base.Core;
using Base.Defs;
using com.ootii.Utilities.Debug;
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
            case "paralyse" : case "paralyze" :
               return DmgType.ParalysingKeyword;
            case "pierce" : return DmgType.PiercingKeyword;
            case "poison" : return DmgType.PoisonousKeyword;
            case "psychic" : return DmgType.PsychicKeyword;
            case "shock" : return DmgType.ShockKeyword;
            case "shred" : return DmgType.ShreddingKeyword;
            case "sonic" : return DmgType.SonicKeyword;
            case "syphon" : return DmgType.SyphonKeyword;
            case "viral" : return DmgType.ViralKeyword;
            case "virophage" : return Repo.GetDef( "c5a8301b-5f09-18f2-5c0f-2ddd62339e2c" ) as DamageKeywordDef;
            case "normal" : case "" :
               return DmgType.DamageKeyword;
         }
         throw new ArgumentException( "Unknown damage type: " + type );
      }

      private static float SyncDamage ( this WeaponDef weapon, DamageKeywordDef type, Func<float,float> handler ) { lock ( weapon ) {
         var dType = type.GetType();
         var list = weapon.DamagePayload.DamageKeywords;
         var pair = list.FirstOrDefault( e => dType.IsAssignableFrom( e.DamageKeywordDef.GetType() ) );
         var orig = pair?.Value ?? 0;
         if ( handler == null ) return orig;
         var value = handler( orig );
         if ( value == orig ) return orig;
         object[] logArg = new object[]{ weapon.name, type.name, orig, value };
         if ( value > 0 ) {
            if ( pair == null ) {
               ZyMod.ApiLog( TraceEventType.Start, "Adding {1} {3} to {0}", logArg );
               list.Add( pair = new DamageKeywordPair { DamageKeywordDef = type, Value = value } );
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
         SyncDamage( weapon, type, ( _ ) => value );
         return weapon;
      }
   }
}
