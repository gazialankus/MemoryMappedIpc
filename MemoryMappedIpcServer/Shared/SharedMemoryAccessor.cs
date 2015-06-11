using System;
using System.Collections.Generic;
using System.IO;
using Winterdom.IO.FileMap;
//using System.IO.MemoryMappedFiles;

namespace MemoryMappedIpcServer.Shared {
    class SharedMemoryAccessor {

        public static string PipeName = "\\\\.\\pipe\\wii_welcomer_pipe";

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

            var memoryMapName = "wii_" + _clientId;

            _memoryMappedFile = isServer
                ? MemoryMappedFile.Create(
                    protection: MapProtection.PageReadWrite,
                    maxSize: CalculateSharedMemorySizeInBytes(lineSize, totalBufferSizeInLines),
                    name: memoryMapName)
                : MemoryMappedFile.Open(
                    access: MapAccess.FileMapAllAccess,
                    name: memoryMapName);

            //_memoryMappedFile = isServer ? 
            //    MemoryMappedFile.CreateNew(
            //        mapName: memoryMapName, 
            //        capacity: CalculateSharedMemorySizeInBytes(lineSize, totalBufferSizeInLines)) 
            //        : 
            //        MemoryMappedFile.OpenExisting(mapName: "wii_" + _clientId);

            _headerAccessor = _memoryMappedFile.MapView(
                access: MapAccess.FileMapAllAccess,
                offset: 0,
                size: CalculateHeaderSizeInBytes());
            _headerReader = new BinaryReader(_headerAccessor);
            _headerWriter = new BinaryWriter(_headerAccessor);

            int currentLocation;
            _bufferStartingLineAddress = currentLocation = 0;
            _bufferWrittenLineCountAddress = currentLocation = currentLocation + sizeof(int);
            _clientHasReadThisManyLinesAddress = currentLocation = currentLocation + sizeof(int);

            //TODO these accessors are streams. should prevent advancing them when I read and write. 
            //_bufferStartingLineAccessor = _memoryMappedFile.MapView(
            //    access: isServer ? MapAccess.FileMapWrite : MapAccess.FileMapRead,
            //    offset: 2 * sizeof(int), //zero, actually. but was lazy to change the others. 
            //    size: sizeof(int));
            //if (isServer) {
            //    _bufferStartingLineWriter = new BinaryWriter(_bufferStartingLineAccessor);
            //} else {
            //    _bufferStartingLineReader = new BinaryReader(_bufferStartingLineAccessor);
            //}
            //_bufferStartingLineAccessor = _memoryMappedFile.CreateViewAccessor(
            //    offset: 2 * sizeof(int),
            //    size: sizeof(int),
            //    access: isServer ? MemoryMappedFileAccess.Write : MemoryMappedFileAccess.Read);

            //_bufferWrittenLineCountAccessor = _memoryMappedFile.MapView(
            //    access: isServer ? MapAccess.FileMapWrite : MapAccess.FileMapRead,
            //    offset: 3 * sizeof(int),
            //    size: sizeof(int));
            //if (isServer) {
            //    _bufferWrittenLineCountWriter = new BinaryWriter(_bufferWrittenLineCountAccessor);
            //} else {
            //    _bufferWrittenLineCountReader = new BinaryReader(_bufferWrittenLineCountAccessor);
            //}
            //_bufferWrittenLineCountAccessor = _memoryMappedFile.CreateViewAccessor(
            //    offset: 3 * sizeof(int),
            //    size: sizeof(int),
            //    access: isServer ? MemoryMappedFileAccess.Write : MemoryMappedFileAccess.Read);

            //_clientHasReadThisManyLinesAccessor = _memoryMappedFile.MapView(
            //    access: MapAccess.FileMapAllAccess,
            //    offset: 4 * sizeof(int),
            //    size: sizeof(int));
            //_clientHasReadThisManyLinesReader = new BinaryReader(_clientHasReadThisManyLinesAccessor);
            //_clientHasReadThisManyLinesWriter = new BinaryWriter(_clientHasReadThisManyLinesAccessor);
            //_clientHasReadThisManyLinesAccessor = _memoryMappedFile.CreateViewAccessor(
            //    offset: 4 * sizeof(int),
            //    size: sizeof(int),
            //    access: MemoryMappedFileAccess.ReadWrite); // both read and write this

            _bufferOffset = CalculateHeaderSizeInBytes();

            _mmStream = _memoryMappedFile.MapView(
                access: isServer ? MapAccess.FileMapWrite : MapAccess.FileMapRead,
                offset: 0,
                size: totalBufferSizeInLines * lineSize + _bufferOffset);

            //MemoryMappedViewStream mmStream = _memoryMappedFile.CreateViewStream(
            //    offset: bufferOffset,
            //    size: totalBufferSizeInLines * lineSize,
            //    access: isServer ? MemoryMappedFileAccess.Write : MemoryMappedFileAccess.Read);

            if (isServer) {
                _bufferWriter = new BinaryWriter(_mmStream);
            } else {
                _bufferReader = new BinaryReader(_mmStream);
            }
            SeekToLine(0);
        }

        private readonly Stream _mmStream;

        public int LineSize { get; private set; }
        public int TotalBufferSizeInLines { get; private set; }

        private int ReadIntFromHeader(int address) {
            long oldPosition = _headerAccessor.Position;
            _headerAccessor.Seek(address, SeekOrigin.Begin);
            int val = _headerReader.ReadInt32();
            _headerAccessor.Seek(oldPosition, SeekOrigin.Begin); //TODO maybe this is not needed, this may not affect the other stream
            return val;
        }

        private void WriteIntToHeader(int address, int value) {
            long oldPosition = _headerAccessor.Position;
            _headerAccessor.Seek(address, SeekOrigin.Begin);
            _headerWriter.Write(value);
            _headerAccessor.Seek(oldPosition, SeekOrigin.Begin);
        }

        private int _serverBufferStartingLine;
        public int BufferStartingLine {
            get {
                if (_isServer) {
                    return _serverBufferStartingLine;
                } else {
                    //_bufferStartingLineAccessor.Read()
                    //int readInt32 = _bufferStartingLineReader.ReadInt32();
                    //_bufferStartingLineReader.BaseStream.Seek(0, SeekOrigin.Begin);
                    //return readInt32;
                    return ReadIntFromHeader(_bufferStartingLineAddress);
                }
            }
            set {
                if (_isServer) {
                    _serverBufferStartingLine = value;
                }
                //_bufferStartingLineWriter.Write(value);
                //_bufferStartingLineWriter.BaseStream.Seek(0, SeekOrigin.Begin);
                WriteIntToHeader(_bufferStartingLineAddress, value);
            }
        }

        private int _serverBufferWrittenLineCount;
        public int BufferWrittenLineCount {
            get {
                if (_isServer) {
                    return _serverBufferWrittenLineCount;
                } else {
                    //var readInt32 = _bufferWrittenLineCountReader.ReadInt32();
                    //_bufferWrittenLineCountReader.BaseStream.Seek(0, SeekOrigin.Begin);
                    //return readInt32;
                    return ReadIntFromHeader(_bufferWrittenLineCountAddress);
                }
            }
            set {
                if (_isServer) {
                    _serverBufferWrittenLineCount = value;
                }
                //_bufferWrittenLineCountWriter.Write(value);
                //_bufferWrittenLineCountWriter.BaseStream.Seek(0, SeekOrigin.Begin);
                WriteIntToHeader(_bufferWrittenLineCountAddress, value);
            }
        }

        public int ClientHasReadThisManyLines {
            get {
                //int readInt32 = _clientHasReadThisManyLinesReader.ReadInt32();
                //_clientHasReadThisManyLinesReader.BaseStream.Seek(0, SeekOrigin.Begin);
                //return readInt32;
                return ReadIntFromHeader(_clientHasReadThisManyLinesAddress);
            }
            set {
                //_clientHasReadThisManyLinesWriter.Write(value);
                //_clientHasReadThisManyLinesWriter.BaseStream.Seek(0, SeekOrigin.Begin);
                WriteIntToHeader(_clientHasReadThisManyLinesAddress, value);
            }
        }

        private readonly Stream _headerAccessor;
        private readonly BinaryReader _headerReader;
        private readonly BinaryWriter _headerWriter;
        private readonly int _bufferStartingLineAddress;
        private readonly int _bufferWrittenLineCountAddress;
        private readonly int _clientHasReadThisManyLinesAddress;
        private readonly int _bufferOffset;

        static private long CalculateSharedMemorySizeInBytes(int lineSize, int totalBufferSizeInLines) {
            return CalculateHeaderSizeInBytes() 
                + lineSize * totalBufferSizeInLines;
        }

        private static int CalculateHeaderSizeInBytes() {
            return sizeof (int) // _bufferStartingLine
                + sizeof (int) // _bufferWrittenLineCount
                + sizeof (int); // _clientHasReadThisManyLines
                //+ sizeof (int) // _totalBufferSizeInLines
                //+ sizeof (int); // _lineSize;
        }

        // with the help of this, both writer and reader know where they are. use it to complete the protocol. 
        private long GetLinePos() {
            return (_mmStream.Position - _bufferOffset) / LineSize;
        }

        private void SeekToLine(int where) {
            _mmStream.Seek(_bufferOffset + where * LineSize, SeekOrigin.Begin);
        }

        public void AddLine(MotionMessage m) {
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
            if (GetLinePos() == TotalBufferSizeInLines) {
                SeekToLine(0);
                Console.WriteLine("server looped back to start");
            }
            MotionMessage.WriteTo(_bufferWriter, m);

            // advance
            ++BufferWrittenLineCount;
        }

        public IEnumerable<MotionMessage> GetAvailableLinesToClient() {
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

                IEnumerable<MotionMessage> enumerable = EnumerateForClient(bufferWrittenLineCountCopy);

                // report this late so that the server can start the rearrangements after we have done our reading. 
                // what if we don't do the reading? well then we should do the enumerator in another function.
                // right now, we are reporting before we read. unless the server writes too fast and fills the buffer. 
                ClientHasReadThisManyLines = bufferWrittenLineCountCopy;

                return enumerable;
            }
        }

        private IEnumerable<MotionMessage> BlankEnumerableForClient() {
            yield break;
        }

        private IEnumerable<MotionMessage> EnumerateForClient(int bufferWrittenLineCountCopy) {
            // read them, starting from BufferStartingLine, and as many as BufferWrittenLineCount
            // need to seek to BufferStartingLine
            SeekToLine(BufferStartingLine);

            for (int i = 0; i < bufferWrittenLineCountCopy; ++i) {
                // keep reading. when we hit the end, rewind to the beginning.
                if (GetLinePos() == TotalBufferSizeInLines) {
                    SeekToLine(0);
                }

                yield return MotionMessage.ReadFrom(_bufferReader);
            }
            yield break;
            //int endLine = BufferStartingLine + BufferWrittenLineCount;
        }

        public void CleanUp() {
            _memoryMappedFile.Dispose();
            // do I need to dispose of anything else? I think the other streams etc are dependent on the memory mapped file, maybe not necessary. 
        }
    }
}
