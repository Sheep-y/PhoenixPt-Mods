using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sheepy.PhoenixPt.ScriptingLibrary {

   internal class ScriptingEngine : ZyMod {
      private static readonly Dictionary< string, object > HostObjects = new Dictionary<string, object>();
      private static readonly Dictionary< string, Type > HostTypes = new Dictionary<string, Type>();

      private static void PrepareEngine ( V8ScriptEngine engine ) {
         lock ( HostObjects ) {
            if ( HostObjects.Count == 0 ) {
               Verbo( "Listing assemblies" );
               HostObjects.Add( "host", new ExtendedHostFunctions() );
               HostObjects.Add( "xHost", new ExtendedHostFunctions() );
               HostObjects.Add( "DotNet", new HostTypeCollection( "mscorlib", "System", "System.Core", "System.Numerics" ) );
               HostObjects.Add( "Game", new HostTypeCollection( "Assembly-CSharp" ) );
               HostObjects.Add( "Unity", new HostTypeCollection(
                  AppDomain.CurrentDomain.GetAssemblies().Select( e => e.GetName().Name ).Where( e => e.StartsWith( "UnityEngine." ) ).ToArray() ) );
               Verbo( "Listing game object types" );
               HostTypes.Add( "Api", typeof( ApiHelper ) );
               HostTypes.Add( "Log", typeof( LogHelper ) );
               HostTypes.Add( "Repo", typeof( RepoHelper ) );
               HostTypes.Add( "console", typeof( ConsoleHelper ) );
               //HostTypes.Add( "Patch", typeof( PatchHelper ) );
               HostTypes.Add( "Damage", typeof( DamageHelpers ) );
               HostTypes.Add( "Enumerable", typeof( Enumerable ) );
               foreach ( var type in GameAssembly.GetTypes().Concat( Assembly.Load( "UnityEngine.CoreModule" ).GetTypes() )  ) {
                  var name = type.Name;
                  if ( name == "JSON" ) continue;
                  if ( HostTypes.ContainsKey( name ) || HostObjects.ContainsKey( name ) ) continue;
                  HostTypes.Add( name, type );
               }
               Info( "Found {0} object types to add to JavaScript", HostTypes.Count );
            }
         }
         foreach ( var obj in HostObjects )
            engine.AddHostObject( obj.Key, obj.Value );
         foreach ( var obj in HostTypes )
            engine.AddHostType( obj.Key, obj.Value );
         Verbo( "Engine ready" );
      }


      private static readonly Dictionary< string, object > Sessions = new Dictionary< string, object >(); // string, V8ScriptEngine

      public static object Eval ( string id, string code ) { try {
         object state; // Make sure Mod will not crash when V8 cannot be loaded.
         lock ( Sessions ) Sessions.TryGetValue( id, out state );
         if ( ! ( state is V8ScriptEngine engine ) ) {
            Verbo( "New eval shell for {0}", id );
            state = engine = new V8ScriptEngine( V8ScriptEngineFlags.DisableGlobalMembers );
            PrepareEngine( engine );
            lock ( Sessions ) Sessions[ id ] = state;
         }
         lock ( engine ) return engine.Evaluate( code );
      } catch ( Exception ex ) { return ex; } }
   }
}