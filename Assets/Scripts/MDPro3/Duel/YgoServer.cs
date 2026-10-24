using System;
using System.Runtime.InteropServices;
using System.Threading;
using MDPro3.Duel.YGOSharp;

namespace MDPro3.Net
{
    internal static unsafe class Dll
    {
        [DllImport("ygoserver", CallingConvention = CallingConvention.Cdecl)]
        public static extern int start_server([MarshalAs(UnmanagedType.LPUTF8Str)] string args);
        [DllImport("ygoserver", CallingConvention = CallingConvention.Cdecl)]
        public static extern void stop_server();
        [DllImport("ygoserver", CallingConvention = CallingConvention.Cdecl)]
        public static extern int get_server_format_capabilities();
        [DllImport("ygoserver", CallingConvention = CallingConvention.Cdecl)]
        public static extern void set_local_genesys_point_cap(uint cap);
        [DllImport("ygoserver", CallingConvention = CallingConvention.Cdecl)]
        public static extern int admin_duel_action([MarshalAs(UnmanagedType.LPUTF8Str)] string name,int operation,int target,int value);
    }

    public class YgoServer
    {
        public static bool SupportsTcgAndGenesys()
        {
            try { return (Dll.get_server_format_capabilities() & 3) == 3; }
            catch (EntryPointNotFoundException) { return false; }
            catch (DllNotFoundException) { return false; }
        }
        public static Thread serverThread;
        public static void StartServer(string args)
        {
            if (ServerRunning())
                StopServer();
            if (SupportsTcgAndGenesys()) Dll.set_local_genesys_point_cap((uint)DuelFormat.PointCap);

            serverThread = new Thread(() =>
            {
                Dll.start_server(args);
            });
            serverThread.Start();
        }

        public static void StopServer()
        {
            Dll.stop_server();
            serverThread?.Abort();
        }

        public static bool ServerRunning()
        {
            return serverThread != null && serverThread.IsAlive;
        }
    }
}
