using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MemoryMappedIpcServer.Shared
{
    public class GyroCalibrationMessage : AbstractMessage {
        public short X, Y, Z;

        public GyroCalibrationMessage(long milliseconds, byte wid, short x, short y, short z) 
            : base(MessageType.GyroCalibrationMessage, milliseconds, wid) {
            X = x;
            Y = y;
            Z = z;
        }

        public GyroCalibrationMessage(BinaryReader br, ref int size) 
            : base(MessageType.GyroCalibrationMessage, br, ref size) {
            X = ReadShortAndAccumulateSize(br, ref size);
            Y = ReadShortAndAccumulateSize(br, ref size);
            Z = ReadShortAndAccumulateSize(br, ref size);
            PadMessageEnd(br, ref size);
        }

        public override int WriteTo(BinaryWriter bw) {
            int size = base.WriteTo(bw);
            WriteAndAccumulateSize(bw, X, ref size);
            WriteAndAccumulateSize(bw, Y, ref size);
            WriteAndAccumulateSize(bw, Z, ref size);
            PadMessageEnd(bw, ref size);
            bw.Flush();
            return size;
        }
    }
}
