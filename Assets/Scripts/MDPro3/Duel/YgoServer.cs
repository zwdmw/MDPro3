using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

namespace MDPro3.Net
{
    internal static unsafe class Dll
    {
        [DllImport("ygoserver", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int start_server([MarshalAs(UnmanagedType.LPStr)] string args);
        [DllImport("ygoserver", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern void stop_server();
    }

    public class YgoServer
    {
        public static Thread serverThread;
        public static void StartServer(string args)
        {
            if(ServerRunning())
                StopServer();

            Debug.Log("YgoServer.StartServer args=" + args);
            serverThread = new Thread(() =>
            {
                try
                {
                    var result = Dll.start_server(args);
                    Debug.Log("YgoServer.start_server returned " + result);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            });
            serverThread.Start();
            Debug.Log("YgoServer server thread started");
        }

        public static void StopServer()
        {
            try
            {
                Dll.stop_server();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("YgoServer.StopServer failed: " + ex);
            }
            serverThread?.Abort();
        }

        public static bool ServerRunning()
        {
            return serverThread != null && serverThread.IsAlive;
        }
    }
}
