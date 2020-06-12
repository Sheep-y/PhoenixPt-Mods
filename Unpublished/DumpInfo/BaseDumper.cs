﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

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
         if ( Data.Count == 0 ) {
            ZyMod.Verbo( "Skipping {0} because it is empty", Name );
            return;
         }
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
         DoDump();
         writer.Flush();
      }

      protected static Comparison<object> Comparator < T, V > ( Func<T,V> mapper ) where T : class where V : IComparable<V> {
         return ( left, right ) => {
            V a = mapper( left as T ), b = mapper( right as T );
            if ( a == null ) return b == null ? 0 : -1;
            if ( b == null ) return 1;
            return a.CompareTo( b );
         };
      }

      protected abstract string FileExtension();
      protected abstract void SortData();
      protected abstract void DoDump();
   }
}