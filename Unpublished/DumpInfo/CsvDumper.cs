using I2.Loc;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sheepy.PhoenixPt.DumpInfo {

   internal abstract class CsvDumper : BaseDumper {
      internal CsvDumper ( string name, List<object> list ) : base( name, list ) { }

      private bool HasCell;

      protected void NewRow () {
         Writer.Write( "\r\n" );
         HasCell = false;
      }

      protected void NewRow ( params string[] val ) {
         NewRow();
         if ( val != null && val.Length > 0 ) NewCells( val );
      }

      protected void NewCells ( params string[] val ) { foreach ( var v in val ) NewCell( v ); }

      protected void NewCell ( string val ) {
         var w = Writer;
         if ( HasCell ) w.Write( ',' );
         if ( string.IsNullOrEmpty( val ) ) return;
         var hasQuote = val.IndexOf( '"' ) >= 0;
         if ( hasQuote || val.IndexOf( ',' ) >= 0 || val.IndexOf( '\n' ) >= 0 || val.IndexOf( '\r' ) >= 0 ) {
            w.Write( '"' );
            w.Write( hasQuote ? val.Replace( "\"", "\"\"" ) : val );
            w.Write( '"' );
         } else
            w.Write( val );
         HasCell = true;
      }
   }

   internal class LangDumper : CsvDumper {
      private List<string> Codes;

      internal LangDumper ( string name, List<object> list ) : base( name, list ) { }

      protected override void SortData() {
         Codes = Data[ 0 ] as List<string>;
         Data.Remove( 0 );
         Data.Sort( CompareDef );
      }

      private static int CompareDef ( object left, object right ) {
         TermData a = left as TermData, b = right as TermData;
         if ( a == null ) return -1;
         if ( b == null ) return 1;
         string aid = a?.Term, bid = b.Term;
         if ( aid == null ) return bid == null ? 0 : -1;
         if ( bid == null ) return 1;
         return aid.CompareTo( bid );
      }

      protected override string FileExtension () => "csv";

      protected override void DoDump () {
         NewCells( "Type", "Term", "Key", "Flags", "Description" );
         if ( Codes != null ) NewCells( Codes.ToArray() );
         foreach ( var term in Data.OfType<TermData>() ) { // Some keys has a trailing \r
            NewRow( term.TermType.ToString(), term.Term, term.Key?.Trim(), Convert.ToBase64String( term.Flags ), term.Description );
            NewCells( term.Languages );
         }
      }
   }
}