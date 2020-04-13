using Harmony;
using I2.Loc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Reflection.BindingFlags;

namespace Sheepy.PhoenixPt {

   /// <summary>
   /// Base mod class to supports manual patching and unpatch, config parsing, and logging shortcuts.
   /// Subclass must provide a logger for non-Modnix logging.
   /// </summary>
   public abstract class ZyMod {

      protected static object _Lock = new object();

      protected static internal HarmonyInstance Patcher;

      protected virtual MethodInfo _GetPatchSubject ( Type type, string method ) { try {
         var result = type.GetMethod( method, Public | NonPublic | Instance | Static );
         if ( result == null ) throw new ApplicationException( $"Not found: {type}.{method}" );
         return result;
      } catch ( AmbiguousMatchException ex ) { throw new ApplicationException( $"Multiple: {type}.{method}", ex ); } }

      protected IPatch Patch ( Type type, string method, string prefix = null, string postfix = null, string transpiler = null ) {
         return Patch( _GetPatchSubject( type, method ), prefix, postfix, transpiler );
      }

      protected virtual IPatch Patch ( MethodInfo method, string prefix = null, string postfix = null, string transpiler = null ) {
         lock ( _Lock ) if ( Patcher == null ) Patcher = HarmonyInstance.Create( GetType().Namespace );
         return new PatchRecord( Patcher, method, _ToHarmony( prefix ), _ToHarmony( postfix ), _ToHarmony( transpiler ) ).Patch();
      }

      protected virtual IPatch TryPatch ( Type type, string method, string prefix = null, string postfix = null, string transpiler = null ) { try {
         return Patch( type, method, prefix, postfix, transpiler );
      } catch ( Exception ex ) {
         Warn( ex );
         return null;
      } }

      protected virtual IPatch TryPatch ( MethodInfo method, string prefix = null, string postfix = null, string transpiler = null ) { try {
         return Patch( method, prefix, postfix, transpiler );
      } catch ( Exception ex ) {
         Warn( ex );
         return null;
      } }

      protected static void Unpatch ( ref IPatch patch ) {
         patch?.Unpatch();
         patch = null;
      }

      protected HarmonyMethod _ToHarmony ( string name ) {
         if ( name == null ) return null;
         return new HarmonyMethod( GetType().GetMethod( name, Public | NonPublic | Static ) ?? throw new NullReferenceException( name + " not found" ) );
      }

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
            Verbo( "Patching {0}.{1}, pre:{2} post:{3} trans:{4}", Target.DeclaringType.Name, Target.Name, Pre?.method.Name, Post?.method.Name, Tran?.method.Name );
            Patcher.Patch( Target, Pre, Post, Tran );
            return this;
         }
         public void Unpatch () {
            Verbo( "Unpatching {0}.{1}, pre:{2} post:{3} trans:{4}", Target.DeclaringType.Name, Target.Name, Pre?.method.Name, Post?.method.Name, Tran?.method.Name );
            if ( Pre  != null ) Patcher.Unpatch( Target, Pre.method  );
            if ( Post != null ) Patcher.Unpatch( Target, Post.method );
            if ( Tran != null ) Patcher.Unpatch( Target, Tran.method );
         }
      }

      protected internal static Func< string, object, object > ModnixApi;

      protected virtual void SetApi ( Func< string, object, object > api ) {
         if ( api == null ) return;
         lock ( _Lock ) {
            ModnixApi = api;
            Logger = (Action< TraceEventType, object, object[] >) api( "logger", "TraceEventType" );
         }
      }

      protected virtual T SetApi < T > ( Func< string, object, object > api, out T config ) where T : new() {
         config = default;
         if ( api == null ) {
            var file = Assembly.GetExecutingAssembly().Location.Replace( ".dll", ".conf" );
            if ( File.Exists( file ) ) try {
               config = JsonConvert.DeserializeObject<T>( File.ReadAllText( file ) );
            } catch ( Exception ex ) { Warn( ex ); }
         } else {
            SetApi( api );
            config = (T) api( "config", typeof( T ) );
         }
         lock ( _Lock ) if ( config == null ) config = new T();
         Verbo( "Config = {0}", Jsonify( config ) );
         return config;
      }

      private static Func<string> Jsonify ( object obj ) => () => JsonConvert.SerializeObject( obj );
      
      protected internal static TextInfo CurrentLang => new CultureInfo( LocalizationManager.CurrentLanguageCode ).TextInfo;
      protected internal static string TitleCase ( string txt ) {
         var lang = CurrentLang;
         return lang.ToTitleCase( lang.ToLower( txt ) );
      }

      protected internal static Action< TraceEventType, object, object[] > Logger;
      private static void Log ( TraceEventType level, object msg, object[] augs ) { lock ( _Lock ) Logger?.Invoke( level, msg, augs ); }
      protected internal static void Verbo ( object msg, params object[] augs ) => Log( TraceEventType.Verbose, msg, augs );
      protected internal static void Info  ( object msg, params object[] augs ) => Log( TraceEventType.Information, msg, augs );
      protected internal static void Warn  ( object msg, params object[] augs ) => Log( TraceEventType.Warning, msg, augs );
      protected internal static bool Error ( object msg, params object[] augs ) {
         Log( TraceEventType.Error, msg, augs );
         return true;
      }
   }

   public interface IPatch {
      IPatch Patch();
      void Unpatch();
   }
}