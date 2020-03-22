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

      private static void UnityToConsole ( string condition, string stackTrace, LogType type ) {
         if ( type == LogType.Error && condition.Contains( "inside a graphic rebuild loop. This is not supported." ) ) return;
         GameConsoleWindow.Create().WriteLine( "{0} {1} {2}", type, condition, stackTrace );
      }
   }
}