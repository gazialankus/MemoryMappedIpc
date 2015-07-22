using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MemoryMappedIpcServer.Shared
{
    public class OrientationQuaternionMessage : AbstractMessage {
        public float X;
        public float Y;
        public float Z;
        public float W;

        public OrientationQuaternionMessage(long milliseconds, byte deviceId, float x, float y, float z, float w) 
            : base(MessageType.OrientationQuaternionMessage, milliseconds, deviceId) {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public OrientationQuaternionMessage(BinaryReader br, ref int size) 
            : base(MessageType.OrientationQuaternionMessage, br, ref size) {
            X = ReadFloatAndAccumulateSize(br, ref size);
            Y = ReadFloatAndAccumulateSize(br, ref size);
            Z = ReadFloatAndAccumulateSize(br, ref size);
            W = ReadFloatAndAccumulateSize(br, ref size);
            PadMessageEnd(br, ref size);
        }

        public override int WriteTo(BinaryWriter bw) {
            int size = base.WriteTo(bw);
            WriteAndAccumulateSize(bw, X, ref size);
            WriteAndAccumulateSize(bw, Y, ref size);
            WriteAndAccumulateSize(bw, Z, ref size);
            WriteAndAccumulateSize(bw, W, ref size);
            PadMessageEnd(bw, ref size);
            bw.Flush();
            return size;
        }
    }
}
