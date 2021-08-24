using Base.Core;
using Base.Defs;
using Base.Utils.GameConsole;
using Harmony;
using Microsoft.ClearScript;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using static System.Reflection.BindingFlags;

namespace Sheepy.PhoenixPt.ScriptingLibrary {
   using Logger = Action< TraceEventType, object, object[] >;
   using CallbackList = IList< KeyValuePair< string, ScriptObject > >;

   public static class ConsoleHelper {
      public static void assert ( bool assertion, params object[] args ) { if ( ! assertion ) error( "Assertion failed:", args ); }
      public static void clear () => GameConsoleWindow.Create().espy().invoke( "ClearScreen" );
      public static void dir ( object obj ) => ScriptingExt.WriteToConsole( obj );
      public static void debug ( object msg, params object[] args ) => GetLogger()( TraceEventType.Verbose, msg, args );
      public static void error ( object msg, params object[] args ) => GetLogger()( TraceEventType.Error, msg, args );
      public static void info ( object msg, params object[] args ) => GetLogger()( TraceEventType.Information, msg, args );
      public static void log ( object msg, params object[] args ) => GetLogger()( TraceEventType.Information, msg, args );
      public static void trace ( object msg, params object[] args ) => GetLogger()( TraceEventType.Transfer, msg, args );
      public static void warn ( object msg, params object[] args ) => GetLogger()( TraceEventType.Warning, msg, args );

      private static System.Collections.IList mods;
      private static MethodInfo loggerFunc;
      private static readonly IDictionary< string, Logger > loggerMap = new Dictionary< string, Logger >();

      private static Logger GetLogger () { try {
         lock ( loggerMap ) {
               if ( mods == null ) {
                  var type = ZyMod.FindAssembly( "ModnixLoader" )?.GetType( "Sheepy.Modnix.ModLoader" );
                  if ( type != null ) mods = Espy.create( type )?.field( "EnabledMods" ) as System.Collections.IList;
                  if ( mods == null ) mods = new List< object >( 0 );
                  else {
                     var loggerType = mods[0].espy().invoke( "Log" )?.GetType();
                     loggerFunc = loggerType?.GetMethod( "Log", new Type[]{ typeof( TraceEventType ), typeof( object ), typeof( object[] ) } );
                  }
               }
            }
         var mod_id = ScriptEngine.Current?.Name;
         Logger result = ZyMod.ApiLog;
         if ( string.IsNullOrEmpty( mod_id ) || loggerFunc == null ) return result;
         lock ( loggerMap ) if ( loggerMap.TryGetValue( mod_id, out result ) ) return result;
         foreach ( var mod in mods ) {
            if ( mod?.espy( "Metadata" )?.espy( "Id" )?.ToString() != mod_id ) continue;
            var logger = mod.espy().invoke( "Log" );
            result = loggerFunc.CreateDelegate( typeof( Logger ), logger ) as Logger ?? result;
            break;
         }
         lock ( loggerMap ) loggerMap.Add( mod_id, result );
         return result;
      } catch ( Exception ) { return ZyMod.ApiLog; } }
   }

   public static class LogHelper {
      public static void Error ( object msg, params object[] args ) => error( msg, args );
      public static void Warn ( object msg, params object[] args ) => warn( msg, args );
      public static void Warning ( object msg, params object[] args ) => warn( msg, args );
      public static void Info ( object msg, params object[] args ) => info( msg, args );
      public static void Verbo ( object msg, params object[] args ) => verbo( msg, args );
      public static void Verbose ( object msg, params object[] args ) => verbo( msg, args );
      public static void error ( object msg, params object[] args ) => ConsoleHelper.error( msg, args );
      public static void warn ( object msg, params object[] args ) => ConsoleHelper.warn( msg, args );
      public static void warning ( object msg, params object[] args ) => warn( msg, args );
      public static void info ( object msg, params object[] args ) => ConsoleHelper.info( msg, args );
      public static void verbo ( object msg, params object[] args ) => ConsoleHelper.debug( msg, args );
      public static void verbose ( object msg, params object[] args ) => verbo( msg, args );
   }

   public static class ApiHelper {
      public static object call ( string action, object param = null ) => ZyMod.Api( action, param );
      public static object Call ( string action, object param = null ) => call( action, param );
      public static T Call < T > ( string action, object param = null ) => call<T>( action, param );
      public static T call < T > ( string action, object param = null ) {
         var result = Call( action, param );
         if ( result is Exception ex ) throw ex;
         return (T) result;
      }
   }

   public static class ExtensionHelper {
      public static MethodInfo method ( this Type cls, string name ) => cls.GetMethod( name, PhoenixPt.ScriptingLibrary.Espy.AnyBinding );
      public static FieldInfo field ( this Type cls, string name ) => cls.GetField( name, PhoenixPt.ScriptingLibrary.Espy.AnyBinding );
      public static PropertyInfo property ( this Type cls, string name ) => cls.GetProperty( name, PhoenixPt.ScriptingLibrary.Espy.AnyBinding );
      public static IEnumerable<MethodInfo> methods ( this Type cls, string name ) => cls.GetMethods( PhoenixPt.ScriptingLibrary.Espy.AnyBinding ).Where( e => e.Name == name );
      public static IEnumerable<FieldInfo> fields ( this Type cls, string name ) => cls.GetFields( PhoenixPt.ScriptingLibrary.Espy.AnyBinding ).Where( e => e.Name == name );
      public static IEnumerable<PropertyInfo> properties ( this Type cls, string name ) => cls.GetProperties( PhoenixPt.ScriptingLibrary.Espy.AnyBinding ).Where( e => e.Name == name );
      public static MethodInfo Method ( this Type cls, string name ) => method( cls, name );
      public static FieldInfo Field ( this Type cls, string name ) => field( cls, name );
      public static PropertyInfo Property ( this Type cls, string name ) => property( cls, name );
      public static IEnumerable<MethodInfo> Methods ( this Type cls, string name ) => methods( cls, name );
      public static IEnumerable<FieldInfo> Fields ( this Type cls, string name ) => fields( cls, name );
      public static IEnumerable<PropertyInfo> Properties ( this Type cls, string name ) => properties( cls, name );

      public static Espy espy ( this object instance ) => PhoenixPt.ScriptingLibrary.Espy.create( instance );
      public static Espy Espy ( this object instance ) => espy( instance );
      public static object espy ( this object instance, string member ) {
         var mem = instance?.GetType()?.GetMember( member, PhoenixPt.ScriptingLibrary.Espy.AnyBinding )?[0];
         if ( mem == null ) return null;
         if ( mem is FieldInfo f ) return f.GetValue( instance );
         else if ( mem is PropertyInfo p ) return p.GetValue( instance );
         else return null;
      }
      public static object Espy ( this object instance, string member ) => espy( instance, member );
      public static bool espy ( this object instance, string member, object value ) {
         var mem = instance?.GetType()?.GetMember( member, PhoenixPt.ScriptingLibrary.Espy.AnyBinding )?[0];
         if ( mem == null ) return false;
         if ( mem is FieldInfo f ) f.SetValue( instance, value );
         else if ( mem is PropertyInfo p ) p.SetValue( instance, value );
         else return false;
         return true;
      }
      public static bool Espy ( this object instance, string member, object value ) => espy( instance, member, value );
   }

   public class Espy {
      public static Assembly gameAssembly => ZyMod.GameAssembly;
      public static Assembly GameAssembly => gameAssembly;
      public static Assembly unityCore => ZyMod.UnityCore;
      public static Assembly UnityCore => unityCore;

      public static Type getType ( string name ) => GameAssembly.GetType( name, false ) ?? ZyMod.UnityCore.GetType( name, false ) ?? Type.GetType( name, false );
      public static Type GetType ( string name ) => getType( name );
      public static Type getType < T > () => typeof( T );
      public static Type GetType < T > () => typeof( T );

      public const BindingFlags anyBinding = Static | Instance | Public | NonPublic;
      public const BindingFlags objectBinding = Instance | Public | NonPublic;
      public const BindingFlags staticBinding = Static   | Public | NonPublic;
      public const BindingFlags AnyBinding = anyBinding;
      public const BindingFlags ObjectBinding = objectBinding;
      public const BindingFlags StaticBinding = staticBinding;

      private BindingFlags myBinding => myObj == null ? StaticBinding : ObjectBinding;

      public readonly Type myType;
      public readonly object myObj;

      public Espy ( Type myType, object myObj = null ) {
         this.myType = myType;
         this.myObj = myObj;
      }

      public static Espy create ( object instance ) => new Espy( instance.GetType(), instance );
      public static Espy Create ( object instance ) => create( instance );
      public static Espy create < T > ( object instance = null ) => new Espy( typeof( T ), instance );
      public static Espy Create < T > ( object instance = null ) => create<T>( instance );
      public static Espy create ( Type type ) => new Espy( type, null );
      public static Espy Create ( Type type ) => create( type );

      public FieldInfo[] fields () => myType.GetFields( myBinding );
      public PropertyInfo[] properties () => myType.GetProperties( myBinding );
      public MethodInfo[] methods () => myType.GetMethods( myBinding );
      public FieldInfo[] Fields () => fields();
      public PropertyInfo[] Properties () => properties();
      public MethodInfo[] Methods () => methods();

      public object field ( string name ) => myType.GetField( name, myBinding )?.GetValue( myObj );
      public object property ( string name ) => myType.GetProperty( name, myBinding )?.GetValue( myObj );
      public object invoke ( string name, params object[] args ) => myType.GetMethod( name, myBinding )?.Invoke( myObj, args );
      public object Field ( string name ) => field( name );
      public object Property ( string name ) => property( name );
      public object Invoke ( string name, params object[] args ) => invoke( name, args );

      public void field ( string name, object value ) => myType.GetField( name, myBinding )?.SetValue( myObj, value );
      public void property ( string name, object value ) => myType.GetProperty( name, myBinding )?.SetValue( myObj, value );
      public void Field ( string name, object value ) => field( name, value );
      public void Property ( string name, object value ) => property( name, value );

      public static object field < T > ( string name ) => create<T>().field( name );
      public static object property < T > ( string name ) => create<T>().property( name );
      public static object call < T > ( string name, params object[] args ) => create<T>().invoke( name, args );
      public static object Field < T > ( string name ) => field<T>( name );
      public static object Property < T > ( string name ) => property<T>( name );
      public static object Call < T > ( string name, params object[] args ) => call<T>( name, args );

      public override string ToString () => $"Espy({myType.Name})";
   }

   public static class PatchHelper {
      private static readonly HarmonyInstance Harmony = HarmonyInstance.Create( "js.helper" );
      private static readonly IDictionary< MethodBase, CallbackList > Prefixes = new Dictionary< MethodBase, CallbackList >();
      private static readonly IDictionary< MethodBase, CallbackList > Postfixes = new Dictionary< MethodBase, CallbackList >();
      private static readonly IDictionary< MethodBase, CallbackList > PrefixReturns = new Dictionary< MethodBase, CallbackList >();
      private static readonly IDictionary< MethodBase, CallbackList > PostfixReturns = new Dictionary< MethodBase, CallbackList >();

      public static void prefix < T > ( string method, ScriptObject patch ) => prefix( typeof( T ), method, patch );
      public static void prefix ( object type, string method, ScriptObject patch ) => prefix( type.espy( "Type" ) as Type ?? type?.GetType(), method, patch );
      public static void prefix ( Type type, string method, ScriptObject patch ) => AddPatch( type, method, patch, Prefixes, nameof( PrefixProxy ) );

      public static void postfix < T > ( string method, ScriptObject patch ) => postfix( typeof( T ), method, patch );
      public static void postfix ( object type, string method, ScriptObject patch ) => postfix( type.espy( "Type" ) as Type ?? type?.GetType(), method, patch );
      public static void postfix ( Type type, string method, ScriptObject patch ) => AddPatch( type, method, patch, Postfixes, nameof( PostfixProxy ) );

      public static void prefixReturn < T > ( string method, ScriptObject patch ) => prefixReturn( typeof( T ), method, patch );
      public static void prefixReturn ( object type, string method, ScriptObject patch ) => prefixReturn( type.espy( "Type" ) as Type ?? type?.GetType(), method, patch );
      public static void prefixReturn ( Type type, string method, ScriptObject patch ) => AddPatch( type, method, patch, PrefixReturns, nameof( PreReturnProxy ) );

      public static void postfixReturn < T > ( string method, ScriptObject patch ) => postfixReturn( typeof( T ), method, patch );
      public static void postfixReturn ( object type, string method, ScriptObject patch ) => postfixReturn( type.espy( "Type" ) as Type ?? type?.GetType(), method, patch );
      public static void postfixReturn ( Type type, string method, ScriptObject patch ) => AddPatch( type, method, patch, PostfixReturns, nameof( PostReturnProxy ) );

      public static void Prefix < T > ( string method, ScriptObject patch ) => prefix< T >( method, patch );
      public static void Prefix ( object type, string method, ScriptObject patch ) => prefix( type, method, patch );
      public static void Prefix ( Type type, string method, ScriptObject patch ) => prefix( type, method, patch );
      public static void Postfix < T > ( string method, ScriptObject patch ) => postfix< T >( method, patch );
      public static void Postfix ( object type, string method, ScriptObject patch ) => postfix( type, method, patch );
      public static void Postfix ( Type type, string method, ScriptObject patch ) => postfix( type, method, patch );
      public static void PrefixReturn < T > ( string method, ScriptObject patch ) => prefixReturn< T >( method, patch );
      public static void PrefixReturn ( object type, string method, ScriptObject patch ) => prefixReturn( type, method, patch );
      public static void PrefixReturn ( Type type, string method, ScriptObject patch ) => prefixReturn( type, method, patch );
      public static void PostfixReturn < T > ( string method, ScriptObject patch ) => postfixReturn< T >( method, patch );
      public static void PostfixReturn ( object type, string method, ScriptObject patch ) => postfixReturn( type, method, patch );
      public static void PostfixReturn ( Type type, string method, ScriptObject patch ) => postfixReturn( type, method, patch );

      private static void AddPatch ( Type type, string method, ScriptObject patch, IDictionary< MethodBase, CallbackList > map, string patcher ) {
         string name = patch.GetProperty( "name" )?.ToString();
         ZyMod.Info( "Adding {0} patch {1} to {2}.{3}", patcher.Replace( "Proxy", "" ), string.IsNullOrEmpty( name ) ? "(anonymous)" : name, type, method );
         var target = type?.Method( method );
         if ( target == null ) throw new NullReferenceException( type?.Name + "." + method + " not found" );
         var mod_id = ScriptEngine.Current?.Name ?? "<unknown>";
         lock ( map ) {
            map.TryGetValue( target, out CallbackList list );
            if ( list == null ) map[ target ] = list = new List< KeyValuePair< string, ScriptObject > >();
            list.Add( new KeyValuePair<string, ScriptObject>( mod_id, patch ) );
         }
         var hfunc = new HarmonyMethod( typeof( PatchHelper ).Method( patcher ) );
         if ( patcher.IndexOf( "prefix", StringComparison.InvariantCultureIgnoreCase ) >= 0 )
            Harmony.Patch( target, hfunc );
         else
            Harmony.Patch( target, null, hfunc );
      }

      private static bool PrefixProxy ( object __instance, MethodBase __originalMethod ) => RunProxy( "prefix", Prefixes, __instance, __originalMethod );
      private static void PostfixProxy ( object __instance, MethodBase __originalMethod ) => RunProxy( "postfix", Postfixes, __instance, __originalMethod );
      private static bool PreReturnProxy ( ref object __result, object __instance, MethodBase __originalMethod ) => RunResultProxy( "prefixReturn", PrefixReturns, ref __result, __instance, __originalMethod );
      private static void PostReturnProxy ( ref object __result, object __instance, MethodBase __originalMethod ) => RunResultProxy( "postfixReturn", PostfixReturns, ref __result, __instance, __originalMethod );

      private static bool RunProxy ( string name, IDictionary< MethodBase, CallbackList > map, object instance, MethodBase method )
         => RunPatches( name, map, new PatchEvent( instance, method, name == "prefix", false, null ) );

      private static bool RunResultProxy ( string name, IDictionary< MethodBase, CallbackList > map, ref object result, object instance, MethodBase method ) {
         var evt = new PatchEvent( instance, method, name == "prefixReturn", true, result );
         var ok = RunPatches( name, map, evt );
         result = evt.result;
         return ok;
      }

      private static bool RunPatches ( string name, IDictionary< MethodBase, CallbackList > map, PatchEvent evt ) { try {
         foreach ( var patch in GetPatchList( map, evt.targetMethod ) ) {
            var logArg = new object[]{ name, patch.Key, evt.targetMethod.DeclaringType, evt.targetMethod.Name };
            try {
               ZyMod.Verbo( "Calling {0} patch of {1} on {2}.{3}", logArg );
               var func = patch.Value.InvokeMethod( "bind", new object[]{ evt.instance } ) as ScriptObject ?? patch.Value;
               func.Invoke( false, new object[]{ evt } );
               if ( evt.aborted ) {
                  ZyMod.Info( "Call aborted by {0} patch of {1} on {2}.{3}:", logArg );
                  return false;
               }
            } catch ( Exception ex ) {
               ZyMod.Warn( "Error in {0} patch of {1} on {2}.{3}:", logArg );
               ZyMod.Warn( ex );
            }
         }
         return true;
      } catch  ( Exception ex ) { ZyMod.Warn( ex ); return true; } }

      private static KeyValuePair< string, ScriptObject >[] GetPatchList ( IDictionary< MethodBase, CallbackList > map, MethodBase method ) {
         if ( method == null ) return null;
         lock ( map ) {
            if ( ! map.TryGetValue( method, out CallbackList tmp ) ) return Array.Empty<KeyValuePair< string, ScriptObject >>();
            var list = tmp?.ToArray();
            return list?.Length > 0 ? list : Array.Empty<KeyValuePair< string, ScriptObject >>();
         }
      }

      public static void unpatchAll () {
         var mod_id = ScriptEngine.Current?.Name ?? "<unknown>";
         Unpatch( mod_id, Prefixes, nameof( PrefixProxy ) );
         Unpatch( mod_id, Postfixes, nameof( PostfixProxy ) );
         Unpatch( mod_id, PrefixReturns, nameof( PreReturnProxy ) );
         Unpatch( mod_id, PostfixReturns, nameof( PostReturnProxy ) );
      }
      public static void UnpatchAll () => unpatchAll();

      private static void Unpatch ( string mod_id, IDictionary< MethodBase, CallbackList > patches, string patcher ) {
         foreach ( var method in patches.ToArray() ) {
            foreach ( var patch in method.Value.ToArray() ) {
               if ( patch.Key != mod_id ) continue;
               method.Value.Remove( patch );
               if ( method.Value.Count == 0 ) {
                  patches.Remove( method.Key );
                  Harmony.Unpatch( method.Key, typeof( PatchHelper ).Method( patcher ) );
               }
            }
         }
      }
   }

   public class PatchEvent {
      public readonly object instance;
      public readonly MethodBase targetMethod;
      public readonly bool canAbort;
      public readonly bool hasResult;

      private object _result;
      public object result {
         get { return _result; }
         set {
            if ( ! hasResult ) throw new InvalidOperationException( "Use prefixReturn or postfixReturn to patch return value" );
            _result = value;
         }
      }

      public PatchEvent ( object instance, MethodBase targetMethod, bool canAbort, bool hasResult, object result ) {
         this.instance = instance;
         this.targetMethod = targetMethod;
         this.canAbort = canAbort;
         this.hasResult = hasResult;
         if ( hasResult ) _result = result;
      }

      public bool aborted { get; private set; }
      public void abort() => aborted = canAbort ? false : true;

      public object Instance => instance;
      public MethodBase TargetMethod => targetMethod;
      public bool CanAbort => canAbort;
      public bool HasResult => hasResult;
      public object Result => result;
      public bool Aborted => aborted;
      public void Abort() => abort();
   }

   public static class RepoHelper {
      private static DefRepository _Repo;
      public static DefRepository repo => _Repo ?? ( _Repo = GameUtl.GameComponent< DefRepository >() );
      public static DefRepository Repo => repo;

      public static BaseDef get ( string key ) => Get< BaseDef >( key );
      public static BaseDef Get ( string key ) => get( key );

      public static T get< T > ( string key ) where T : BaseDef => (T) DataCache.API_PP_Def( null, key );
      public static T Get< T > ( string key ) where T : BaseDef => get< T >( key );

      public static IEnumerable< BaseDef > getAll ( object nameOrType ) => DataCache.API_PP_Defs( null, nameOrType );
      public static IEnumerable< BaseDef > GetAll ( object nameOrType ) => getAll( nameOrType );

      public static IEnumerable< T > getAll< T > ( string name = null ) where T : BaseDef => DataCache.API_PP_Defs( null, name ).OfType<T>();
      public static IEnumerable< T > GetAll< T > ( string name = null ) where T : BaseDef => getAll< T >( name );
   }

   public static class DamageHelpers {
      private static SharedDamageKeywordsDataDef DmgType;

      public static DamageKeywordDef Parse ( string type = "normal" ) => parse( type );
      public static DamageKeywordDef parse ( string type = "normal" ) {
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
            case "normal" : case "" : case null :
               return DmgType.DamageKeyword;
         }
         throw new ArgumentException( "Unknown damage type: " + type );
      }

      private static float Modify ( this WeaponDef weapon, DamageKeywordDef damageType, Func<float,float> handler ) { lock ( weapon ) {
         if ( weapon == null ) throw new ArgumentNullException( nameof( weapon ) );
         if ( damageType == null ) throw new ArgumentNullException( nameof( damageType ) );
         var list = weapon.DamagePayload.DamageKeywords;
         var pair = list.Find( e => e.DamageKeywordDef == damageType );
         var orig = pair?.Value ?? 0;
         if ( handler == null ) return orig;
         var value = handler( orig );
         if ( value == orig ) return orig;
         object[] logArg = new object[]{ weapon.name, damageType.name, orig, value };
         if ( value > 0 ) {
            if ( pair == null ) {
               ZyMod.Verbo( "Adding {1} {3} to {0}", logArg );
               list.Add( pair = new DamageKeywordPair { DamageKeywordDef = damageType, Value = value } );
            } else {
               ZyMod.Verbo( "Update {0} {1} {2} to {3}", logArg );
               pair.Value = value;
            }
         } else if ( pair != null ) {
            ZyMod.Verbo( "Removing {1} {2} from {0}", logArg );
            list.Remove( pair );
         }
         return orig;
      } }

      public static float getDamage ( this WeaponDef weapon, string type = "normal" ) => GetDamage( weapon, Parse( type ) );
      public static float GetDamage ( this WeaponDef weapon, string type = "normal" ) => getDamage( weapon, type );

      public static float getDamage ( this WeaponDef weapon, DamageKeywordDef type ) => Modify( weapon, type, null );
      public static float GetDamage ( this WeaponDef weapon, DamageKeywordDef type ) => getDamage( weapon, type );

      public static WeaponDef setDamage ( this WeaponDef weapon, float value ) => SetDamage( weapon, DmgType.DamageKeyword, value );
      public static WeaponDef SetDamage ( this WeaponDef weapon, float value ) => setDamage( weapon, value );

      public static WeaponDef setDamage ( this WeaponDef weapon, string type, float value ) => SetDamage( weapon, Parse( type ), value );
      public static WeaponDef SetDamage ( this WeaponDef weapon, string type, float value ) => setDamage( weapon, type, value );

      public static WeaponDef SetDamage ( this WeaponDef weapon, DamageKeywordDef type, float value ) => setDamage( weapon, type, value );
      public static WeaponDef setDamage ( this WeaponDef weapon, DamageKeywordDef type, float value ) {
         ExchangeDamage( weapon, type, value );
         return weapon;
      }

      public static float addDamage ( this WeaponDef weapon, string type, float value ) => AddDamage( weapon, Parse( type ), value );
      public static float AddDamage ( this WeaponDef weapon, string type, float value ) => addDamage( weapon, type, value );

      public static float addDamage ( this WeaponDef weapon, DamageKeywordDef type, float value ) => Modify( weapon, type, ( orig ) => orig + value ) + value;
      public static float AddDamage ( this WeaponDef weapon, DamageKeywordDef type, float value ) => addDamage( weapon, type, value );

      public static float exchangeDamage ( this WeaponDef weapon, string type, float value ) => ExchangeDamage( weapon, Parse( type ), value );
      public static float ExchangeDamage ( this WeaponDef weapon, string type, float value ) => exchangeDamage( weapon, type, value );

      public static float exchangeDamage ( this WeaponDef weapon, DamageKeywordDef type, float value ) => Modify( weapon, type, ( _ ) => value );
      public static float ExchangeDamage ( this WeaponDef weapon, DamageKeywordDef type, float value ) => exchangeDamage( weapon, type, value );

      public static float compareExchangeDamage ( this WeaponDef weapon, string type, float ifMatch, float setTo ) => CompareExchangeDamage( weapon, Parse( type ), ifMatch, setTo );
      public static float CompareExchangeDamage ( this WeaponDef weapon, string type, float ifMatch, float setTo ) => compareExchangeDamage( weapon, type, ifMatch, setTo );

      public static float compareExchangeDamage ( this WeaponDef weapon, DamageKeywordDef type, float ifMatch, float setTo ) => Modify( weapon, type, ( orig ) => orig == ifMatch ? setTo : orig );
      public static float CompareExchangeDamage ( this WeaponDef weapon, DamageKeywordDef type, float ifMatch, float setTo ) => compareExchangeDamage( weapon, type, ifMatch, setTo );

      public static float Acid ( this WeaponDef weapon ) => acid( weapon );
      public static WeaponDef Acid ( this WeaponDef weapon, float value ) => acid( weapon, value );
      public static float Blast ( this WeaponDef weapon ) => blast( weapon );
      public static WeaponDef Blast ( this WeaponDef weapon, float value ) => blast( weapon, value );
      public static float Bleed ( this WeaponDef weapon ) => bleed( weapon );
      public static WeaponDef Bleed ( this WeaponDef weapon, float value ) => bleed( weapon, value );
      public static float Damage ( this WeaponDef weapon ) => damage( weapon );
      public static WeaponDef Damage ( this WeaponDef weapon, float value ) => damage( weapon, value );
      public static float Fire ( this WeaponDef weapon ) => fire( weapon );
      public static WeaponDef Fire ( this WeaponDef weapon, float value ) => fire( weapon, value );
      public static float Goo ( this WeaponDef weapon ) => goo( weapon );
      public static WeaponDef Goo ( this WeaponDef weapon, float value ) => goo( weapon, value );
      public static float Mist ( this WeaponDef weapon ) => mist( weapon );
      public static WeaponDef Mist ( this WeaponDef weapon, float value ) => mist( weapon, value );
      public static float Paralyze ( this WeaponDef weapon ) => paralyze( weapon );
      public static WeaponDef Paralyze ( this WeaponDef weapon, float value ) => paralyze( weapon, value );
      public static float Pierce ( this WeaponDef weapon ) => pierce( weapon );
      public static WeaponDef Pierce ( this WeaponDef weapon, float value ) => pierce( weapon, value );
      public static float Poison ( this WeaponDef weapon ) => poison( weapon );
      public static WeaponDef Poison ( this WeaponDef weapon, float value ) => poison( weapon, value );
      public static float Psychic ( this WeaponDef weapon ) => psychic( weapon );
      public static WeaponDef Psychic ( this WeaponDef weapon, float value ) => psychic( weapon, value );
      public static float Shock ( this WeaponDef weapon ) => shock( weapon );
      public static WeaponDef Shock ( this WeaponDef weapon, float value ) => shock( weapon, value );
      public static float Shred ( this WeaponDef weapon ) => shred( weapon );
      public static WeaponDef Shred ( this WeaponDef weapon, float value ) => shred( weapon, value );
      public static float Sonic ( this WeaponDef weapon ) => sonic( weapon );
      public static WeaponDef Sonic ( this WeaponDef weapon, float value ) => sonic( weapon, value );
      public static float Vampiric ( this WeaponDef weapon ) => vampiric( weapon );
      public static WeaponDef Vampiric ( this WeaponDef weapon, float value ) => vampiric( weapon, value );
      public static float Viral ( this WeaponDef weapon ) => viral( weapon );
      public static WeaponDef Viral ( this WeaponDef weapon, float value ) => viral( weapon, value );
      public static float Virophage ( this WeaponDef weapon ) => virophage( weapon );
      public static WeaponDef Virophage ( this WeaponDef weapon, float value ) => virophage( weapon, value );

      public static float acid ( this WeaponDef weapon ) => GetDamage( weapon, "acid" );
      public static WeaponDef acid ( this WeaponDef weapon, float value ) => SetDamage( weapon, "acid", value );
      public static float blast ( this WeaponDef weapon ) => GetDamage( weapon, "blast" );
      public static WeaponDef blast ( this WeaponDef weapon, float value ) => SetDamage( weapon, "blast", value );
      public static float bleed ( this WeaponDef weapon ) => GetDamage( weapon, "bleed" );
      public static WeaponDef bleed ( this WeaponDef weapon, float value ) => SetDamage( weapon, "bleed", value );
      public static float damage ( this WeaponDef weapon ) => GetDamage( weapon, "" );
      public static WeaponDef damage ( this WeaponDef weapon, float value ) => SetDamage( weapon, "", value );
      public static float fire ( this WeaponDef weapon ) => GetDamage( weapon, "fire" );
      public static WeaponDef fire ( this WeaponDef weapon, float value ) => SetDamage( weapon, "fire", value );
      public static float goo ( this WeaponDef weapon ) => GetDamage( weapon, "goo" );
      public static WeaponDef goo ( this WeaponDef weapon, float value ) => SetDamage( weapon, "goo", value );
      public static float mist ( this WeaponDef weapon ) => GetDamage( weapon, "mist" );
      public static WeaponDef mist ( this WeaponDef weapon, float value ) => SetDamage( weapon, "mist", value );
      public static float paralyze ( this WeaponDef weapon ) => GetDamage( weapon, "paralyse" );
      public static WeaponDef paralyze ( this WeaponDef weapon, float value ) => SetDamage( weapon, "paralyse", value );
      public static float pierce ( this WeaponDef weapon ) => GetDamage( weapon, "pierce" );
      public static WeaponDef pierce ( this WeaponDef weapon, float value ) => SetDamage( weapon, "pierce", value );
      public static float poison ( this WeaponDef weapon ) => GetDamage( weapon, "poison" );
      public static WeaponDef poison ( this WeaponDef weapon, float value ) => SetDamage( weapon, "poison", value );
      public static float psychic ( this WeaponDef weapon ) => GetDamage( weapon, "psychic" );
      public static WeaponDef psychic ( this WeaponDef weapon, float value ) => SetDamage( weapon, "psychic", value );
      public static float shock ( this WeaponDef weapon ) => GetDamage( weapon, "shock" );
      public static WeaponDef shock ( this WeaponDef weapon, float value ) => SetDamage( weapon, "shock", value );
      public static float shred ( this WeaponDef weapon ) => GetDamage( weapon, "shred" );
      public static WeaponDef shred ( this WeaponDef weapon, float value ) => SetDamage( weapon, "shred", value );
      public static float sonic ( this WeaponDef weapon ) => GetDamage( weapon, "sonic" );
      public static WeaponDef sonic ( this WeaponDef weapon, float value ) => SetDamage( weapon, "sonic", value );
      public static float vampiric ( this WeaponDef weapon ) => GetDamage( weapon, "vampiric" );
      public static WeaponDef vampiric ( this WeaponDef weapon, float value ) => SetDamage( weapon, "vampiric", value );
      public static float viral ( this WeaponDef weapon ) => GetDamage( weapon, "viral" );
      public static WeaponDef viral ( this WeaponDef weapon, float value ) => SetDamage( weapon, "viral", value );
      public static float virophage ( this WeaponDef weapon ) => GetDamage( weapon, "virophage" );
      public static WeaponDef virophage ( this WeaponDef weapon, float value ) => SetDamage( weapon, "virophage", value );
   }
}
