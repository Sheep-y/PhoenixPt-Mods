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
   /// Advanced base mod class with patch transaction and default logger.
   /// </summary>
   public class ZyAdvMod : ZyMod {

      protected IPatch TryPatch ( Type type, string method, string prefix = null, string postfix = null, string transpiler = null ) { try {
         return base.Patch( type, method, prefix, postfix, transpiler );
      } catch ( Exception ex ) {
         return null;
      } }

      protected virtual IPatch TryPatch ( MethodInfo method, string prefix = null, string postfix = null, string transpiler = null ) { try {
         return base.Patch( method, prefix, postfix, transpiler );
      } catch ( Exception ex ) {
         return null;
      } }

      private string TransId = null;
      private List< IPatch > Trans = new List<IPatch>();

      protected override MethodInfo _GetPatchSubject ( Type target, string method ) { try {
         return base._GetPatchSubject( target, method );
      } catch ( Exception ex ) { RollbackPatch( ex ); throw; } }

      protected override IPatch Patch ( MethodInfo toPatch, string prefix = null, string postfix = null, string transpiler = null ) { try {
         var patch = base.Patch( toPatch, prefix, postfix, transpiler );
         lock ( Trans ) if ( TransId != null ) Trans.Add( patch );
         return patch;
      } catch ( Exception ex ) { RollbackPatch( ex ); throw; } }

      protected void StartPatch ( string id = "patches" ) { lock( Trans ) {
         if ( TransId != null ) return;
         id = id ?? "null";
         Verbo( "Start {0}", id );
         TransId = id;
      } }

      protected void RollbackPatch ( object reason = null ) { lock( Trans ) {
         if ( TransId == null ) return;
         Warn( "Rollback {0} ({1} patches) {2}", TransId, Trans.Count, reason ?? "" );
         foreach ( var patch in Trans )
            patch.Unpatch();
         Trans.Clear();
         TransId = null;
      } }

      protected void CommitPatch () { lock( Trans ) {
         if ( TransId == null ) return;
         Info( "Commit {0} ({1} patches)", TransId, Trans.Count );
         Trans.Clear();
         TransId = null;
      } }

      protected Exception BatchPatch ( Action patcher ) => BatchPatch( "patches", patcher );

      protected Exception BatchPatch ( string id, Action patcher ) {
         StartPatch( id );
         try {
            patcher();
            CommitPatch();
            return null;
         } catch ( Exception ex ) {
            RollbackPatch( ex );
            return ex;
         }
      }

      protected static string LogFile;

      protected override void SetLogger ( Action< SourceLevels, object, object[] > logger ) {
         if ( logger == null ) {
            var asm = Assembly.GetExecutingAssembly();
            LogFile = asm.Location.Replace( ".dll", ".log" );
            base.SetLogger( DefaultLogger );
            Info( DateTime.Now.ToString( "D" ) + " " + GetType().Namespace + " " + asm.GetName().Version );
         } else
            base.SetLogger( logger );
      }

      // A simple file logger when one is not provided by the mod loader.
      private static void DefaultLogger ( SourceLevels lv, object msg, object[] param ) { try {
         string line = msg?.ToString();
         try {
            if ( param != null ) line = string.Format( line, param );
         } catch ( FormatException ) { }
         using ( var stream = File.AppendText( LogFile ) ) {
            stream.WriteLineAsync( DateTime.Now.ToString( "T" ) + " " + line );
         }
      } catch ( Exception ex ) { Console.WriteLine( ex ); } }

   }
}
