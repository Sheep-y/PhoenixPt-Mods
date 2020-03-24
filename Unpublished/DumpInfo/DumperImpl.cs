using Base.Defs;
using Base.UI;
using I2.Loc;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using static System.Reflection.BindingFlags;

namespace Sheepy.PhoenixPt.DumpInfo {

   internal class BaseDefDumper : Dumper {
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

   internal class TermDumper : Dumper {
      internal TermDumper ( string name, Type key, List<object> list ) : base( name, key, list ) { }

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