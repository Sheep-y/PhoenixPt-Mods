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

      public static BaseDef GetDef ( string guid ) => GetDef< BaseDef >( guid );

      public static T GetDef< T > ( string guid ) where T : BaseDef {
         if ( string.IsNullOrEmpty( guid ) ) return null;
         BaseDef result = null;
         if ( DataCache.IsGuid( guid ) ) result = Repo.GetDef( guid );
         if ( result is T r ) return r;
         var list = DataCache.DefsByName( guid ) ?? DataCache.DefsByPath( guid );
         if ( list != null && list.Length > 0 ) return list.OfType<T>().FirstOrDefault();
         return null;
      }
   }
}
