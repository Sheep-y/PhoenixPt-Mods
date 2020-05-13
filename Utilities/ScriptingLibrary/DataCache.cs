using Base.Core;
using Base.Defs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sheepy.PhoenixPt.ScriptingLibrary {
   using ModnixAPI = Func<string,object,object>;

   internal class DataCache : ZyMod {

      internal static void RegisterAPI () {
         Api( "api_add pp.def", (ModnixAPI) API_PP_Def );
         Api( "api_add pp.defs", (ModnixAPI) API_PP_Defs );
      }

      private static BaseDef API_PP_Def ( string spec, object param ) => API_PP_Defs( spec, param )?.FirstOrDefault();

      private static IEnumerable< BaseDef > API_PP_Defs ( string spec, object param ) {
         CreateCache();
         if ( param == null ) return GetAllDefs();
         if ( param is string txt ) {
            if ( IsGuid( txt ) ) return GetDefs( ByGuid, txt );
            else if ( ! txt.Contains( '/' ) ) return GetDefs( ByName, txt );
            else return GetDefs( ByPath, txt );
         }
         throw new ArgumentException( "Unknown param type " + param.GetType() );
      }

      private static Dictionary<Type,List<BaseDef>> ByType = new Dictionary<Type, List<BaseDef>>();
      private static Dictionary<string,BaseDef[]> ByName;
      private static Dictionary<string,BaseDef[]> ByGuid;
      private static Dictionary<string,BaseDef[]> ByPath;

      private static IEnumerable<T> GetAllDefs<T> () where T : BaseDef => GameUtl.GameComponent<DefRepository>().GetAllDefs<T>();
      private static IEnumerable<BaseDef> GetAllDefs () => GameUtl.GameComponent<DefRepository>().GetAllDefs( typeof( BaseDef ), true );
      private static IEnumerable<BaseDef> NoDefs => Enumerable.Empty<BaseDef>();

      private static IEnumerable<BaseDef> GetDefs ( Dictionary<string,BaseDef[]> cache, string key ) =>
         cache.TryGetValue( key, out BaseDef[] list ) ? list : NoDefs;

      private static void CreateCache () { lock ( ByType ) {
         if ( ByName != null ) return;
         Info( "Building data cache" );
         ByName = new Dictionary<string, BaseDef[]>();
         ByGuid = new Dictionary<string, BaseDef[]>();
         ByPath = new Dictionary<string, BaseDef[]>();
         foreach ( var def in GetAllDefs<BaseDef>() )
            AddToCache( def );
         Verbo( "Built cache from {0} BaseDef", ByGuid.Count );
      } }

      private static void AddToCache ( BaseDef def ) {
         if ( def == null ) return;
         AddToCache( ByGuid, def.Guid, def, true );
         AddToCache( ByName, def.name, def, false );
         AddToCache( ByPath, def.ResourcePath, def, false ); // path has too many dups
      }

      private static void AddToCache ( Dictionary<string,BaseDef[]> cache, string key, BaseDef def, bool warnOnDup ) {
         if ( key == null ) key = "";
         if ( ! cache.TryGetValue( key, out BaseDef[] list ) ) {
            cache.Add( key, new BaseDef[]{ def } );
            return;
         }
         if ( warnOnDup && key.Length > 0 )
            Warn( "Duplicate {0}: \"{1}\" ~ \"{2}\"", key, list[0].name, def.name );
         var len = list.Length;
         var newList = new BaseDef[ len + 1 ];
         Array.Copy( list, newList, len );
         newList[ len ] = def;
         cache[ key ] = newList;
      }

      private static bool IsGuid ( string txt ) => txt.Length == 36 && txt[8] == '-' /* && txt[13] == '-' && txt[18] == '-' */ && txt[23] == '-';
   }
}