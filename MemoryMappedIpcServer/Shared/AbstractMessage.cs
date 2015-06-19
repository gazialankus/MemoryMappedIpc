using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MemoryMappedIpcServer.Shared
{
    public enum MessageType: byte {
        ButtonMessage = 1, 
        GyroMessage = 2, 
        AccelMessage = 3, 
        GyroCalibrationMessage = 4, 
    }

    public class AbstractMessage {
        public MessageType MessageType;
        public long Milliseconds;
        public byte Wid;

        public AbstractMessage(MessageType messageType, long milliseconds, byte wid) {
            MessageType = messageType;
            Milliseconds = milliseconds;
            Wid = wid;
        }

        public AbstractMessage(MessageType messageType, BinaryReader br, ref int size) {
            //the message type would already be read to make the type decision
            //MessageType = (MessageType)br.ReadByte();
            MessageType = messageType;
            Milliseconds = ReadLongAndAccumulateSize(br, ref size);
            Wid = ReadByteAndAccumulateSize(br, ref size);
        }

        public virtual int WriteTo(BinaryWriter bw) {
            int size = 0;
            WriteAndAccumulateSize(bw, (byte)MessageType, ref size);
            WriteAndAccumulateSize(bw, Milliseconds, ref size);
            WriteAndAccumulateSize(bw, Wid, ref size);
            return size;
            // not flushing since the actual message will follow
        }

        protected void WriteAndAccumulateSize(BinaryWriter bw, byte val, ref int currentSum) {
            bw.Write(val);
            currentSum += sizeof(byte);
        }

        protected void WriteAndAccumulateSize(BinaryWriter bw, short val, ref int currentSum) {
            bw.Write(val);
            currentSum += sizeof(short);
        }

        protected void WriteAndAccumulateSize(BinaryWriter bw, int val, ref int currentSum) {
            bw.Write(val);
            currentSum += sizeof(int);
        }

        protected void WriteAndAccumulateSize(BinaryWriter bw, long val, ref int currentSum) {
            bw.Write(val);
            currentSum += sizeof(long);
        }

        protected void WriteAndAccumulateSize(BinaryWriter bw, float val, ref int currentSum) {
            bw.Write(val);
            currentSum += sizeof(float);
        }

        protected byte ReadByteAndAccumulateSize(BinaryReader bw, ref int currentSum) {
            currentSum += sizeof(byte);
            return bw.ReadByte();
        }

        protected short ReadShortAndAccumulateSize(BinaryReader bw, ref int currentSum) {
            currentSum += sizeof(short);
            return bw.ReadInt16();
        }

        protected int ReadIntAndAccumulateSize(BinaryReader bw, ref int currentSum) {
            currentSum += sizeof(int);
            return bw.ReadInt32();
        }

        protected long ReadLongAndAccumulateSize(BinaryReader bw, ref int currentSum) {
            currentSum += sizeof(long);
            return bw.ReadInt64();
        }

        protected float ReadFloatAndAccumulateSize(BinaryReader bw, ref int currentSum) {
            currentSum += sizeof(float);
            return bw.ReadSingle();
        }

        //static public int GetByteSize() {
        //    return sizeof (byte) + sizeof (long) + sizeof (byte);
        //}

        //static public int GetFixedByteSize() {
        //    return Math.Max(MotionMessage.GetByteSize(), ButtonMessage.GetByteSize());
        //}

        public static AbstractMessage ReadFrom(BinaryReader br) {
            MessageType messageType = (MessageType)br.ReadByte();
            int size = 1;
            switch (messageType) {
                case MessageType.ButtonMessage:
                    return new ButtonMessage(br, ref size);
                case MessageType.GyroMessage:
                    return new MotionMessage(MessageType.GyroMessage, br, ref size);
                case MessageType.AccelMessage:
                    return new MotionMessage(MessageType.AccelMessage, br, ref size);
                case MessageType.GyroCalibrationMessage:
                    return new GyroCalibrationMessage(MessageType.GyroCalibrationMessage, br, ref size);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // header: byte, long, byte
        public static int ContentSize = sizeof(byte) + sizeof(long) + sizeof(byte);
        // motion message: float, float, float
        public static int LineSize = ContentSize + 3 * sizeof(float);

        protected void PadMessageEnd(BinaryWriter bw, ref int writtenBytes) {
            int numPaddingBytes = LineSize - writtenBytes;
            if (numPaddingBytes > 0) {
                Console.WriteLine("will seek forwards " + numPaddingBytes + " bytes");
                bw.BaseStream.Seek(numPaddingBytes, SeekOrigin.Current);
            }
        }

        protected void PadMessageEnd(BinaryReader br, ref int readBytes) {
            int numPaddingBytes = LineSize - readBytes;
            if (numPaddingBytes > 0) {
                Console.WriteLine("write seek " + numPaddingBytes + " bytes");
                br.BaseStream.Seek(numPaddingBytes, SeekOrigin.Current);
            }
        }

    }



}
