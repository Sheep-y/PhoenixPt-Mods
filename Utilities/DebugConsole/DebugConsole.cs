using Base.Core;
using Base.Utils.GameConsole;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Sheepy.PhoenixPt.DebugConsole {

   public class ModConfig {
      public bool Log_Game_Error = true;
      public bool Log_Game_Info = true;
   }

   public class Mod : ZyMod {
      internal static ModConfig Config;

      public static void Init () => new Mod().SplashMod();

      public void SplashMod ( Func< string, object, object > api = null ) {
         GameConsoleWindow.DisableConsoleAccess = false;
         SetApi( api, out Config );
         if ( Config.Log_Game_Error || Config.Log_Game_Info ) {
            Application.logMessageReceived += UnityToConsole;
            Patch( typeof( TimingScheduler ), "Update", postfix: nameof( BufferToConsole ) );
         }
      }

      private static List<string> Buffer = new List<string>();

      private static void UnityToConsole ( string condition, string stackTrace, LogType type ) { try {
         //if ( type == LogType.Error && condition.Contains( "inside a graphic rebuild loop. This is not supported." ) ) return;
         switch ( type ) {
            case LogType.Exception: case LogType.Error: case LogType.Warning:
               if ( ! Config.Log_Game_Error ) return; break;
            default:
               if ( ! Config.Log_Game_Info ) return; break;
         }
         // Long message can cause the 
         if ( condition.Length > 1000 || stackTrace?.Contains( "UnityTools.LogFormatter" ) == true || condition.Contains( "Called from a secondary Thread" ) ) return;
         var line = $"{type} {condition}   {stackTrace}".Trim();
         if ( line.Length == 0 ) return;
         lock ( _Lock ) Buffer.Add( line );
      } catch ( Exception ex ) {
         Error( ex );
      } }

      private static void BufferToConsole () { try {
         string[] buffer;
         lock ( _Lock ) {
            if ( Buffer.Count == 0 ) return;
            buffer = Buffer.ToArray();
            Buffer.Clear();
         }
         var console = GameConsoleWindow.Create();
         foreach ( var line in buffer )
            console.WriteLine( line );
      } catch ( Exception ex ) {
         Error( ex );
      } }
   }
}