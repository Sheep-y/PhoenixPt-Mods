﻿using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
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

      // Assemblies usable by the script, beyond System.* and Unity.*
      private static readonly string[] ScriptAssemblies = new string[]{ "mscorlib", "Assembly-CSharp,", "Cinemachine,", "0Harmony,", "Newtonsoft.Json," };
      private static ScriptOptions Options;

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
         Verbo( "{0} namespaces found and added to scripts: {1}", usings.Length, (Func<string>) usingLog );
      } }

      private static readonly Dictionary< string, ScriptState > Sessions = new Dictionary<string, ScriptState>();

      public static object Eval ( string id, string code ) { try {
         Task<ScriptState<object>> task;
         ScriptState state;
         lock ( Sessions ) Sessions.TryGetValue( id, out state );
         if ( state == null ) {
            PrepareScript();
            Verbo( "New eval shell for {0}", id );
            task = CSharpScript.RunAsync( code, Options );
         } else
            task = state.ContinueWithAsync( code, Options );
         task.Wait();
         if ( task.IsFaulted ) return new EvaluateException( "Eval error", task.Exception );
         lock ( Sessions ) {
            Sessions.Remove( id );
            Sessions.Add( id, task.Result );
         }
         return task.Result.ReturnValue;
      } catch ( CompilationErrorException ex ) { return ex; } }
   }
}