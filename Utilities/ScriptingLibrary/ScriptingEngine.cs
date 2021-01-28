using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sheepy.PhoenixPt.ScriptingLibrary {

   internal class ScriptingEngine : ZyMod {
      private static readonly Dictionary< string, object > Sessions = new Dictionary< string, object >(); // string, V8ScriptEngine

      public static object Eval ( string id, string code ) { try {
         object state; // Make sure Mod will not crash when V8 cannot be loaded.
         lock ( Sessions ) Sessions.TryGetValue( id, out state );
         if ( ! ( state is V8ScriptEngine engine ) ) {
            Verbo( "New eval shell for {0}", id );
            state = engine = new V8ScriptEngine();
            engine.AddHostObject( "game", new HostTypeCollection( "mscorlib", "Assembly-CSharp", "Cinemachine", "0Harmony", "Newtonsoft.Json" ) );
            engine.AddHostObject( "jsl", typeof( ScriptHelpers ) );
            lock ( Sessions ) Sessions[ id ] = state;
         }
         return engine.Evaluate( code );
      } catch ( Exception ex ) { return ex; } }
   }
}