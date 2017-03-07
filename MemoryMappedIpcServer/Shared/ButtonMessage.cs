using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MemoryMappedIpcServer.Shared
{
    public enum WiiButton : short {
        Two	= 0x0001, 
        One	= 0x0002,
        B = 0x0004,
        A = 0x0008,
        Minus = 0x0010,
        Home = 0x0080,
        Left = 0x0100,
        Right = 0x0200,
        Down = 0x0400,
        Up = 0x0800,
        Plus = 0x1000,
    }

    public class ButtonMessage : AbstractMessage {

        public short Pressed;
        public short Held;
        public short Released;

        public static IEnumerable<WiiButton> GetButtonListIn(short buttons) {
            return Enum.GetValues(typeof(WiiButton)).Cast<WiiButton>()
                .Where(wiiButton => IsButtonIn(wiiButton, buttons));
        }

        public static bool IsButtonIn(WiiButton button, short buttons) {
            return (buttons & (short)button) != 0;
        }

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
