using Base.Utils.GameConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Sheepy.PhoenixPt.DiagnosticConsole {
   public class Mod {
      public static void Init () => new Mod().SplashMod();

      public void SplashMod () {
         Application.logMessageReceived += UnityToConsole;
         GameConsoleWindow.DisableConsoleAccess = false;
      }

      public static void UnityToConsole ( string condition, string stackTrace, LogType type ) {
         GameConsoleWindow.Create().WriteLine( "{0} {1} {2}", type, condition, stackTrace );
      }
   }
}
