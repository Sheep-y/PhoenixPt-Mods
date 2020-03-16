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
   /// Base mod class to supports manual patching and unpatch, default logger, and logging.
   /// </summary>
   public class SheepyMod {

      protected internal HarmonyInstance Patcher;

      protected IPatchRecord Patch ( Type target, string toPatch, string prefix = null, string postfix = null, string transpiler = null ) {
         return Patch( target.GetMethod( toPatch, Public | NonPublic | Instance | Static ), prefix, postfix, transpiler );
      }

      protected IPatchRecord Patch ( MethodInfo toPatch, string prefix = null, string postfix = null, string transpiler = null ) {
         if ( Patcher == null ) Patcher = HarmonyInstance.Create( GetType().Namespace );
         Verbo( "Patching {0}.{1}, pre={2} post={3} trans={4}", toPatch.DeclaringType, toPatch.Name, prefix, postfix, transpiler );
         var patch = new PatchRecord{ Patcher = Patcher, Target = toPatch, Prefix = ToHarmonyMethod( prefix ), Postfix = ToHarmonyMethod( postfix ), Transpiler = ToHarmonyMethod( transpiler ) };
         Patcher.Patch( toPatch, patch.Prefix, patch.Postfix, patch.Transpiler );
         return patch;
      }

      protected HarmonyMethod ToHarmonyMethod ( string name ) {
         if ( name == null ) return null;
         return new HarmonyMethod( GetType().GetMethod( name ) ?? throw new NullReferenceException( name + " not found" ) );
      }

      private class PatchRecord : IPatchRecord {
         internal HarmonyInstance Patcher;
         internal MethodBase Target;
         internal HarmonyMethod Prefix;
         internal HarmonyMethod Postfix;
         internal HarmonyMethod Transpiler;
         public void Unpatch () {
            Verbo( "Unpatching {0}, pre={1} post={2} trans={3}", Target, Prefix, Postfix, Transpiler );
            if ( Prefix     != null ) Patcher.Unpatch( Target, Prefix.method );
            if ( Postfix    != null ) Patcher.Unpatch( Target, Postfix.method );
            if ( Transpiler != null ) Patcher.Unpatch( Target, Transpiler.method );
         }
      }

      protected static readonly string AssemblyLocation = Assembly.GetExecutingAssembly().Location;

      protected static T ReadSettings < T > ( T supplied ) where T : class, new() {
         if ( supplied != null ) return supplied;
         try {
            var file = AssemblyLocation.Replace( ".dll", ".conf" );
            if ( File.Exists( file ) ) return JsonConvert.DeserializeObject<T>( File.ReadAllText( file ) );
         } catch ( Exception ex ) { Warn( ex ); }
         return new T();
      }

      protected internal static Action< SourceLevels, object, object[] > Logger;
      protected void SetLogger ( Action< SourceLevels, object, object[] > logger ) { lock ( AssemblyLocation ) Logger = logger; }
      private static void Log ( SourceLevels level, object msg, object[] augs ) { lock ( AssemblyLocation ) Logger?.Invoke( level, msg, augs ); }
      protected internal static void Verbo ( object msg, params object[] augs ) => Log( SourceLevels.Verbose, msg, augs );
      protected internal static void Info  ( object msg, params object[] augs ) => Log( SourceLevels.Information, msg, augs );
      protected internal static void Warn  ( object msg, params object[] augs ) => Log( SourceLevels.Warning, msg, augs );
      protected internal static bool Error ( object msg, params object[] augs ) {
         Log( SourceLevels.Error, msg, augs );
         return true;
      }
   }

   public interface IPatchRecord {
      void Unpatch();
   }
}
