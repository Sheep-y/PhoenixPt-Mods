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
         CreateCache();
         if ( param == null ) return GetAllDefs();
         if ( param is string txt ) {
            if ( IsGuid( txt ) ) return GetDefs( ByGuid, txt );
            else if ( ! txt.Contains( '/' ) ) return GetDefs( ByName, txt );
            else return GetDefs( ByPath, txt );
         } else if ( param is Type type )
            return ByType.TryGetValue( type, out List< BaseDef > list ) ? list : NoDefs;
         throw new ArgumentException( "Unknown param type " + param.GetType() );
      }

      private static DefRepository Repo;
      private static Dictionary< Type, List< BaseDef > > ByType = new Dictionary< Type, List< BaseDef > >();
      private static SimpleCache ByName;
      private static SimpleCache ByGuid;
      private static SimpleCache ByPath;

      private static IEnumerable< T > GetAllDefs< T > () where T : BaseDef => Repo.GetAllDefs< T >();
      private static BaseDefs GetAllDefs () => Repo.GetAllDefs( typeof( BaseDef ), true );
      private static BaseDefs NoDefs => Enumerable.Empty< BaseDef >();

      private static BaseDefs GetDefs ( SimpleCache cache, string key ) {
         if ( ! cache.TryGetValue( key, out object data ) ) return NoDefs;
         if ( data is BaseDef def ) return new BaseDef[]{ def };
         return data as BaseDefs ?? NoDefs;
      }

      private static void CreateCache () { lock ( ByType ) {
         if ( Repo != null ) return;
         Repo = GameUtl.GameComponent< DefRepository >();
         Info( "Building data cache" );
         ByName = new SimpleCache();
         ByGuid = new SimpleCache();
         ByPath = new SimpleCache();
         foreach ( var def in GetAllDefs< BaseDef >() )
            AddToCache( def );
         Verbo( "Built cache from {0} BaseDef", ByGuid.Count );
      } }

      private static void AddToCache ( BaseDef def ) {
         if ( def == null ) return;
         AddToCache( ByGuid, def.Guid, def, true );
         AddToCache( ByName, def.name, def, false );
         AddToCache( ByPath, def.ResourcePath, def, false ); // path has too many dups
      }

      private static void AddToCache ( SimpleCache cache, string key, BaseDef def, bool warnOnDup ) {
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
         if ( warnOnDup && key.Length > 0 )
            Warn( "Duplicate {0}: \"{1}\" ~ \"{2}\"", key, list[0].name, def.name );
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