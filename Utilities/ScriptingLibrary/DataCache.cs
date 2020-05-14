using Base.Core;
using Base.Defs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sheepy.PhoenixPt.ScriptingLibrary {
   using ModnixAPI = Func<string,object,object>;
   using SimpleCache = Dictionary< string, object >;
   using BaseDefs = IEnumerable< BaseDef >;

   internal class DataCache : ZyMod {

      internal static void RegisterAPI () {
         Api( "api_add pp.def", (ModnixAPI) API_PP_Def );
         Api( "api_add pp.defs", (ModnixAPI) API_PP_Defs );
      }

      private static BaseDef API_PP_Def ( string spec, object param ) => API_PP_Defs( spec, param ).FirstOrDefault();

      private static BaseDefs API_PP_Defs ( string spec, object param ) {
         var mayRescan = ! CreateCache();
         if ( param == null ) return ByType[ typeof( BaseDef ) ];
         if ( param is string txt ) {
            if ( IsGuid( txt ) ) return GetByGuid( txt );
            BaseDefs result = null;
            if ( ! txt.Contains( '/' ) )
               result = GetDefs( ByName, txt, mayRescan ) ?? GetDefs( ByPath, txt, false );
            else
               result = GetDefs( ByPath, txt, mayRescan ) ?? GetDefs( ByName, txt, false );
            if ( result == null ) {
               Type t = ByType.Keys.FirstOrDefault( e => e.Name == txt || e.FullName == txt );
               if ( t != null ) result = ListDefs( t, mayRescan );
            }
            return result ?? NoDefs;

         } else if ( param is Type type )
            return ListDefs( type, mayRescan ) ?? NoDefs;
         throw new ArgumentException( "Unknown param type " + param.GetType() );

      }

      private static BaseDefs ListDefs ( Type t, bool rescanOnMiss ) {
         if ( ByType.TryGetValue( t, out List< BaseDef > list ) ) return list;
         if ( ! rescanOnMiss ) return null;
         RebuildCache();
         return ListDefs( t, false );
      }

      private static DefRepository Repo;
      private static Dictionary< Type, List< BaseDef > > ByType = new Dictionary< Type, List< BaseDef > >();
      private static SimpleCache ByName;
      private static SimpleCache ByPath;

      private static BaseDefs GetAllDefs () => Repo.DefRepositoryDef.AllDefs;

      private static BaseDefs GetByGuid ( string txt ) {
         var def = Repo.GetDef( txt );
         return def == null ? new BaseDef[] { def } : NoDefs;
      }

      private static BaseDefs NoDefs => Enumerable.Empty< BaseDef >();

      private static BaseDefs GetDefs ( SimpleCache cache, string key, bool rescanOnMiss ) {
         if ( cache.TryGetValue( key, out object data ) ) {
            if ( data is BaseDef def ) return new BaseDef[]{ def };
            return data as BaseDefs;
         }
         if ( ! rescanOnMiss || ! RebuildCache() ) return null;
         return GetDefs( cache, key, false );
      }

      private static bool CreateCache () { lock ( ByType ) {
         if ( Repo != null ) return false;
         Verbo( "Building BaseDef cache" );
         Repo = GameUtl.GameComponent< DefRepository >();
         ByName = new SimpleCache();
         ByPath = new SimpleCache();
         foreach ( var def in GetAllDefs() )
            AddToCache( def );
         Info( "Built cache from {0} BaseDef", ByType[ typeof( BaseDef ) ].Count );
         return true;
      } }

      private static bool RebuildCache () { lock ( ByType ) {
         if ( Repo == null ) { CreateCache(); return false; }
         var count = GetAllDefs().Count();
         if ( count == ByType[ typeof( BaseDef ) ].Count ) return false;
         Verbo( "Cache miss; rebuilding BaseDef cache." );
         foreach ( var list in ByType.Values ) list.Clear();
         ByName.Clear();
         ByPath.Clear();
         CreateCache();
         return true;
      } }

      private static void AddToCache ( BaseDef def ) {
         if ( def == null ) return;
         AddToCache( ByName, def.name, def );
         AddToCache( ByPath, def.ResourcePath, def ); // path has too many dups
      }

      private static void AddToCache ( SimpleCache cache, string key, BaseDef def ) {
         AddToTypeCache( def );
         if ( key == null ) key = "";
         if ( ! cache.TryGetValue( key, out object data ) ) {
            cache.Add( key, def );
            return;
         }
         if ( data is BaseDef[] list ) {
            var len = list.Length;
            var newList = new BaseDef[ len + 1 ];
            Array.Copy( list, newList, len );
            newList[ len ] = def;
            cache[ key ] = newList;
         } else
            cache[ key ] = list = new BaseDef[]{ data as BaseDef, def };
      }

      private static void AddToTypeCache ( BaseDef def ) {
         Type t = def.GetType();
         do {
            if ( ! ByType.TryGetValue( t, out List< BaseDef > list ) )
               ByType[ t ] = list = new List< BaseDef >();
            list.Add( def );
            if ( t == typeof( BaseDefs ) ) return;
            t = t.BaseType;
         } while ( t != null );
      }

      private static bool IsGuid ( string txt ) => txt.Length == 36 && txt[8] == '-' /* && txt[13] == '-' && txt[18] == '-' */ && txt[23] == '-';
   }
}