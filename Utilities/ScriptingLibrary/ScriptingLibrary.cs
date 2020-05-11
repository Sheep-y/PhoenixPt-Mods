using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Sheepy.PhoenixPt.ScriptingLibrary {

   public class ScriptingLibrary : ZyMod {

      internal static void SplashMod ( Func<string,object,object> api ) => SetApi( api );

      public static object ActionMod ( string modId, Dictionary<string,object> action ) {
         object value = null;
         if ( action?.TryGetValue( "Eval", out value ) != true || ! ( value is string code ) ) return false;
         LoadLibraries();
         PrepareScript();
         Verbo( "{0}|Eval> {1}", modId, code );
         var task = CSharpScript.EvaluateAsync( code, Options );
         task.Wait();
         if ( task.IsFaulted ) return new EvaluateException( "Eval error", task.Exception );
         if ( task.Result != null ) Info( task.Result );
         return true;
      }

      // Assemblies usable by the script, beyond System.* and Unity.*
      private static readonly string[] ScriptAssemblies = new string[]{ "mscorlib", "Assembly-CSharp,", "Cinemachine,", "0Harmony,", "Newtonsoft.Json," };
      private static ScriptOptions Options;

      private static void LoadLibraries () { lock ( ScriptAssemblies ) {
         if ( Options != null ) return;
         Info( "Loading C# scripting" );

         var dir = Path.Combine( Api( "dir" )?.ToString(), "lib" );
         foreach ( var f in new string[]{
               "netstandard.dll",
               "Microsoft.Win32.Primitives.dll",
               "System.Buffers.dll",
               "System.Collections.Immutable.dll",
               "System.Memory.dll",
               "System.Numerics.Vectors.dll",
               "System.Reflection.Metadata.dll",
               "System.Runtime.CompilerServices.VisualC.dll",
               "System.Security.Principal.dll",
               "System.Threading.Tasks.Extensions.dll",
               "System.ValueTuple.dll",
               "Microsoft.CodeAnalysis.CSharp.dll",
               "Microsoft.CodeAnalysis.CSharp.Scripting.dll",
               "Microsoft.CodeAnalysis.dll",
               "Microsoft.CodeAnalysis.Scripting.dll" } ) {
            var path = Path.Combine( dir, f );
            Verbo( "Loading {0}", path );
            Assembly.LoadFrom( path );
         }
      } }

      private static void PrepareScript () { lock ( ScriptAssemblies ) {
         if ( Options != null ) return;
         Info( "Initiating C# scripting" );

         Options = ScriptOptions.Default
            .WithLanguageVersion( Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7_3 )
            .WithAllowUnsafe( false )
            .WithCheckOverflow( true )
            .WithFileEncoding( Encoding.UTF8 )
            .WithWarningLevel( 4 )
            .WithEmitDebugInformation( false )
            .WithOptimizationLevel( Microsoft.CodeAnalysis.OptimizationLevel.Release );

         var assemblies = new HashSet<Assembly>();
         var names = new HashSet<string>();
         var CodePath = Api( "dir", "loader" );
         foreach ( var asm in AppDomain.CurrentDomain.GetAssemblies() ) try {
            Type[] types;
            if ( asm.IsDynamic || ! CodePath.Equals( Path.GetDirectoryName( asm.Location ) ) ) continue;
            var name = asm.FullName;
            //ModLoader.Log.Verbo( name );
            if ( name.StartsWith( "System" ) || name.StartsWith( "Unity." ) || name.StartsWith( "UnityEngine." ) ||
                  ScriptAssemblies.Any( e => name.StartsWith( e ) ) ) {
               assemblies.Add( asm );
               try {
                  types = asm.GetTypes();
               } catch ( ReflectionTypeLoadException rtlex ) { // Happens on System.dll
                  types = rtlex.Types;
               }
               foreach ( var t in types ) {
                  if ( t?.IsPublic != true ) continue;
                  if ( ! string.IsNullOrEmpty( t.Namespace ) )
                     names.Add( t.Namespace );
               }
            }
         } catch ( Exception ex ) { Warn( ex ); }
         names.Remove( "System.Xml.Xsl.Runtime" );

         var usings = names.ToArray();
         Options = Options.WithReferences( assemblies.ToArray() ).WithImports( usings );
         string usingLog () {
            Array.Sort( usings );
            return string.Join( ";", usings );
         }
         Verbo( "{0} namespaces found and added to scripts: {0}", usings.Length, (Func<string>) usingLog );
      } }
   }
}