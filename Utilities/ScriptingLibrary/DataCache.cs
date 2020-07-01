using Base.Core;
using Base.Defs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sheepy.PhoenixPt.ScriptingLibrary {
   using ModnixAPI = Func< string, object, object >;
   using SimpleCache = Dictionary< string, object >;
   using BaseDefs = IEnumerable< BaseDef >;

   internal class DataCache : ZyMod {

      internal static void RegisterAPI () {
         Api( "api_add pp.def", (ModnixAPI) API_PP_Def );
         Api( "api_add pp.defs", (ModnixAPI) API_PP_Defs );
      }

      public static BaseDef API_PP_Def ( string spec, object param ) {
         CreateCache();
         if ( param == null ) throw new ArgumentNullException( nameof( param ) );
         if ( param is string txt && ( "guid".Equals( spec ) || IsGuid( txt ) ) )
            return Repo.GetDef( txt );
         return GetDefs( spec, param ).FirstOrDefault();
      }

      public static BaseDefs API_PP_Defs ( string spec, object param ) {
         CreateCache();
         if ( param == null ) return AllDefs;
         return GetDefs( spec, param );
      }

      private static BaseDefs GetDefs ( string spec, object param ) {
         if ( param is string txt ) {
            if ( string.IsNullOrWhiteSpace( txt ) ) throw new ArgumentNullException( nameof( param ) );
            if ( IsGuid( txt ) ) return DefsByGuid( txt );
            BaseDefs result = null;
            if ( ! txt.Contains( '/' ) )
               result = GetDefs( ByName, txt ) ?? GetDefs( ByPath, txt );
            else
               result = GetDefs( ByPath, txt ) ?? GetDefs( ByName, txt );
            if ( result == null ) {
               Type t = ByType.Keys.FirstOrDefault( e => e.Name == txt || e.FullName == txt );
               if ( t != null ) result = ListDefs( t );
            }
            return result ?? NoDefs;

         } else if ( param is Type type )
            return ListDefs( type ) ?? NoDefs;
         throw new ArgumentException( "Unknown param type " + param.GetType() );
      }

      public static DefRepository Repo;
      private static readonly Dictionary< Type, List< BaseDef > > ByType = new Dictionary< Type, List< BaseDef > >();
      private static SimpleCache ByName;
      private static SimpleCache ByPath;

      // Master game def
      public static List< BaseDef > AllDefs => Repo.DefRepositoryDef.AllDefs;

      public static BaseDef[] NoDefs => Array.Empty< BaseDef >();

      private static BaseDef[] DefsByGuid ( string txt ) {
         var def = Repo.GetDef( txt );
         return def == null ? new BaseDef[] { def } : NoDefs;
      }

      public static BaseDef[] DefsByName ( string key ) { CreateCache(); return GetDefs( ByName, key ); }

      public static BaseDef[] DefsByPath ( string key ) { CreateCache(); return GetDefs( ByPath, key ); }

      private static BaseDef[] GetDefs ( SimpleCache cache, string key ) {
         if ( cache.TryGetValue( key, out object data ) ) {
            if ( data is BaseDef def ) return new BaseDef[]{ def };
            return data as BaseDef[];
         }
         if ( ! RebuildCache() ) return null;
         return GetDefs( cache, key );
      }

      private static BaseDefs ListDefs ( Type t ) {
         if ( ByType.TryGetValue( t, out List< BaseDef > list ) ) return list;
         if ( ! RebuildCache() ) return null;
         return ListDefs( t );
      }

      private static bool CreateCache () { lock ( ByType ) {
         if ( Repo != null ) return false;
         Verbo( "Building BaseDef cache" );
         Repo = GameUtl.GameComponent< DefRepository >();
         ByType[ typeof( BaseDef ) ] = new List<BaseDef>( AllDefs );
         ByName = new SimpleCache();
         ByPath = new SimpleCache();
         foreach ( var def in AllDefs )
            AddToCache( def );
         Info( "Built cache from {0} BaseDef", ByType[ typeof( BaseDef ) ].Count );
         return true;
      } }

      private static bool RebuildCache () { lock ( ByType ) {
         //if ( Repo == null ) { CreateCache(); return false; }
         var count = AllDefs.Count;
         if ( count <= ByType[ typeof( BaseDef ) ].Count ) return false;
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
            cache[ key ] = new BaseDef[]{ data as BaseDef, def };
      }

      private static void AddToTypeCache ( BaseDef def ) {
         Type t = def.GetType();
         do {
            if ( ! ByType.TryGetValue( t, out List< BaseDef > list ) )
               ByType[ t ] = list = new List< BaseDef >();
            list.Add( def );
            t = t.BaseType;
         } while ( t != typeof( BaseDef ) );
      }

      public static bool IsGuid ( string txt ) => txt.Length == 36 &&
         txt[8] == '-' /* && txt[13] == '-' && txt[18] == '-' */ && txt[23] == '-' && txt[35] <= 'f';
   }
}
