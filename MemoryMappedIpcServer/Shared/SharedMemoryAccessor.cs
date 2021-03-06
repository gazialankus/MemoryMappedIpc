using System;
using System.Collections.Generic;
using System.IO;
using Winterdom.IO.FileMap;

namespace MemoryMappedIpcServer.Shared {

    // these are used as tokens of negotiation with the game 
    public enum InfoType: byte {
        // what is being placed in the lines in shared memory
        AccelAndGyro = 1, 
        OrientationQuaternion = 2,
    }

    public enum DeviceType: byte {
        // the device type
        Wii = 1,
        Android = 2, 
        Gamepad = 3, 
    }


    public class SharedMemoryAccessor {

        private readonly MemoryMappedFile _memoryMappedFile;
        private readonly int _clientId;
        private readonly bool _isServer;
        // for server
        private readonly BinaryWriter _bufferWriter;
        // for client
        private readonly BinaryReader _bufferReader;

        public SharedMemoryAccessor(string memoryMapName, int clientId, bool isServer, int lineSize, int totalBufferSizeInLines) {
            //var memoryMapName = "wii_" + _clientId; //used to be this way.

            this._clientId = clientId;
            _isServer = isServer;

            int currentLocation;
            _bufferStartingLineAddress = currentLocation = 0;
            currentLocation = currentLocation + sizeof(int);
            _bufferWrittenLineCountAddress = currentLocation;
            currentLocation = currentLocation + sizeof(int);
            _clientHasReadThisManyLinesAddress = currentLocation;
            currentLocation = currentLocation + sizeof(int);
            _clientWantsWiiGyroRecalibrationForBitmaskAddress = currentLocation;
            currentLocation = currentLocation + sizeof(int);
            _clientSuppliedWiiGyroRecalibrationForAddress = currentLocation;
            currentLocation = currentLocation + sizeof(int);

            _wiiGyroCalibrationXAddress = currentLocation;
            currentLocation = currentLocation + sizeof(int);
            _wiiGyroCalibrationYAddress = currentLocation;
            currentLocation = currentLocation + sizeof(int);
            _wiiGyroCalibrationZAddress = currentLocation;
            currentLocation = currentLocation + sizeof(int);

            _clientClosedConnectionAddress = currentLocation;
            currentLocation = currentLocation + sizeof(int);
            _clientPingAddress = currentLocation;
            currentLocation = currentLocation + sizeof(int);

            _clientHasSuppliedDesiredCriteriaAddress = currentLocation;
            currentLocation = currentLocation + sizeof(int);

            _clientDesiredInfoTypeAddress = currentLocation;
            currentLocation = currentLocation + sizeof(int);
            _clientDesiredDeviceTypeAddress = currentLocation;
            currentLocation = currentLocation + sizeof(int);
            _clientDesiredDeviceIdAddress = currentLocation;
            currentLocation = currentLocation + sizeof(int);
            _clientGenericIntMessageAddress = currentLocation;
            currentLocation = currentLocation + sizeof(int);
            

            int headerSize = currentLocation;


            _memoryMappedFile = isServer
                ? MemoryMappedFile.Create(
                    protection: MapProtection.PageReadWrite,
                    maxSize: headerSize + lineSize * totalBufferSizeInLines,
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
                size: headerSize);
            _headerReader = new BinaryReader(_headerAccessor);
            _headerWriter = new BinaryWriter(_headerAccessor);


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

            _bufferOffset = headerSize;

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

            this.LineSize = lineSize;
            this.TotalBufferSizeInLines = totalBufferSizeInLines;


            if (isServer) {
                InitializeShmemValues();
            } else {
                // let the client catch up and ignore previous values.
                ClientHasReadThisManyLines = BufferWrittenLineCount;
            }
        }

        private void InitializeShmemValues() {
            ClientWantsWiiGyroRecalibrationForBitmask = 0;
            ClientSuppliedWiiGyroRecalibrationFor = -1;
            ClientClosedConnection = false;
            ClientHasSuppliedDesiredCriteria = false;
        }

        public int LineSize { get; private set; }
        public int TotalBufferSizeInLines { get; private set; }

        private readonly Stream _mmStream;

        private int ReadIntFromHeader(int address) {
            lock (_headerAccessor) { //since we access this from another thread now. 
                long oldPosition = _headerAccessor.Position;
                _headerAccessor.Seek(address, SeekOrigin.Begin);
                int val = _headerReader.ReadInt32();
                _headerAccessor.Seek(oldPosition, SeekOrigin.Begin); //TODO maybe this is not needed, this may not affect the other stream
                return val;
            }
        }

        private void WriteIntToHeader(int address, int value) {
            lock (_headerAccessor) {
                long oldPosition = _headerAccessor.Position;
                _headerAccessor.Seek(address, SeekOrigin.Begin);
                _headerWriter.Write(value);
                _headerAccessor.Seek(oldPosition, SeekOrigin.Begin);
            }
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

        // Wii-specific
        public int ClientWantsWiiGyroRecalibrationForBitmask {
            get {
                return ReadIntFromHeader(_clientWantsWiiGyroRecalibrationForBitmaskAddress);
            }
            set {
                WriteIntToHeader(_clientWantsWiiGyroRecalibrationForBitmaskAddress, value);
            }
        }

        public int ClientSuppliedWiiGyroRecalibrationFor {
            get {
                return ReadIntFromHeader(_clientSuppliedWiiGyroRecalibrationForAddress);
            }
            set {
                WriteIntToHeader(_clientSuppliedWiiGyroRecalibrationForAddress, value);
            }
        }

        public short[] WiiGyroCalibrationValues {
            get {
                return new short[3] {
                    (short) ReadIntFromHeader(_wiiGyroCalibrationXAddress), 
                    (short) ReadIntFromHeader(_wiiGyroCalibrationYAddress), 
                    (short) ReadIntFromHeader(_wiiGyroCalibrationZAddress),
                };
            }
            set {
                WriteIntToHeader(_wiiGyroCalibrationXAddress, value[0]);
                WriteIntToHeader(_wiiGyroCalibrationYAddress, value[1]);
                WriteIntToHeader(_wiiGyroCalibrationZAddress, value[2]);
            }
        }
        // ^Wii-specific

        public bool ClientClosedConnection {
            get {
                return ReadIntFromHeader(_clientClosedConnectionAddress) != 0;
            }
            set {
                WriteIntToHeader(_clientClosedConnectionAddress, value ? 1 : 0);
            }
        }

        public int ClientPing {
            get {
                return ReadIntFromHeader(_clientPingAddress);
            }
            set {
                WriteIntToHeader(_clientPingAddress, value);
            }
        }

        // client's desired data type info
        public bool ClientHasSuppliedDesiredCriteria {
            get {
                return ReadIntFromHeader(_clientHasSuppliedDesiredCriteriaAddress) != 0;
            }
            set {
                WriteIntToHeader(_clientHasSuppliedDesiredCriteriaAddress, value ? 1: 0);
            }
        }

        public int ClientDesiredInfoType {
            get {
                return ReadIntFromHeader(_clientDesiredInfoTypeAddress);
            }
            set {
                WriteIntToHeader(_clientDesiredInfoTypeAddress, value);
            }
        }

        public int ClientDesiredDeviceType {
            get {
                return ReadIntFromHeader(_clientDesiredDeviceTypeAddress);
            }
            set {
                WriteIntToHeader(_clientDesiredDeviceTypeAddress, value);
            }
        }

        public int ClientDesiredDeviceId {
            get {
                return ReadIntFromHeader(_clientDesiredDeviceIdAddress);
            }
            set {
                WriteIntToHeader(_clientDesiredDeviceIdAddress, value);
            }
        }

        public int ClientGenericIntMessage {
            get {
                return ReadIntFromHeader(_clientGenericIntMessageAddress);
            }
            set {
                WriteIntToHeader(_clientGenericIntMessageAddress, value);
            }
        }



        private readonly Stream _headerAccessor;
        private readonly BinaryReader _headerReader;
        private readonly BinaryWriter _headerWriter;
        private readonly int _bufferStartingLineAddress;
        private readonly int _bufferWrittenLineCountAddress;
        private readonly int _clientHasReadThisManyLinesAddress;
        private readonly int _clientWantsWiiGyroRecalibrationForBitmaskAddress;
        private readonly int _clientSuppliedWiiGyroRecalibrationForAddress;
        private readonly int _wiiGyroCalibrationXAddress; 
        private readonly int _wiiGyroCalibrationYAddress;
        private readonly int _wiiGyroCalibrationZAddress;
        private readonly int _clientClosedConnectionAddress;
        private readonly int _clientPingAddress;
        private readonly int _clientHasSuppliedDesiredCriteriaAddress;
        private readonly int _clientDesiredInfoTypeAddress;
        private readonly int _clientDesiredDeviceTypeAddress;
        private readonly int _clientDesiredDeviceIdAddress;
        private readonly int _clientGenericIntMessageAddress;

        private readonly int _bufferOffset;


        // with the help of this, both writer and reader know where they are. use it to complete the protocol. 
        private long GetLinePos() {
            return (_mmStream.Position - _bufferOffset) / LineSize;
        }

        private void SeekToLine(int where) {
            _mmStream.Seek(_bufferOffset + where * LineSize, SeekOrigin.Begin);
        }

        public void AddLine(AbstractMessage m) {
            // did the client read in the meantime? (the client can read while stuff below are happening. make sure they don't break stuff.)
                // if so, we'll have to rearrange the buffer
                // if not, we'll just write and go on

            PrepareClientForWriting();


            //write
            if (GetLinePos() == TotalBufferSizeInLines) {
                SeekToLine(0);
                //Console.WriteLine("server looped back to start");
            }
            m.WriteTo(_bufferWriter);

            // advance
            ++BufferWrittenLineCount;
        }

        private void PrepareClientForWriting()
        {
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
        }

        public IEnumerable<AbstractMessage> GetAvailableLinesToClient() {
            // if there are available lines, tell the server that you have read them and return them 
            // I should always seek here, because if the client does not actually read them, the stream does not advance. 

            // if there are no new lines, break.
            if (ClientHasReadThisManyLines > 0) {
                // we've already read some the last time. the server did not have a chance to reorganize. pass this time. 
                return BlankEnumerableForClient();
            } else if (BufferWrittenLineCount == 0) {
                //Console.WriteLine("still no new line");
                // the server did not add any new lines, yet.
                return BlankEnumerableForClient();
            } else {
                // report that you have read this many 
                int bufferWrittenLineCountCopy = BufferWrittenLineCount;

                // does it matter when I report this? reporting this causes the server to rearrange stuff. so maybe hold on to it. 

                IEnumerable<AbstractMessage> enumerable = EnumerateForClient(bufferWrittenLineCountCopy);

                // report this late so that the server can start the rearrangements after we have done our reading. 
                // what if we don't do the reading? well then we should do the enumerator in another function.
                // right now, we are reporting before we read. unless the server writes too fast and fills the buffer. 
                ClientHasReadThisManyLines = bufferWrittenLineCountCopy;

                return enumerable;
            }
        }

        public AbstractMessage GetLastAvailableLineToClient() {
            if (ClientHasReadThisManyLines > 0) {
                // we've already read some the last time. the server did not have a chance to reorganize. pass this time. 
                return null;
            } else if (BufferWrittenLineCount == 0) {
                //Console.WriteLine("still no new line");
                // the server did not add any new lines, yet.
                return null;
            } else {

                // report that you have read this many 
                int bufferWrittenLineCountCopy = BufferWrittenLineCount;

                // TODO should every seekto line have a modulo operator?
                SeekToLine((BufferStartingLine + bufferWrittenLineCountCopy - 1) % TotalBufferSizeInLines);

                ClientHasReadThisManyLines = bufferWrittenLineCountCopy;

                return AbstractMessage.ReadFrom(_bufferReader);
            }
        }

        private IEnumerable<AbstractMessage> BlankEnumerableForClient() {
            yield break;
        }

        private IEnumerable<AbstractMessage> EnumerateForClient(int bufferWrittenLineCountCopy) {
            // read them, starting from BufferStartingLine, and as many as BufferWrittenLineCount
            // need to seek to BufferStartingLine
            SeekToLine(BufferStartingLine);

            for (int i = 0; i < bufferWrittenLineCountCopy; ++i) {
                // keep reading. when we hit the end, rewind to the beginning.
                if (GetLinePos() == TotalBufferSizeInLines) {
                    SeekToLine(0);
                }

                yield return AbstractMessage.ReadFrom(_bufferReader);
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
