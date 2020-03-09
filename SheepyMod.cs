using Harmony;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
         if ( Patcher == null )
            Patcher = HarmonyInstance.Create( GetType().Namespace );
         Verbo( "Patching {0}.{1}, pre={2} post={3} trans={4}", toPatch.DeclaringType, toPatch.Name, prefix, postfix, transpiler );
         var patch = new PatchRecord{ Mod = this, Target = toPatch, Prefix = ToHarmonyMethod( prefix ), Postfix = ToHarmonyMethod( postfix ), Transpiler = ToHarmonyMethod( transpiler ) };
         Patcher.Patch( toPatch, patch.Prefix, patch.Postfix, patch.Transpiler );
         return patch;
      }

      protected HarmonyMethod ToHarmonyMethod ( string name ) {
         if ( name == null ) return null;
         MethodInfo func = GetType().GetMethod( name );
         if ( func == null ) throw new NullReferenceException( name + " not found" );
         return new HarmonyMethod( func );
      }

      private class PatchRecord : IPatchRecord {
         internal SheepyMod Mod;
         internal MethodBase Target;
         internal HarmonyMethod Prefix;
         internal HarmonyMethod Postfix;
         internal HarmonyMethod Transpiler;
         public void Unpatch () {
            Verbo( "Unpatching {0}, pre={1} post={2} trans={3}", Target, Prefix?.methodName, Postfix?.methodName, Transpiler?.methodName );
            if ( Prefix     != null ) Mod.Patcher.Unpatch( Target, Prefix.method );
            if ( Postfix    != null ) Mod.Patcher.Unpatch( Target, Postfix.method );
            if ( Transpiler != null ) Mod.Patcher.Unpatch( Target, Transpiler.method );
         }
      }

      protected internal static Action< SourceLevels, object, object[] > Logger;
      protected static string LogFile;

      protected void SetLogger ( Action< SourceLevels, object, object[] > logger ) {
         if ( logger == null ) { // Use default
            Logger = DefaultLogger;
            Info( DateTime.Now.ToString( "D" ) + " " + GetType().Namespace + " " + Assembly.GetExecutingAssembly().GetName().Version );
            LogFile = Regex.Replace( Assembly.GetExecutingAssembly().Location, "\\.dll$", ".log", RegexOptions.IgnoreCase );
         } else
            Logger = logger; // Logger provided by mod loader
      }

      // A simple file logger when one is not provided by the mod loader.
      private void DefaultLogger ( SourceLevels lv, object msg, object[] param ) { try {
         string line = msg?.ToString();
         try {
            if ( param != null ) line = string.Format( line, param );
         } catch ( FormatException ) { }
         using ( var stream = File.AppendText( LogFile ) ) {
            stream.WriteLineAsync( DateTime.Now.ToString( "T" ) + " " + GetType().Namespace + " " + line );
         }  
      } catch ( Exception ex ) { Console.WriteLine( ex ); } }

      protected internal static void Verbo ( object msg, params object[] augs ) => Logger?.Invoke( SourceLevels.Verbose, msg, augs );
      protected internal static void Info  ( object msg, params object[] augs ) => Logger?.Invoke( SourceLevels.Information, msg, augs );
      protected internal static void Warn  ( object msg, params object[] augs ) => Logger?.Invoke( SourceLevels.Warning, msg, augs );
      protected internal static bool Error ( object msg, params object[] augs ) {
         Logger?.Invoke( SourceLevels.Error, msg, augs );
         return true;
      }
   }

   public interface IPatchRecord {
      void Unpatch();
   }
}
