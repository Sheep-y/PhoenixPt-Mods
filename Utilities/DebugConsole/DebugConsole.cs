using Base.Utils.GameConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Sheepy.PhoenixPt.DebugConsole {
   public static class Mod {
      public static void Init () => SplashMod();

      public static void SplashMod ( Func< string, object, object > api = null ) {
         Application.logMessageReceived += UnityToConsole;
         GameConsoleWindow.DisableConsoleAccess = false;
      }

      private static string LineCache = "";

      private static void UnityToConsole ( string condition, string stackTrace, LogType type ) {
         if ( type == LogType.Error && condition.Contains( "inside a graphic rebuild loop. This is not supported." ) ) return;
         var line = $"{LineCache}\n{type} {condition} {stackTrace}";
         try {
            GameConsoleWindow.Create().WriteLine( line );
            LineCache = "";
         } catch ( Exception er ) { LineCache = line; }
      }
   }
}