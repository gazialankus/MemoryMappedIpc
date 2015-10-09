using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MemoryMappedIpcServer.Shared
{
    public class ServerNames {
        public static string MotionServer(int clientId) {
            return "wii_" + clientId;
        }

        public static string GamepadServer() {
            return "Gamepad_proj";
        }

        public static int DefaultTotalBufferSizeInLines = 100;
    }
}
