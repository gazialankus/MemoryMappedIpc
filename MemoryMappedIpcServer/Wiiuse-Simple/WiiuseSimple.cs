using System;
using System.Runtime.InteropServices;

namespace MemoryMappedIpcServer {
    class WiiuseSimple {

        [DllImport("wiiuse_simple.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int test_wiiuse_simple();

        [DllImport("wiiuse_simple.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void wus_init(int num_remotes_desired);

        [DllImport("wiiuse_simple.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int wus_find_and_connect(int num_remotes_desired);

        [DllImport("wiiuse_simple.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int wus_is_connected(int wmi);

        [DllImport("wiiuse_simple.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int wus_any_connected();

        [DllImport("wiiuse_simple.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int wus_poll();

        [DllImport("wiiuse_simple.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern Int16 wus_get_buttons_pressed(int wmi);

        [DllImport("wiiuse_simple.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern Int16 wus_get_buttons_held(int wmi);

        [DllImport("wiiuse_simple.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern Int16 wus_get_buttons_released(int wmi);

        [DllImport("wiiuse_simple.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void wus_start_accel(int wmi);

        [DllImport("wiiuse_simple.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void wus_stop_accel(int wmi);

        [DllImport("wiiuse_simple.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void wus_start_gyro(int wmi);

        [DllImport("wiiuse_simple.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void wus_stop_gyro(int wmi);

        [DllImport("wiiuse_simple.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int wus_accel_received(int wmi);

        [DllImport("wiiuse_simple.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void wus_get_accel(int wmi, out float x, out float y, out float z);

        [DllImport("wiiuse_simple.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int wus_gyro_received(int wmi);

        [DllImport("wiiuse_simple.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void wus_get_gyro(int wmi, out float x, out float y, out float z);

        [DllImport("wiiuse_simple.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void wus_cleanup();


    }
}
