using Base.Defs;
using I2.Loc;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sheepy.PhoenixPt.DumpInfo {

   internal abstract class BaseDumper {
      protected readonly string Filename;
      protected readonly string Name;
      protected readonly List<object> Data;

      internal BaseDumper ( string name, List<object> list ) {
         Name = name;
         foreach ( var chr in Path.GetInvalidFileNameChars() )
            name = name.Replace( chr, '-' );
         Filename = name.Trim();
         Data = list;
      }

      protected StreamWriter Writer;

      private string DeleteOldDumps () {
         var path = Path.Combine( Mod.DumpDir, Filename + "." + FileExtension() );
         var gz = path + ".gz";
         File.Delete( path );
         File.Delete( gz );
         return Mod.Config.Use_GZip ? gz : path;
      }

      internal void DumpData () { try { lock ( Data ) {
         ZyMod.Info( "Dumping {0} ({1})", Name, Data.Count );
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
         ZyMod.Verbo( "Finished {0}, {1} bytes written", Name, new FileInfo( path ).Length );
         Data.Clear();
      } } catch ( Exception ex ) {  ZyMod.Error( ex ); } }

      private void DumpToWriter ( StreamWriter writer ) {
         Writer = writer;
         writer.Write( $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" );
         DoDump();
         writer.Flush();
      }

      protected abstract string FileExtension();
      protected abstract void SortData();
      protected abstract void DoDump();

   }

   internal class BaseDefDumper : XmlDumper {
      internal BaseDefDumper ( string name, Type key, List<object> list ) : base( name, key, list ) { }

      protected override void SortData() => Data.Sort( CompareDef );

      private static int CompareDef ( object left, object right ) {
         BaseDef a = left as BaseDef, b = right as BaseDef;
         string aid = a?.Guid, bid = b?.Guid;
         if ( aid == null ) return bid == null ? 0 : -1;
         if ( bid == null ) return 1;
         return aid.CompareTo( bid );
      }
   }

   internal class LangDumper : XmlDumper {
      internal LangDumper ( string name, Type key, List<object> list ) : base( name, key, list ) { }

      protected override void SortData() => Data.Sort( CompareDef );

      private static int CompareDef ( object left, object right ) {
         TermData a = left as TermData, b = right as TermData;
         string aid = a?.Term, bid = b.Term;
         if ( aid == null ) return bid == null ? 0 : -1;
         if ( bid == null ) return 1;
         return aid.CompareTo( bid );
      }
   }
}