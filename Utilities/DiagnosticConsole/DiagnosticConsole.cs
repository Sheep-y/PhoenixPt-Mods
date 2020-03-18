using Base.Utils.GameConsole;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Sheepy.PhoenixPt.DiagnosticConsole {
   public class Mod : ZyMod {
      public static void Init () => new Mod().SplashMod();

      public void SplashMod ( Action< SourceLevels, object, object[] > logger = null ) {
         //Application.logMessageReceivedThreaded += UnityToModLoaderLog;
         Application.logMessageReceived += UnityToConsole;
         GameConsoleWindow.DisableConsoleAccess = false;
         SetLogger( logger );
      }

      public static void UnityToConsole ( string condition, string stackTrace, LogType type ) {
         GameConsoleWindow.Create().WriteLine( "{0} {1} {2}", type, condition, stackTrace );
      }

      public static void UnityToModLoaderLog ( string condition, string stackTrace, LogType type ) {
         var level = SourceLevels.Information;
         switch ( type ) {
            case LogType.Warning   : level = SourceLevels.Warning; break;
            case LogType.Error     :
            case LogType.Exception : level = SourceLevels.Error; break;
         }
         Logger?.Invoke( level, "{0} {1}", new object[]{ condition, stackTrace } );
      }
   }
}
