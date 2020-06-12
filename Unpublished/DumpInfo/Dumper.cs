using Base.Defs;
using Base.UI;
using PhoenixPoint.Common.Entities.Addons;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text.RegularExpressions;
using UnityEngine;
using static System.Reflection.BindingFlags;

namespace Sheepy.PhoenixPt.DumpInfo {

   internal abstract class Dumper {
      protected readonly string Filename;
      protected readonly Type DataType;
      protected readonly List<object> Data;

      internal Dumper ( string name, Type key, List<object> list ) {
         foreach ( var chr in Path.GetInvalidFileNameChars() )
            name = name.Replace( chr, '-' );
         Filename = name.Trim();
         DataType = key;
         Data = list;

         var config = Mod.Config;
         Depth = config.Depth;
         Replace_Recur_With_Ref = config.Replace_Recur_With_Ref;
      }

      private StreamWriter Writer;
      private readonly Dictionary< object, int > RecurringObject = new Dictionary< object, int >();

      private readonly ushort Depth = 50;
      private readonly bool   Replace_Recur_With_Ref = true;

      private string DeleteOldDumps () {
         var path = Path.Combine( Mod.DumpDir, Filename + ".xml" );
         var gz = path + ".gz";
         File.Delete( path );
         File.Delete( gz );
         return Mod.Config.Use_GZip ? gz : path;
      }

      internal void DumpData () { try { lock ( Data ) {
         ZyMod.Info( "Dumping {0} ({1})", DataType.Name, Data.Count );
         SortData();
         var path = DeleteOldDumps();
         using ( var fstream = new FileStream( path, FileMode.Create ) ) {
            if ( Mod.Config.Use_GZip ) {
               var buffer = new GZipStream( fstream, CompressionLevel.Optimal );
               using ( var writer = new StreamWriter( buffer ) ) DumpToWriter( writer );
            } else {
               using ( var writer = new StreamWriter( fstream ) ) DumpToWriter( writer );
            }
         }
         ZyMod.Verbo( "Finished {0}, {1} bytes written", DataType.Name, new FileInfo( path ).Length );
         Data.Clear();
         RecurringObject.Clear();
      } } catch ( Exception ex ) {  ZyMod.Error( ex ); } }

      private void DumpToWriter ( StreamWriter writer ) {
         Writer = writer;
         writer.Write( $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" );
         var attr = new List<string>{ "count", Data.Count.ToString() };
         if ( Mod.GameVersion != null ) attr.AddRange( new string[]{ "game", Mod.GameVersion } );
         attr.AddRange( new string[]{ "dumpinfo", Assembly.GetExecutingAssembly().GetName().Version.ToString() } );
         StartTag( DataType.Name, false, attr.ToArray() );
         foreach ( var val in Data )
            RecurringObject.Add( val, RecurringObject.Count );
         foreach ( var val in Data )
            ToXml( val );
         EndTag( DataType.Name );
         writer.Flush();
      }

      protected abstract void SortData();

      private void ToXml ( object subject ) {
         if ( subject == null ) return;
         Mem2Xml( subject.GetType().Name, subject, 0 );
         Writer.Write( '\n' );
      }

      private void Mem2Xml ( string name, object val, int level ) {
         if ( val == null ) { NullMem( name ); return; }
         if ( val is string str ) { StartTag( name, true, "val", str ); return; }
         if ( val is LocalizedTextBind l10n ) {
            StartTag( name, false, "key", l10n.LocalizationKey );
            Writer.Write( EscXml( l10n.Localize() ) );
            EndTag( name );
            return;
         }
         if ( val is GameObject obj ) { StartTag( name, true, "name", obj.name ); return; }
         if ( val is byte[] bary ) {
            if ( name == "NativeData" ) // MapParcelDef.NativeData
               StartTag( name, true, "length", bary.Length.ToString() );
            else
               SimpleMem( name, Convert.ToBase64String( bary ) );
            return;
         }
         var type = val.GetType();
         if ( type.IsClass ) {
            var bDef = val as BaseDef;
            if ( ! ( val is Array ) ) { // Simple objects
               if ( val is AK.Wwise.Bank ) return; // Ref error NullReferenceException
               if ( bDef != null )
                  foreach ( var simpleDef in Mod.SkipRecur )
                     if ( DataType != simpleDef && simpleDef.IsAssignableFrom( val.GetType() ) ) {
                        SimpleBaseDef( name, bDef );
                        return;
                     }
               if ( type.Namespace?.StartsWith( "UnityEngine", StringComparison.InvariantCulture ) == true )
                  { StartTag( name, true, "type", type.FullName ); return; }
            }
            if ( level >= Depth ) { SimpleMem( name, "..." ); return; }
            if ( val is IEnumerable list && ! ( val is AddonDef ) ) {
               DumpList( name, list, level + 1 );
               return;
            }
            int id = -1;
            if ( level > 0 ) {
               if ( isRecur( name, val, out id ) ) return;
            } else if ( Replace_Recur_With_Ref )
               id = RecurringObject[ val ];
            if ( bDef != null )
               SimpleBaseDef( name, bDef, false );
            else if ( id < 0 )
               StartTag( name, false );
            else
               StartTag( name, false, "id", id.ToString( "X" ) );
         } else {
            if ( type.IsPrimitive || type.IsEnum || val is Guid ) { StartTag( name, true, "val", val.ToString() ); return; }
            if ( val is Color color ) { WriteColour( name, color ); return; }
            StartTag( name ); // Other structs
         }
         Obj2Xml( val, level + 2 ); // Either structs or non-enum objects
         EndTag( name );
      }

      private bool isRecur ( string name, object val, out int id ) {
         id = -1;
         if ( ! Replace_Recur_With_Ref ) return false;
         try {
            if ( RecurringObject.TryGetValue( val, out int link ) ) {
               if ( val is BaseDef bDef )
                  SimpleBaseDef( name, bDef );
               else
                  StartTag( name, true, "ref", link.ToString( "X" ) );
               return true;
            }
         } catch ( Exception ex ) { StartTag( name, true, "err_H", ex.GetType().Name ); return true; } // Hash error
         id = RecurringObject.Count;
         RecurringObject.Add( val, id );
         return false;
      }

      private void SimpleBaseDef ( string tag, BaseDef def, bool selfClose = true ) => StartTag( tag, selfClose, "name", def.name, "guid", def.Guid );

      private void DumpList ( string name, IEnumerable list, int level ) {
         StartTag( name );
         var itemType = list.GetType().GetElementType();
         foreach ( var e in list ) {
            if ( e == null )
               NullMem( "LI" );
            else
               Mem2Xml( e.GetType() == itemType ? "LI" : ( "LI." + e.GetType().Name ), e, level );
         }
         EndTag( name );
      }

      private void Obj2Xml ( object subject, int level ) {
         var type = subject.GetType();
         if ( level == 0 ) { Writer.Write( type.Name, subject, 1 ); return; }
         if ( level > Mod.Config.Depth ) { Writer.Write( "..." ); return; }
         foreach ( var f in type.GetFields( Public | NonPublic | Instance ) ) try {
            Mem2Xml( f.Name, f.GetValue( subject ), level + 1 );
         } catch ( ApplicationException ex ) {
            StartTag( f.Name, true, "err_F", ex.GetType().Name ); // Field.GetValue error
         }
         if ( subject.GetType().IsClass ) {
            foreach ( var f in type.GetProperties( Public | NonPublic | Instance ) ) try {
               if ( f.GetCustomAttributes( typeof( ObsoleteAttribute ), false ).Any() ) continue;
               Mem2Xml( f.Name, f.GetValue( subject ), level + 1 );
            } catch ( ApplicationException ex ) {
               StartTag( f.Name, true, "err_P", ex.GetType().Name ); // Property.GetValue error
            }
         }
      }

      private void SimpleMem ( string name, string val ) {
         StartTag( name );
         Writer.Write( EscXml( val ) );
         EndTag( name );
      }

      private void WriteColour ( string tag, Color val ) {
         var w = Writer;
         w.Write( '<' );
         w.Write( EscTag( tag ) );
         w.Write( " r=\"" );
         w.Write( val.r );
         w.Write( "\" g=\"" );
         w.Write( val.g );
         w.Write( "\" b=\"" );
         w.Write( val.b );
         w.Write( "\" a=\"" );
         w.Write( val.a );
         w.Write( "\"/>" );
      }

      private void NullMem ( string name ) {
         _StartTag( name );
         Writer.Write( " null=\"1\"/>" );
      }

      private void _StartTag ( string tag ) {
         Writer.Write( '<' );
         Writer.Write( EscTag( tag ) );
      }

      private void _WriteAttr ( string attr, string aVal ) {
         var w = Writer;
         w.Write( ' ' );
         w.Write( attr );
         w.Write( "=\"" );
         w.Write( EscXml( aVal ) );
         w.Write( '"' );
      }

      private void StartTag ( string tag ) {
         _StartTag( tag );
         Writer.Write( '>' );
      }

      private void StartTag ( string tag, bool selfClose, params string[] attrs ) {
         _StartTag( tag );
         for ( var i = 0 ; i < attrs.Length ; i += 2 )
            _WriteAttr( attrs[i], attrs[i+1] );
         Writer.Write( selfClose ? "/>" : ">" );
      }

      private void EndTag ( string tag ) {
         var w = Writer;
         w.Write( "</" );
         w.Write( EscTag( tag ) );
         w.Write( '>' );
      }

      private static Regex cleanTag = new Regex( "[^\\w:-]+", RegexOptions.Compiled );
      private static string EscTag ( string txt ) {
         txt = cleanTag.Replace( txt, "." );
         while ( txt.Length > 0 && txt[0] == '.' ) txt = txt.Substring( 1 );
         return txt;
      }
      private static string EscXml ( string txt ) => SecurityElement.Escape( txt );
   }
}