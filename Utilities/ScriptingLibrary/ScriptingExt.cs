﻿using Base.Utils.GameConsole;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sheepy.PhoenixPt.ScriptingLibrary {
   using ModnixAPI = Func<string,object,object>;

   internal class ScriptingExt : ZyMod {

      internal static void RegisterAPI () {
         Api( "api_add eval.js", (ModnixAPI) API_Eval_JS );
         //Api( "api_add zy.eval.js", (ModnixAPI) API_Eval_CS );
      }

      private static object API_Eval_JS ( string spec, object param ) {
         var code = param?.ToString();
         if ( string.IsNullOrWhiteSpace( code ) ) return new ArgumentNullException( nameof( param ) );
         if ( ScriptingLibrary.LoadLibraries() is Exception err ) return err;
         Info( "API> {0}", code );
         var mod = ( Api( "api_stack mod" ) as string[] )?.FirstOrDefault( e => e != "Zy.JavaScript" ) ?? "API";
         return ScriptingEngine.Eval( mod, code );
      }

      [ ConsoleCommand( Command = "JavaScript", Description = "(code) - Evaluate JavaScript code, or enter JavaScript shell if no code" ) ]
      public static void Console_JavaScript ( IConsole console, string[] param ) {
         var code = string.Join( " ", param ?? Array.Empty<string>() ).Trim();
         if ( string.IsNullOrEmpty( code ) ) {
            Api( "console.shell start", new Dictionary<string, object> {
               { "abbr", "JS" },
               { "placeholder_colour", UnityEngine.Color.blue },
               { "handler", (Action<IConsole,string>) EvalToConsole },
            } );
         } else
            EvalToConsole( console, code );
      }

      private static void EvalToConsole ( IConsole console, string code ) {
         if ( ScriptingLibrary.LoadLibraries() is Exception err )
            console.Write( FormatException( err ) );
         else
            WriteToConsole( ScriptingEngine.Eval( "Console", code ), console );
      }

      internal static void WriteToConsole ( object value, IConsole console = null ) {
         if ( value is Exception ex ) GameConsoleWindow.Create().Write( FormatException( ex ) + ex.StackTrace );
         if ( Api( "console.write", value ) is bool write && write ) return;
         if ( console == null ) console = GameConsoleWindow.Create();
         else try { // Catch ToString() error
            console.Write( value + " (" + value.GetType().FullName + ")" );
         } catch ( Exception ) { console.Write( value.GetType().FullName ); }
      }

      private static string FormatException ( Exception ex ) => $"<color=red>{ex.GetType()} {ex.Message}</color>";
   }
}