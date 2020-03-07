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
   public class SheepyMod {

      protected internal HarmonyInstance Patcher;

      protected internal IPatchRecord Patch ( Type target, string toPatch, string prefix = null, string postfix = null ) {
         return Patch( target.GetMethod( toPatch, Public | NonPublic | Instance | Static ), prefix, postfix );
      }

      protected internal IPatchRecord Patch ( MethodInfo toPatch, string prefix = null, string postfix = null ) {
         if ( Patcher == null )
            Patcher = HarmonyInstance.Create( GetType().Namespace );
         Info( "Patching {0}.{1}, pre={2} post={3}", toPatch.DeclaringType, toPatch.Name, prefix, postfix );
         var patch = new PatchRecord{ Mod = this, Target = toPatch, Prefix = ToHarmonyMethod( prefix ), Postfix = ToHarmonyMethod( postfix ) };
         Patcher.Patch( toPatch, patch.Prefix, patch.Postfix );
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
         public void Unpatch () {
            Info( "Unpatching {0}, pre={1} post={2}", Target, Prefix?.methodName, Postfix?.methodName );
            if ( Prefix  != null ) Mod.Patcher.Unpatch( Target, Prefix.method );
            if ( Postfix != null ) Mod.Patcher.Unpatch( Target, Postfix.method );
         }
      }

      protected static Action< SourceLevels, object, object[] > Logger;
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
      protected void DefaultLogger ( SourceLevels lv, object msg, object[] param ) { try {
         string line = msg?.ToString();
         try {
            if ( param != null ) line = string.Format( line, param );
         } catch ( FormatException ) { }
         using ( var stream = File.AppendText( LogFile ) ) {
            stream.WriteLineAsync( DateTime.Now.ToString( "T" ) + " " + GetType().Namespace + " " + line );
         }  
      } catch ( Exception ex ) { Console.WriteLine( ex ); } }

      protected static void Verbo ( object msg, params object[] augs ) => Logger?.Invoke( SourceLevels.Verbose, msg, augs );
      protected static void Info  ( object msg, params object[] augs ) => Logger?.Invoke( SourceLevels.Information, msg, augs );
      protected static void Warn  ( object msg, params object[] augs ) => Logger?.Invoke( SourceLevels.Warning, msg, augs );
      protected static bool Error ( object msg, params object[] augs ) {
         Logger?.Invoke( SourceLevels.Error, msg, augs );
         return true;
      }
   }

   public interface IPatchRecord {
      void Unpatch();
   }
}
