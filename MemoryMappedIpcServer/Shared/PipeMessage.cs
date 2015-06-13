using System;
using System.Collections.Generic;
using System.Text;

namespace MemoryMappedIpcServer.Shared
{
    public class PipeMessage {
        // for these two, the next byte is the wii id
        public const byte START_GYRO_CALIBRATION = 11;
        public const byte STOP_GYRO_CALIBRATION = 22;
    }
}
