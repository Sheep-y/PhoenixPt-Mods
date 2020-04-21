﻿using Base.Utils.GameConsole;
using Newtonsoft.Json;
using PhoenixPoint.Geoscape.View;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Sheepy.PhoenixPt.DebugConsole {
   internal class Extensions : ZyMod {
      [ ConsoleCommand( Command = "modnix", Description = "Call Modnix or compatible api." ) ]
      public static void ApiCommand ( IConsole console, string[] param ) { try {
         if ( ! HasApi ) {
            console.WriteLine( "<color=fuchsia>Modnix API not found.</color>" );
            return;
         }
         if ( param == null || param.Length == 0 ) throw new ApplicationException( "Api action required." );
         object arg = null;
         if ( param.Length > 2 ) {
            arg = new string[ param.Length - 1 ];
            Array.Copy( param, 1, arg as string[], 0, param.Length - 1 );
         } else if ( param.Length == 2 )
            arg = param[1];
         WriteResult( Api( param[0], arg ) );
      } catch ( Exception ex ) { Error( ex ); } }

      internal static object GuiTree ( object root ) {
         if ( root is Transform t ) root = t.gameObject;
         if ( ! ( root is GameObject obj ) ) {
            Warn( new ArgumentException( "Not a Unity GameObject: " + root?.GetType().FullName ?? "null" ) );
            return false;
         }
         DumpComponents( "", new HashSet<object>(), obj );
         return true;
      }

      internal static void DumpComponents ( string prefix, HashSet<object> logged, GameObject e ) {
         if ( prefix.Length > 20 ) return;
         if ( logged.Contains( e ) ) return;
         logged.Add( e );
         Info( "{0}> {1} {2}{3} Layer {4}", prefix, e.name, TypeName( e ), e.activeSelf ? "" : " (Inactive)", e.layer );
         foreach ( var c in e.GetComponents<Component>() ) {
            var typeName = TypeName( c );
            if ( c is Transform rect )
               Info( "{0}...{1} Pos {2} Scale {3} Rotate {4}", prefix, typeName, rect.localPosition, rect.localScale, rect.localRotation );
            else if ( c is UnityEngine.UI.Text txt )
               Info( "{0}...{1} {2} {3}", prefix, typeName, txt.color, txt.text );
            else if ( c is I2.Loc.Localize loc )
               Info( "{0}...{1} {2}", prefix, typeName, loc.mTerm );
            else if ( c is UnityEngine.UI.LayoutGroup layout )
               Info( "{0}...{1} Padding {2}", prefix, typeName, layout.padding );
            else if ( c is FovControllableBehavior fov )
               Info( "{0}...{1} Invis {2} Billboard {3}-{4}", prefix, typeName, fov.ControllingDef.InvisibleOverFov, fov.ControllingDef.BillboardStartFov, fov.ControllingDef.BillboardFullFov );
            else
               Info( "{0}...{1}", prefix, typeName );
         }
         for ( int i = 0 ; i < e.transform.childCount ; i++ ) 
            DumpComponents( prefix + "  ", logged, e.transform.GetChild( i ).gameObject );
      }

      private static string TypeName ( object e ) => e?.GetType().FullName.Replace( "UnityEngine.", "UE." );

      private static void WriteResult ( object result ) { try {
         if ( result is string line ) ;
         else if ( result == null ) line = "null";
         else if ( result is Exception || result is StackTrace || result is MethodInfo || result is Task ) line = result.ToString();
         else line = JsonConvert.SerializeObject( result, Formatting.None );
         Mod.ConsoleWrite( line );
         if ( result is Task<object> task )
            task.ContinueWith( e => {
               if ( e.IsCanceled ) WriteResult( "Cancelled" );
               else if ( e.IsFaulted ) WriteResult( e.Exception );
               else WriteResult( e.Result );
            } );
      } catch ( Exception ex ) { Error( ex ); } }
   }
}