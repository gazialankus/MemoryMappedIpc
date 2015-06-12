using System;
using System.Collections.Generic;
using System.Text;

namespace MemoryMappedIpcServer.Shared
{
    public class PipeMessage {
        // for these two, the next byte is the wii id
        public static byte START_GYRO_CALIBRATION = 1;
        public static byte STOP_GYRO_CALIBRATION = 2;
    }
}
