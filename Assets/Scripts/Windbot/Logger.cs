using System;
using UnityEngine;

namespace WindBot
{
    public static class Logger
    {
        public static void WriteLine(string message)
        {
            var line = "[" + DateTime.Now.ToString("yy-MM-dd HH:mm:ss") + "] " + message;
            Console.WriteLine(line);
            Debug.Log(line);
        }

        public static void DebugWriteLine(string message)
        {
#if DEBUG
            Console.WriteLine("[" + DateTime.Now.ToString("yy-MM-dd HH:mm:ss") + "] " + message);
#endif
            Debug.Log("[WindBot] " + message);
        }

        public static void WriteErrorLine(string message)
        {
            var line = "[" + DateTime.Now.ToString("yy-MM-dd HH:mm:ss") + "] " + message;
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Error.WriteLine(line);
            Console.ResetColor();
            Debug.LogError(line);
        }
    }
}
