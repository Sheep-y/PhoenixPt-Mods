﻿using Base.Core;
using Base.Defs;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
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

      public static T GetDef< T > ( string key ) where T : BaseDef =>
         DataCache.API_PP_Def( null, key ) as T;

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

      public static float GetDamage ( this WeaponDef weapon, string type ) => GetDamage( weapon, GetDamageType( type ) );

      public static float GetDamage ( this WeaponDef weapon, DamageKeywordDef type ) { lock ( weapon ) {
         var dType = type.GetType();
         var list = weapon.DamagePayload.DamageKeywords;
         var pair = list.FirstOrDefault( e => dType.IsAssignableFrom( e.DamageKeywordDef.GetType() ) );
         return pair?.Value ?? 0;
      } }

      public static WeaponDef SetDamage ( this WeaponDef weapon, DamageKeywordDef type, float value ) { lock ( weapon ) {
         var dType = type.GetType();
         var list = weapon.DamagePayload.DamageKeywords;
         var pair = list.FirstOrDefault( e => dType.IsAssignableFrom( e.DamageKeywordDef.GetType() ) );
         if ( value > 0 ) {
            if ( pair == null ) list.Add( pair = new DamageKeywordPair { DamageKeywordDef = type, Value = value } );
            else pair.Value = value;
         } else if ( pair != null )
            list.Remove( pair );
         return weapon;
      } }
   }
}
