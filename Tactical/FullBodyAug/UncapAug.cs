using Harmony;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace Sheepy.PhoenixPt.FullBodyAug {

   internal class UncapAug : ZyMod {

      internal IPatch UncapMutate () => PatchAugUncap( typeof( UIModuleMutate ) );

      internal IPatch UncapBionic () => PatchAugUncap( typeof( UIModuleBionics ) );

      private IPatch PatchAugUncap ( Type screen ) => TryPatch( screen, "InitCharacterInfo", transpiler: nameof( TranspileInitCharacterInfo ) );

      private static IEnumerable<CodeInstruction> TranspileInitCharacterInfo ( IEnumerable<CodeInstruction> instr ) {
         var allCount = instr.Count( e => e.opcode == OpCodes.Ldc_I4_2 );
         if ( allCount < 3 || allCount > 4 ) { // Pre-Blood and Titanium Mutation = 3, Post-B&T Mutation = 4, Post-B&T Bionics = 3
            Api( "log error", new Exception( $"Unrecognised argument code.  Cap count {allCount}, expected 3 or 4.  Aborting." ) );
            //foreach ( var code in instr ) Verbo( code );
            return instr;
         }
         var codes = instr.ToArray();
         for ( var i = 0 ; i < codes.Length ; i++ ) {
            var code = codes[i];
            if ( code.opcode != OpCodes.Ldc_I4_2 ) continue;
            // Post-B&T, the second ldc.i4.2 loads the enum AugumentSlotState.AugumentationLimitReached.
            // It follows a stloc.s 5 and must be skipped.
            if ( i > 0 && codes[ i - 1 ].opcode != OpCodes.Stloc_S )
               code.opcode = OpCodes.Ldc_I4_3;
         }
         return instr.ToList();
      }
   }
}