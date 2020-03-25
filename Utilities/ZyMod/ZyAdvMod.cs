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
   public abstract class ZyAdvMod : ZyMod {

      private string TransId = null;
      private List< IPatch > Trans = new List<IPatch>();
      private bool NoRollback = false;

      protected override MethodInfo _GetPatchSubject ( Type target, string method ) { try {
         return base._GetPatchSubject( target, method );
      } catch ( Exception ex ) { RollbackPatch( ex ); throw; } }

      protected override IPatch Patch ( MethodInfo toPatch, string prefix = null, string postfix = null, string transpiler = null ) { try {
         var patch = base.Patch( toPatch, prefix, postfix, transpiler );
         lock ( Trans ) if ( TransId != null ) Trans.Add( patch );
         return patch;
      } catch ( Exception ex ) { RollbackPatch( ex ); throw; } }

      protected override IPatch TryPatch ( Type type, string method, string prefix = null, string postfix = null, string transpiler = null ) {
         lock ( Trans ) NoRollback = true;
         var result = base.TryPatch( type, method, prefix, postfix, transpiler );
         lock ( Trans ) NoRollback = false;
         return result;
      }

      protected override IPatch TryPatch ( MethodInfo method, string prefix = null, string postfix = null, string transpiler = null ) {
         lock ( Trans ) NoRollback = true;
         var result = base.TryPatch( method, prefix, postfix, transpiler );
         lock ( Trans ) NoRollback = false;
         return result;
      }

      protected bool StartPatch ( string id = "patches" ) { lock( Trans ) {
         if ( TransId != null ) {
            Warn( "Cannot start {0}, already in {1}", id, TransId );
            return false;
         }
         id = id ?? "null";
         TransId = id;
         Verbo( "Start {0}", id );
         return true;
      } }

      protected IPatch[] RollbackPatch ( object reason = null ) { lock( Trans ) {
         if ( NoRollback || TransId == null ) return null;
         Warn( "Rollback {0} ({1} patches) {2}", TransId, Trans.Count, reason ?? "" );
         var result = Trans.ToArray();
         foreach ( var patch in Trans )
            patch.Unpatch();
         Trans.Clear();
         TransId = null;
         return result;
      } }

      protected IPatch[] CommitPatch () { lock( Trans ) {
         if ( TransId == null ) return null;
         var result = Trans.ToArray();
         Info( "Commit {0} ({1} patches)", TransId, Trans.Count );
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

      protected static string LogFile;

      protected override void SetApi ( Func< string, object, object > api ) {
         base.SetApi( api );
         if ( api == null ) {
            var asm = Assembly.GetExecutingAssembly();
            lock ( _Lock ) {
               LogFile = asm.Location.Replace( ".dll", ".log" );
               Logger = DefaultLogger;
            }
            Info( DateTime.Now.ToString( "D" ) + " " + GetType().Namespace + " " + asm.GetName().Version );
         }
      }

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

   }
}
