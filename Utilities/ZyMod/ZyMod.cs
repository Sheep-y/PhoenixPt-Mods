#if DEBUG
#define ZyBatch
#define ZyConfig
#define ZyDefLog
#define ZyLang
#define ZyLib
#define ZyYield
#endif

#if ZyBatch
#define ZyUnpatch
#endif

#if ZyDefLog
#define ZyLog
#endif

using System;
using System.Reflection;

#if ! ZyNoPatch || ZyYield
using Harmony;
using static System.Reflection.BindingFlags;
#endif

#if ZyConfig
using Newtonsoft.Json;
using System.IO;
#endif

#if ( ZyBatch || ZyYield )
using System.Collections.Generic;
#endif

#if ZyLang
using I2.Loc;
using System.Globalization;
#endif

#if ZyLog
using System.Diagnostics;
#endif

#if ZyLib
using System.Linq;
#endif

namespace Sheepy.PhoenixPt {

   /// <summary>
   /// A light base mod class that supports manual patching and barebone API.
   /// Use build symbols to enable optional features:
   ///   * ZyBatch   - Patch transactions.  Implies ZyUnpatch.
   ///   * ZyConfig  - Json configuration.
   ///   * ZyDefLog  - Logging with default logger.  Implies ZyLog. (static)
   ///   * ZyLang    - Language related helpers, such as TitleCase. (static)
   ///   * ZyLib     - Assembly related helpers, such as GameAssembly. (static)
   ///   * ZyLog     - Logging shortcuts.
   ///   * ZyUnpatch - Allow patches to be unpatched.
   ///   * ZyYield   - Yield patch related helpers.
   ///
   /// Alternatively, use this flag to remove even barebone features:
   ///   * ZyNoPatch - Remove patch code, leaving only API.
   ///                 ZyBatch and ZyUnpatch will not take effect.
   /// </summary>
   public abstract class ZyMod {

      protected static object _SLock = new object();
      protected object _ILock = new object();

#if ! ZyNoPatch
      protected internal HarmonyInstance Patcher;

      private MethodInfo GetPatchSubject ( Type type, string method ) {
         #if ZyBatch
         try {
         #endif
            try {
               var result = type.GetMethod( method, Public | NonPublic | Instance | Static );
               if ( result == null ) throw new ApplicationException( $"Not found: {type}.{method}" );
               return result;
            } catch ( AmbiguousMatchException ex ) { throw new ApplicationException( $"Multiple: {type}.{method}", ex ); }
         #if ZyBatch
         } catch ( Exception ex ) { RollbackPatch( ex ); throw; }
         #endif
      }

      protected IPatch Patch ( Type type, string method, string prefix = null, string postfix = null, string transpiler = null ) {
         #if ZyBatch
         try {
         #endif
            return Patch( GetPatchSubject( type, method ), prefix, postfix, transpiler );
         #if ZyBatch
         } catch ( Exception ex ) { RollbackPatch( ex ); throw; }
         #endif
      }

      protected IPatch Patch ( MethodBase method, string prefix = null, string postfix = null, string transpiler = null ) {
         IPatch patch;
         #if ZyBatch
         try {
         #endif
            lock ( _ILock ) if ( Patcher == null ) Patcher = HarmonyInstance.Create( GetType().Namespace );
            patch = new PatchRecord( Patcher, method, ToHarmony( prefix ), ToHarmony( postfix ), ToHarmony( transpiler ) ).Patch();
         #if ZyBatch
         } catch ( Exception ex ) { RollbackPatch( ex ); throw; }
         lock ( Trans ) if ( TransId != null ) Trans.Add( patch );
         #endif
         return patch;
      }

      protected IPatch TryPatch ( Type type, string method, string prefix = null, string postfix = null, string transpiler = null ) { try {
         #if ZyBatch
         lock ( Trans ) NoRollback = true;
         #endif
         return Patch( type, method, prefix, postfix, transpiler );
      } catch ( Exception ex ) {
         #if ZyLog
         Warn( ex );
         #else
         Api( "log w", ex );
         #endif
         return null;
      #if ZyBatch
      } finally {
         lock ( Trans ) NoRollback = false;
      #endif
      } }

      protected IPatch TryPatch ( MethodBase method, string prefix = null, string postfix = null, string transpiler = null ) { try {
         #if ZyBatch
         lock ( Trans ) NoRollback = true;
         #endif
         return Patch( method, prefix, postfix, transpiler );
      } catch ( Exception ex ) {
         #if ZyLog
         Warn( ex );
         #else
         Api( "log w", ex );
         #endif
         return null;
      #if ZyBatch
      } finally {
         lock ( Trans ) NoRollback = false;
      #endif
      } }

      private HarmonyMethod ToHarmony ( string name ) {
         if ( name == null ) return null;
         return new HarmonyMethod( GetType().GetMethod( name, Public | NonPublic | Static ) ?? throw new NullReferenceException( name + " not found" ) );
      }

      #if ZyUnpatch
      protected static void Unpatch ( ref IPatch patch ) {
         patch?.Unpatch();
         patch = null;
      }
      #endif

      #if ZyBatch
      private string TransId = null;
      private readonly List< IPatch > Trans = new List<IPatch>();
      private bool NoRollback = false;

      protected bool StartPatch ( string id = "patches" ) { lock( Trans ) {
         if ( TransId != null ) {
            #if ZyLog
            Warn( "Cannot start {0}, already in {1}", id, TransId );
            #else
            Api( "log w", $"Cannot start {id}, already in {TransId}" );
            #endif
            return false;
         }
         id = id ?? "null";
         TransId = id;
         #if ZyLog
         Verbo( "Start {0}", id );
         #else
         Api( "log v", "Start " + id );
         #endif
         return true;
      } }

      protected IPatch[] RollbackPatch ( object reason = null ) { lock( Trans ) {
         if ( NoRollback || TransId == null ) return null;
         #if ZyLog
         Warn( "Rollback {0} ({1} patches) {2}", TransId, Trans.Count, reason );
         #else
         Api( "log w", $"Rollback {TransId} ({Trans.Count} patches) {reason}" );
         #endif

         var result = Trans.ToArray();
         foreach ( var patch in Trans )
            patch.Unpatch();
         Trans.Clear();
         TransId = null;
         return result;
      } }

      protected IPatch[] CommitPatch () { lock( Trans ) {
         if ( TransId == null ) return null;
         #if ZyLog
         Info( "Commit {0} ({1} patches)", TransId, Trans.Count );
         #else
         Api( "log", $"Commit {TransId} ({Trans.Count} patches)" );
         #endif

         var result = Trans.ToArray();
         Trans.Clear();
         TransId = null;
         return result;
      } }

      protected IPatch[] BatchPatch ( Action patcher ) => BatchPatch( "patches", patcher );

      protected IPatch[] BatchPatch ( string id, Action patcher ) {
         StartPatch( id );
         try {
            patcher();
            return CommitPatch();
         } catch ( Exception ex ) {
            RollbackPatch( ex );
            return null;
         }
      }
      #endif

      private class PatchRecord : IPatch {
         private readonly HarmonyInstance Patcher;
         private readonly MethodBase Target;
         private readonly HarmonyMethod Pre;
         private readonly HarmonyMethod Post;
         private readonly HarmonyMethod Tran;

         internal PatchRecord ( HarmonyInstance patcher, MethodBase target, HarmonyMethod pre, HarmonyMethod post, HarmonyMethod tran ) {
            Patcher = patcher;
            Target = target;
            Pre = pre;
            Post = post;
            Tran = tran;
         }

         public IPatch Patch () {
            #if ZyLog
            Verbo( (Func<string>) PatchLog );
            #else
            Api( "log v", (Func<string>) PatchLog );
            #endif
            Patcher.Patch( Target, Pre, Post, Tran );
            return this;
         }
         private string PatchLog () => ActionLog( "Patching" );
         private string ActionLog ( string action ) => string.Format( "{0} {1}.{2}, pre:{3} post:{4} trans:{5}", action, Target.DeclaringType.Name, Target.Name, Pre?.method.Name, Post?.method.Name, Tran?.method.Name );

         #if ZyUnpatch
         public void Unpatch () {
            #if ZyLog
            Verbo( (Func<string>) UnpatchLog );
            #else
            Api( "log v", (Func<string>) UnpatchLog );
            #endif
            if ( Pre  != null ) Patcher.Unpatch( Target, Pre.method  );
            if ( Post != null ) Patcher.Unpatch( Target, Post.method );
            if ( Tran != null ) Patcher.Unpatch( Target, Tran.method );
         }
         private string UnpatchLog () => ActionLog( "Unpatching" );
         #endif
      }
#endif

      private static Func< string, object, object > ModnixApi;

      public static bool HasApi { get { lock ( _SLock ) return ModnixApi != null; } }

      public static object Api ( string action, object param = null ) {
         Func< string, object, object > api;
         lock ( _SLock ) api = ModnixApi;
         return api?.Invoke( action, param );
      }

      public static void SetApi ( Func< string, object, object > api ) {
         if ( api == null ) {
            #if ZyDefLog
            var asm = Assembly.GetExecutingAssembly();
            lock ( _SLock ) {
               LogFile = asm.Location.Replace( ".dll", ".log" );
               Logger = DefaultLogger;
            }
            Info( "{0} {1} {2}", DateTime.Now.ToString( "D" ), LogFile, asm.GetName().Version );
            #endif
            return;
         }
         lock ( _SLock ) ModnixApi = api;
         #if ZyLog
         Logger = (Action< TraceEventType, object, object[] >) api( "logger", "TraceEventType" );
         #endif
      }

      #if ZyConfig
      public static T SetApi < T > ( Func< string, object, object > api, out T config ) where T : new() {
         config = default;
         SetApi( api );
         if ( api == null ) {
            var file = Assembly.GetExecutingAssembly().Location.Replace( ".dll", ".conf" );
            if ( File.Exists( file ) ) try {
               config = JsonConvert.DeserializeObject<T>( File.ReadAllText( file ) );
            #if ZyDefLog
            } catch ( Exception ex ) {
               Warn( ex );
            #else
            } catch ( Exception ) {
            #endif
            }
         } else {
            config = (T) api( "config", typeof( T ) );
         }
         if ( config == null ) config = new T();
         Api( "log v", Jsonify( config ) );
         return config;
      }

      private static Func<string> Jsonify ( object obj ) => () => "Config = " + JsonConvert.SerializeObject( obj );
      #endif

      #if ZyLang
      public static TextInfo CurrentLang => new CultureInfo( LocalizationManager.CurrentLanguageCode ).TextInfo;
      public static string TitleCase ( string txt ) {
         if ( string.IsNullOrWhiteSpace( txt ) ) return txt;
         var lang = CurrentLang;
         return lang.ToTitleCase( lang.ToLower( txt ) );
      }
      #endif

      #if ZyLib
      public static Assembly GameAssembly => Api( "assembly", "game" ) as Assembly ?? FindAssembly( "Assembly-CSharp" );
      public static Assembly UnityCore => FindAssembly( "UnityEngine.CoreModule" );

      public static Assembly FindAssembly ( string name ) {
         if ( ! name.EndsWith( "," ) ) name += ",";
         return Array.Find( AppDomain.CurrentDomain.GetAssemblies(), e => e.FullName.StartsWith( name, StringComparison.OrdinalIgnoreCase ) );
      }
      #endif

      #if ZyYield
      public static MethodInfo FindYieldMethod ( Type type, string methodName ) {
         if ( type == null ) throw new ArgumentNullException( nameof( type ) );
         var findName = "<" + methodName + ">";
         // Find compiler generated classes
         foreach ( var t in type.GetNestedTypes( NonPublic | Static ) ) {
            if ( ! t.Name.StartsWith( "<>c", StringComparison.OrdinalIgnoreCase ) ) continue;
            // And among them find the first generated method
            foreach ( var m in t.GetMethods( NonPublic | Instance | Static ) ) {
               if ( m.Name.StartsWith( findName, StringComparison.Ordinal ) ) return m;
            }
         }
         throw new MissingMemberException( type.FullName + "." + methodName );
      }

      public static MethodInfo TryFindYield ( Type type, string methodName, string errorLevel = null ) { try {
         return FindYieldMethod( type, methodName );
      } catch {
         if ( errorLevel != null )
            Api( "log " + errorLevel, "Yield not found: " + ( type?.FullName ?? "null" ) + "." + methodName );
         return null;
      } }

      protected static IEnumerable<CodeInstruction> DumpILCode ( IEnumerable<CodeInstruction> instr ) {
         foreach ( var code in instr ) Info( code );
         return instr;
      }
      #endif

      #if ZyLog
      public static Action< TraceEventType, object, object[] > Logger;
      public static void ApiLog ( TraceEventType level, object msg, params object[] args ) { lock ( _SLock ) Logger?.Invoke( level, msg, args ); }
      public static void Trace ( object msg, params object[] args ) => ApiLog( TraceEventType.Transfer, msg, args );
      public static void Verbo ( object msg, params object[] args ) => ApiLog( TraceEventType.Verbose, msg, args );
      public static void Info  ( object msg, params object[] args ) => ApiLog( TraceEventType.Information, msg, args );
      public static void Warn  ( object msg, params object[] args ) => ApiLog( TraceEventType.Warning, msg, args );
      public static bool Error ( object msg, params object[] args ) {
         ApiLog( TraceEventType.Error, msg, args );
         return true;
      }
      #endif

      #if ZyDefLog
      protected static string LogFile;

      // A simple file logger when one is not provided by the mod loader.
      private static void DefaultLogger ( TraceEventType lv, object msg, object[] param ) { try {
         string line = msg?.ToString();
         try {
            if ( param != null ) line = string.Format( line, param );
         } catch ( FormatException ) { }
         using ( var stream = File.AppendText( LogFile ) ) {
            stream.WriteLineAsync( DateTime.Now.ToString( "T" ) + " " + line );
         }
      } catch ( Exception ex ) { Console.WriteLine( ex ); } }
      #endif
   }

   #if ! ZyNoPatch
   public interface IPatch {
      IPatch Patch();
      #if ZyUnpatch
      void Unpatch();
      #endif
   }
   #endif
}