using Base.Core;
using Base.Defs;
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
   }
}
