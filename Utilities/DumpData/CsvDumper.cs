using Base.Defs;
using Base.Utils.GameConsole;
using I2.Loc;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical.Entities.Statuses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sheepy.PhoenixPt.DumpData {

   internal abstract class CsvDumper : BaseDumper {
      protected override string FileExtension () => "csv";

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

   internal class GuidDumper : CsvDumper {
      internal GuidDumper ( string name, List<object> list ) : base( name, list ) { }

      protected override void SortData() => Data.Sort( Comparator< BaseDef, string >( e => e?.Guid ) );

      protected override string FileExtension () => "csv";

      protected override void DoDump () {
         NewCells( "Type", "Guid", "name", "ResourcePath", "Name/Id", "DispayName1", "DisplayName2", "Description", "Category", "Tags" );
         foreach ( var e in Data.OfType<BaseDef>() ) {
            if ( e == null ) continue;
            var type = e.GetType();
            NewRow( type.Name, e.Guid, e.name, e.ResourcePath );

            // ViewElement, ViewDef, Id, RequirementText
            var view = e.GetMember( "ViewElementDef" ) ?? e.GetMember( "ViewElement" ) ?? e.GetMember( "ViewDef" );
            if ( view is ViewElementDef v )
               NewCells( v.Name, v.DisplayName1?.Localize(), v.DisplayName2?.Localize(), v.Description?.Localize(), v.Category?.Localize() );
            else if ( view != null ) {
               NewCell( view.GetMember( "Name" )?.ToText() );
               NewCell( view.GetMember( "DisplayName1" )?.ToText() ?? view.GetMember( "DisplayName" )?.ToText() );
               NewCell( view.GetMember( "DisplayName2" )?.ToText() );
               NewCell( view.GetMember( "Description" )?.ToText() );
               NewCell( view.GetMember( "Category" )?.ToText() );
            } else {
               view = e.GetMember( "Id" );
               if ( view != null ) NewCell( view?.ToText() );
               else {
                  view = e.GetMember( "RequirementText" );
                  if ( view != null ) NewCells( "", "", "", view?.ToText() );
               }
            }
            var tags = ( e.GetMember( "Tags" ) as IEnumerable )?.OfType<BaseDef>();
            if ( tags != null ) NewCell( string.Join( ", ", tags.Select( tag => tag.name ) ) );
         }
      }
   }

   internal class LangDumper : CsvDumper {
      private List<string> Langs;
      private readonly HashSet<string> Exported = new HashSet<string>();

      internal LangDumper ( string name, List<object> list ) : base( name, list ) { }

      protected override void SortData() {
         Langs = Data[ 0 ] as List<string>;
         Data.Remove( 0 );
         Data.Sort( Comparator< TermData, string >( e => e?.Term ) );
      }

      protected override void DoDump () {
         NewCells( "Type", "Term", "Key", "Flags", "Description" );
         if ( Langs != null ) NewCells( Langs.ToArray() );
         foreach ( var term in Data.OfType<TermData>() ) { // Some keys has a trailing \r
            if ( Exported.Contains( term.Term ) ) continue;
            NewRow( term.TermType.ToString(), term.Term, term.Key?.Trim(), Convert.ToBase64String( term.Flags ), term.Description );
            NewCells( term.Languages );
            Exported.Add( term.Term );
         }
         Exported.Clear();
      }
   }

   internal class CommandDumper : CsvDumper {
      internal CommandDumper ( string name, List<object> list ) : base( name, list ) { }

      protected override void SortData() => Data.Sort( Comparator< ConsoleCommandAttribute, string >( e => e.Command ) );

      protected override void DoDump () {
         NewCells( "Class", "Command", "Parameter", "Description" );
         FieldInfo _method = typeof( ConsoleCommandAttribute ).GetField( "_methodInfo", BindingFlags.NonPublic | BindingFlags.Instance );
         foreach ( var cmd in Data.OfType<ConsoleCommandAttribute>() ) {
            var method =  _method.GetValue( cmd ) as MethodInfo;
            NewRow( method.DeclaringType.FullName, cmd.Command, string.Join( ", ", method.GetParameters()
               .Skip( 1 ).Select( e => e.ParameterType.Name + " " + e.Name ) ), cmd.Description );
         }
      }
   }
}