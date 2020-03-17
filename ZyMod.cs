using Harmony;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Reflection.BindingFlags;

namespace Sheepy.PhoenixPt {

   /// <summary>
   /// Base mod class to supports manual patching and unpatch, config parsing, and logging shortcuts.
   /// Subclass must provide a logger for logging to functional.
   /// </summary>
   public class ZyMod {

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
         return new PatchRecord { Patcher = Patcher, Target = method,
            Pre = _ToHarmony( prefix ), Post = _ToHarmony( postfix ), Tran = _ToHarmony( transpiler ) }.Patch();
      }

      protected static void Unpatch ( ref IPatch patch ) {
         if ( patch == null ) return;
         patch.Unpatch();
         patch = null;
      }

      protected HarmonyMethod _ToHarmony ( string name ) {
         if ( name == null ) return null;
         return new HarmonyMethod( GetType().GetMethod( name ) ?? throw new NullReferenceException( name + " not found" ) );
      }

      private class PatchRecord : IPatch {
         internal HarmonyInstance Patcher;
         internal MethodBase Target;
         internal HarmonyMethod Pre;
         internal HarmonyMethod Post;
         internal HarmonyMethod Tran;
         public IPatch Patch () {
            Verbo( "Patching {0}, pre={2} post={3} trans={4}", Target, Pre, Post, Tran );
            Patcher.Patch( Target, Pre, Post, Tran );
            return this;
         }
         public void Unpatch () {
            Verbo( "Unpatching {0}, pre={1} post={2} trans={3}", Target, Pre, Post, Tran );
            if ( Pre  != null ) Patcher.Unpatch( Target, Pre.method );
            if ( Post != null ) Patcher.Unpatch( Target, Post.method );
            if ( Tran != null ) Patcher.Unpatch( Target, Tran.method );
         }
      }

      protected static T ReadSettings < T > ( T supplied ) where T : class, new() {
         if ( supplied != null ) return supplied;
         try {
            var file = Assembly.GetExecutingAssembly().Location.Replace( ".dll", ".conf" );
            if ( File.Exists( file ) ) return JsonConvert.DeserializeObject<T>( File.ReadAllText( file ) );
         } catch ( Exception ex ) { Warn( ex ); }
         return new T();
      }

      protected internal static Action< SourceLevels, object, object[] > Logger;
      protected virtual void SetLogger ( Action< SourceLevels, object, object[] > logger ) { lock ( _Lock ) Logger = logger; }
      private static void Log ( SourceLevels level, object msg, object[] augs ) { lock ( _Lock ) Logger?.Invoke( level, msg, augs ); }
      protected internal static void Verbo ( object msg, params object[] augs ) => Log( SourceLevels.Verbose, msg, augs );
      protected internal static void Info  ( object msg, params object[] augs ) => Log( SourceLevels.Information, msg, augs );
      protected internal static void Warn  ( object msg, params object[] augs ) => Log( SourceLevels.Warning, msg, augs );
      protected internal static bool Error ( object msg, params object[] augs ) {
         Log( SourceLevels.Error, msg, augs );
         return true;
      }
   }

   public interface IPatch {
      IPatch Patch();
      void Unpatch();
   }
}