using System;
using System.IO;

namespace MemoryMappedIpcServer.Shared {

    public class MotionMessage : AbstractMessage {
        public float X;
        public float Y;
        public float Z;

        public MotionMessage(MessageType messageType, long milliseconds, byte wid, float x, float y, float z) 
            : base(messageType, milliseconds, wid) {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public MotionMessage(MessageType messageType, BinaryReader br, ref int size) : base(messageType, br, ref size) {
            X = ReadFloatAndAccumulateSize(br, ref size);
            Y = ReadFloatAndAccumulateSize(br, ref size);
            Z = ReadFloatAndAccumulateSize(br, ref size);
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
