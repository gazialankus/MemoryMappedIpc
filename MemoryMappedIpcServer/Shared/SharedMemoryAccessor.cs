using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryMappedIpcServer.Shared {
    class SharedMemoryAccessor {
        private readonly MemoryMappedFile _memoryMappedFile;
        private readonly string _clientId;
        private readonly bool _isServer;
        // for server
        private BinaryWriter _bufferWriter;
        // for client
        private BinaryReader _bufferReader;

        public SharedMemoryAccessor(String clientId, bool isServer, int lineSize, int totalBufferSizeInLines) {
            this.LineSize = lineSize;
            this.TotalBufferSizeInLines = totalBufferSizeInLines;
            this._clientId = clientId;
            _isServer = isServer;

            _memoryMappedFile = isServer ? MemoryMappedFile.CreateNew("wii_" + _clientId, CalculateSharedMemorySizeInBytes(lineSize, totalBufferSizeInLines)) : MemoryMappedFile.OpenExisting("wii_" + _clientId);

            _bufferStartingLineAccessor = _memoryMappedFile.CreateViewAccessor(
                offset: 2 * sizeof(int),
                size: sizeof(int),
                access: isServer ? MemoryMappedFileAccess.Write : MemoryMappedFileAccess.Read);
            _bufferWrittenLineCountAccessor = _memoryMappedFile.CreateViewAccessor(
                offset: 3 * sizeof(int),
                size: sizeof(int),
                access: isServer ? MemoryMappedFileAccess.Write : MemoryMappedFileAccess.Read);
            _clientHasReadThisManyLinesAccessor = _memoryMappedFile.CreateViewAccessor(
                offset: 4 * sizeof(int),
                size: sizeof(int),
                access: MemoryMappedFileAccess.ReadWrite); // both read and write this

            int bufferOffset = CalculateHeaderSizeInBytes();

            MemoryMappedViewStream mmStream = _memoryMappedFile.CreateViewStream(
                offset: bufferOffset,
                size: totalBufferSizeInLines * lineSize,
                access: isServer ? MemoryMappedFileAccess.Write : MemoryMappedFileAccess.Read);

            if (isServer) {
                _bufferWriter = new BinaryWriter(mmStream);
            } else {
                _bufferReader = new BinaryReader(mmStream);
            }
        }

        public int LineSize { get; private set; }
        public int TotalBufferSizeInLines { get; private set; }

        private int _serverBufferStartingLine;
        public int BufferStartingLine {
            get {
                if (_isServer) {
                    return _serverBufferStartingLine;
                } else {
                    return _bufferStartingLineAccessor.ReadInt32(0);
                }
            }
            set {
                if (_isServer) {
                    _serverBufferStartingLine = value;
                }
                _bufferStartingLineAccessor.Write(0, value);
            }
        }
        private readonly MemoryMappedViewAccessor _bufferStartingLineAccessor;

        private int _serverBufferWrittenLineCount;
        public int BufferWrittenLineCount {
            get {
                if (_isServer) {
                    return _serverBufferWrittenLineCount;
                } else {
                    return _bufferWrittenLineCountAccessor.ReadInt32(0);
                }
            }
            set {
                if (_isServer) {
                    _serverBufferWrittenLineCount = value;
                }
                _bufferWrittenLineCountAccessor.Write(0, value);
            }
        }
        private readonly MemoryMappedViewAccessor _bufferWrittenLineCountAccessor;

        public int ClientHasReadThisManyLines {
            get { return _clientHasReadThisManyLinesAccessor.ReadInt32(0); }
            set { _clientHasReadThisManyLinesAccessor.Write(0, value); }
        }
        private readonly MemoryMappedViewAccessor _clientHasReadThisManyLinesAccessor;

        static private long CalculateSharedMemorySizeInBytes(int lineSize, int totalBufferSizeInLines) {
            return CalculateHeaderSizeInBytes() 
                + lineSize * totalBufferSizeInLines;
        }

        private static int CalculateHeaderSizeInBytes() {
            return sizeof (int) // _bufferStartingLine
                + sizeof (int) // _bufferWrittenLineCount
                + sizeof (int) // _clientHasReadThisManyLines
                + sizeof (int) // _totalBufferSizeInLines
                + sizeof (int); // _lineSize;
        }

        // with the help of this, both writer and reader know where they are. use it to complete the protocol. 
        private long GetLinePos(Stream baseStream) {
            return baseStream.Position / LineSize;
        }

        private void Seek(Stream baseStream, int where) {
            baseStream.Seek(where * LineSize, SeekOrigin.Begin);
        }

        public void AddLine(int i) {
            // did the client read in the meantime? (the client can read while stuff below are happening. make sure they don't break stuff.)
                // if so, we'll have to rearrange the buffer
                // if not, we'll just write and go on

            // did the client read in the meantime? (the client can read while stuff below are happening. make sure they don't break stuff.)
            int clientHasReadThisManyLinesCopy = ClientHasReadThisManyLines; // copy for efficiency
            if (clientHasReadThisManyLinesCopy > 0) {
                // if so, we'll have to rearrange the buffer
                // I can do whatever I want till I set ClientHasReadThisManyLines to 0. I know the client won't touch this stuff till then. 

                // BufferWrittenLineCount is the ones that the client did not read. it should be decremented.
                BufferWrittenLineCount -= clientHasReadThisManyLinesCopy;
                // BufferStartingLine needs to be advanced
                BufferStartingLine = (BufferStartingLine + clientHasReadThisManyLinesCopy) % TotalBufferSizeInLines;

                // now I can tell the client that it's safe to read again. 
                ClientHasReadThisManyLines = 0;
            } 
            // either case, we'll just write and go on
            // first write, then advance. so that the client does not read unwritten buffer in the meantime. 

            //write
            if (GetLinePos(_bufferWriter.BaseStream) == TotalBufferSizeInLines) {
                Seek(_bufferWriter.BaseStream, 0);
                Console.WriteLine("server looped back to start");
            }
            _bufferWriter.Write(i);

            // advance
            ++BufferWrittenLineCount;

            //// this could have been calculated with the line position. TODO see if you can do that afterwards. leave all state to streams. 

            //if (GetLinePos(_bufferWriter.BaseStream) > 98) {
            //    Console.WriteLine("Pos: " + _bufferWriter.BaseStream.Position + " " + _bufferWriter.BaseStream.Position / LineSize + " " + BufferWrittenLineCount);
            //}

            //// do the appropriate seeking if necessary to wrap around
            //if ((BufferWrittenLineCount + BufferStartingLine) % TotalBufferSizeInLines == 0) {
            //    _bufferWriter.Seek(0, SeekOrigin.Begin);
            //}
        }

        // TODO this will not exist. 
        // instead, the client will get the whole thing at once, from the marked start with the given count. 
        // actually, the client does not have to seek everytime, it seems. just seek when you wrap around. (which you should keep track of TODO)
        // BufferWrittenLineCount tells you how many reads you should do. 
        // should work! 
        //public int ReadLine() {
        //    Console.WriteLine("pos was: " + _bufferReader.BaseStream.Position + " " + GetLinePos(_bufferReader.BaseStream));
        //    return _bufferReader.ReadInt32();
        //}

        public IEnumerable<int> GetAvailableLinesToClient() {
            // if there are available lines, tell the server that you have read them and return them 
            // I should always seek here, because if the client does not actually read them, the stream does not advance. 

            // if there are no new lines, break.
            if (ClientHasReadThisManyLines > 0) {
                // we've already read some the last time. the server did not have a chance to reorganize. pass this time. 
                return BlankEnumerableForClient();
            } else if (BufferWrittenLineCount == 0) {
                Console.WriteLine("still no new line");
                // the server did not add any new lines, yet.
                return BlankEnumerableForClient();
            } else {
                // report that you have read this many 
                int bufferWrittenLineCountCopy = BufferWrittenLineCount;

                // does it matter when I report this? reporting this causes the server to rearrange stuff. so maybe hold on to it. 

                IEnumerable<int> enumerable = EnumerateForClient(bufferWrittenLineCountCopy, _bufferReader);

                // report this late so that the server can start the rearrangements after we have done our reading. 
                // what if we don't do the reading? well then we should do the enumerator in another function.
                // right now, we are reporting before we read. unless the server writes too fast and fills the buffer. 
                ClientHasReadThisManyLines = bufferWrittenLineCountCopy;

                return enumerable;
            }
        }

        private IEnumerable<int> BlankEnumerableForClient() {
            yield break;
        }

        private IEnumerable<int> EnumerateForClient(int bufferWrittenLineCountCopy, BinaryReader bufferReader) {
            // read them, starting from BufferStartingLine, and as many as BufferWrittenLineCount
            // need to seek to BufferStartingLine
            Seek(bufferReader.BaseStream, BufferStartingLine);

            for (int i = 0; i < bufferWrittenLineCountCopy; ++i) {
                // keep reading. when we hit the end, rewind to the beginning.
                if (GetLinePos(_bufferReader.BaseStream) == TotalBufferSizeInLines) {
                    Seek(bufferReader.BaseStream, 0);
                }

                yield return _bufferReader.ReadInt32();
            }
            yield break;
            //int endLine = BufferStartingLine + BufferWrittenLineCount;
        }

        // an iterable?
    }
}
