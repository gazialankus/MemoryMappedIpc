using System.IO;

namespace MemoryMappedIpcServer.Shared {
    public struct MotionMessage {
        public byte Wid;
        public bool IsGyro;
        public long Milliseconds;

        public float X;
        public float Y;
        public float Z;

        public MotionMessage(byte wid, bool isGyro, long milliseconds, float x, float y, float z) {
            this.Wid = wid;
            this.IsGyro = isGyro;
            this.Milliseconds = milliseconds;
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        static public void WriteTo(BinaryWriter bw, MotionMessage m) {
            bw.Write(m.Wid);
            bw.Write(m.IsGyro);
            bw.Write(m.Milliseconds);
            bw.Write(m.X);
            bw.Write(m.Y);
            bw.Write(m.Z);
            bw.Flush();
        }

        static public MotionMessage ReadFrom(BinaryReader br) {
            return new MotionMessage(
                wid: br.ReadByte(),
                isGyro: br.ReadBoolean(),
                milliseconds: br.ReadInt64(),
                x: br.ReadSingle(),
                y: br.ReadSingle(),
                z: br.ReadSingle());
        }

        static public int GetByteSize() {
            return sizeof (byte) + sizeof (bool) + sizeof (long) + sizeof (float) * 3;
        }
    }
}
