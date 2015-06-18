using System.IO;

namespace MemoryMappedIpcServer.Shared
{
    public class ButtonMessage : AbstractMessage {
        
        public short Pressed;
        public short Held;
        public short Released;

        public ButtonMessage(MessageType messageType, long milliseconds, byte wid, short pressed, short held, short released) 
            : base(messageType, milliseconds, wid) {
            Pressed = pressed;
            Held = held;
            Released = released;
        }

        public ButtonMessage(BinaryReader br, ref int size) : base(MessageType.ButtonMessage, br, ref size) {
            Pressed = ReadShortAndAccumulateSize(br, ref size);
            Held = ReadShortAndAccumulateSize(br, ref size);
            Released = ReadShortAndAccumulateSize(br, ref size);
            PadMessageEnd(br, ref size);
        }

        public override int WriteTo(BinaryWriter bw) {
            int size = base.WriteTo(bw);
            WriteAndAccumulateSize(bw, Pressed, ref size);
            WriteAndAccumulateSize(bw, Held, ref size);
            WriteAndAccumulateSize(bw, Released, ref size);
            PadMessageEnd(bw, ref size);
            bw.Flush();
            return size;
        }

        //static public int GetByteSize() {
        //    return sizeof (short) * 3;
        //}
    }
}
