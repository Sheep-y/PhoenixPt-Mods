using Base.Defs;
using Base.UI;
using PhoenixPoint.Common.Entities.Addons;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Security;
using System.Text.RegularExpressions;
using UnityEngine;
using static System.Reflection.BindingFlags;

namespace Sheepy.PhoenixPt.DumpData {

   internal class XmlDumper : BaseDumper {
      protected override string FileExtension () => "xml";

      protected readonly Type DataType;

      internal XmlDumper ( string name, Type key, List<object> list ) : base( name, list ) {
         DataType = key;
         var config = Mod.Config;
         Depth = config.Depth;
         Skip_Dumped_Objects = config.Skip_Dumped_Objects;
         Skip_Dumped_Defs    = config.Skip_Dumped_Defs;
         Skip_Empty_Objects  = config.Skip_Empty_Objects;
         Skip_Empty_Lists    = config.Skip_Empty_Lists;
         Skip_Zeros          = config.Skip_Zeros;
      }

      private readonly Dictionary< object, int > RecurringObject = new Dictionary< object, int >();

      private readonly ushort Depth;
      private readonly bool   Skip_Dumped_Objects;
      private readonly bool   Skip_Dumped_Defs;
      private readonly bool   Skip_Empty_Objects;
      private readonly bool   Skip_Empty_Lists;
      private readonly bool   Skip_Zeros;

      protected override void DoDump () {
         Writer.Write( $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" );
         var attr = new List<string>{ "count", Data.Count.ToString() };
         if ( Mod.GameVersion != null ) attr.AddRange( new string[]{ "game", Mod.GameVersion } );
         attr.AddRange( new string[]{ "DumpData", Assembly.GetExecutingAssembly().GetName().Version.ToString() } );
         StartTag( DataType.Name, false, attr.ToArray() );
         foreach ( var val in Data )
            RecurringObject.Add( val, RecurringObject.Count );
         foreach ( var val in Data )
            ToXml( val );
         EndTag( DataType.Name );
         RecurringObject.Clear();
      }

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
               if ( bDef != null && Skip_Dumped_Defs )
                  foreach ( var simpleDef in Mod.DumpedTypes )
                     if ( DataType != simpleDef && simpleDef.IsAssignableFrom( val.GetType() ) ) {
                        SimpleBaseDef( name, bDef );
                        return;
                     }
               if ( type.Namespace?.StartsWith( "UnityEngine", StringComparison.InvariantCulture ) == true )
                  { StartTag( name, true, "type", type.FullName ); return; }
            }
            if ( level >= Depth ) { SimpleMem( name, "..." ); return; }
            if ( val is IEnumerable list && ! ( val is AddonDef ) ) {
               if ( Skip_Empty_Lists && ! list.GetEnumerator().MoveNext() ) return;
               DumpList( name, list, level + 1 );
               return;
            }
            int id = -1;
            if ( level > 0 ) {
               if ( WasDumped( name, val, out id ) ) return;
            } else if ( Skip_Dumped_Objects )
               id = RecurringObject[ val ];
            if ( IsEmpty( val ) ) { StartTag( name, true, "empty", "1" ); return; }
            if ( bDef != null )
               SimpleBaseDef( name, bDef, false );
            else if ( id < 0 )
               StartTag( name, false );
            else
               StartTag( name, false, "id", id.ToString( "X" ) );
         } else {
            if ( type.IsPrimitive || type.IsEnum || val is Guid ) {
               var valTxt = val.ToString();
               if ( Skip_Zeros && ( valTxt == "0" || valTxt == "0.0" ) ) return;
               StartTag( name, true, "val", valTxt );
               return;
            }
            if ( val is Color color ) { WriteColour( name, color ); return; }
            if ( IsEmpty( val ) ) { StartTag( name, true, "empty", "1" ); return; }
            StartTag( name ); // Other structs
         }
         Obj2Xml( val, level + 2 ); // Either structs or non-enum objects
         EndTag( name );
      }

      private bool WasDumped ( string name, object val, out int id ) {
         id = -1;
         if ( ! Skip_Dumped_Objects ) return false;
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

      private bool IsEmpty ( object subject ) { try {
         if ( ! Skip_Empty_Objects ) return false;
         if ( subject is AK.Wwise.Event wEvt ) return wEvt.WwiseObjectReference == null && wEvt.ObjectReference == null && wEvt.Id == 0;
         if ( subject is AK.Wwise.Switch wSwt ) return wSwt.WwiseObjectReference == null && wSwt.ObjectReference == null && wSwt.GroupWwiseObjectReference == null;
         var type = subject.GetType();
         foreach ( var f in type.GetFields( Public | NonPublic | Instance ) ) {
            if ( IsObsolete( f ) ) continue;
            if ( ! IsEmptyValue( f.GetValue( subject ) ) ) return false;
         }
         if ( ! subject.GetType().IsClass ) return true;
         foreach ( var f in type.GetProperties( Public | NonPublic | Instance ) ) {
            if ( IsObsolete( f ) ) continue;
            if ( ! IsEmptyValue( f.GetValue( subject ) ) ) return false;
         }
         return true;
      } catch ( ApplicationException ) { return false; } }

      private bool IsEmptyValue ( object val ) {
         if ( val == null || val == default ) return true;
         if ( val is string txt ) return string.IsNullOrEmpty( txt );
         if ( val is LocalizedTextBind l10n ) return string.IsNullOrEmpty( l10n.LocalizationKey );
         if ( val is IEnumerable it ) return ! it.GetEnumerator().MoveNext();
         return false;
      }

      private void Obj2Xml ( object subject, int level ) {
         var type = subject.GetType();
         if ( level == 0 ) { Writer.Write( type.Name, subject, 1 ); return; }
         if ( level > Mod.Config.Depth ) { Writer.Write( "..." ); return; }
         var isBaseDef = subject is BaseDef;
         foreach ( var f in type.GetFields( Public | NonPublic | Instance ) ) try {
            if ( IsObsolete( f ) ) continue;
            if ( isBaseDef && f.Name == "Guid" ) continue;
            Mem2Xml( f.Name, f.GetValue( subject ), level + 1 );
         } catch ( ApplicationException ex ) {
            StartTag( f.Name, true, "err_F", ex.GetType().Name ); // Field.GetValue error
         }
         if ( ! subject.GetType().IsClass ) return;
         foreach ( var f in type.GetProperties( Public | NonPublic | Instance ) ) try {
               if ( IsObsolete( f ) ) continue;
               if ( isBaseDef && f.Name == "name" ) continue;
               Mem2Xml( f.Name, f.GetValue( subject ), level + 1 );
            } catch ( ApplicationException ex ) {
            StartTag( f.Name, true, "err_P", ex.GetType().Name ); // Property.GetValue error
         }
      }

      private static bool IsObsolete ( MemberInfo f ) => f.GetCustomAttributes( typeof( ObsoleteAttribute ), true ).Length > 0;

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

      private static Regex CleanTag = new Regex( "[^\\w:-]+", RegexOptions.Compiled );
      private static string EscTag ( string txt ) {
         txt = CleanTag.Replace( txt, "." );
         while ( txt.Length > 0 && txt[0] == '.' ) txt = txt.Substring( 1 );
         return txt;
      }
      private static string EscXml ( string txt ) => SecurityElement.Escape( txt );
      protected override void SortData () { }
   }

   internal class BaseDefDumper : XmlDumper {
      internal BaseDefDumper ( string name, Type key, List<object> list ) : base( name, key, list ) { }

      protected override void SortData() => Data.Sort( Comparator< BaseDef, string >( e => e?.Guid ) );
   }
}