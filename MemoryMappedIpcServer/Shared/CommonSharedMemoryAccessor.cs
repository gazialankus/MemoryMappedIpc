using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Winterdom.IO.FileMap;

namespace MemoryMappedIpcServer.Shared {
    class CommonSharedMemoryAccessor {

        private readonly bool _isServer;
        private static string memoryMapName = "wii_header";
        private readonly MemoryMappedFile _memoryMappedFile;

        public bool IsServer() {
            return _isServer;
        }

        // for the client
        public CommonSharedMemoryAccessor() {
            _isServer = false;

            int currentLocation = 0;
            _numberOfClienRequestsAddress = currentLocation; 
            currentLocation = currentLocation + sizeof(int);
            _numberOfAcceptedClientsAddress = currentLocation;
            currentLocation = currentLocation + sizeof(int);
            _lineSizeAddress = currentLocation;
            currentLocation = currentLocation + sizeof(int);
            _totalBufferSizeInLinesAddress = currentLocation;
            currentLocation = currentLocation + sizeof(int);
            int totalDataSize = currentLocation;

            _memoryMappedFile = _isServer
                ? MemoryMappedFile.Create(
                    protection: MapProtection.PageReadWrite,
                    maxSize: totalDataSize,
                    name: memoryMapName)
                : MemoryMappedFile.Open(
                    access: MapAccess.FileMapAllAccess,
                    name: memoryMapName);

            _headerAccessor = _memoryMappedFile.MapView(
                access: MapAccess.FileMapAllAccess,
                offset: 0,
                size: totalDataSize);
            _headerReader = new BinaryReader(_headerAccessor);
            _headerWriter = new BinaryWriter(_headerAccessor);

            _mutex = Mutex.OpenExisting(_mutexName);
        }

        // for the server
        public CommonSharedMemoryAccessor(int lineSize, int totalBufferSizeInLines) {
            _isServer = true;

            int currentLocation = 0;
            _numberOfClienRequestsAddress = currentLocation; 
            currentLocation = currentLocation + sizeof(int);
            _numberOfAcceptedClientsAddress = currentLocation;
            currentLocation = currentLocation + sizeof(int);
            _lineSizeAddress = currentLocation;
            currentLocation = currentLocation + sizeof(int);
            _totalBufferSizeInLinesAddress = currentLocation;
            currentLocation = currentLocation + sizeof(int);
            int totalDataSize = currentLocation;

            _memoryMappedFile = _isServer
                ? MemoryMappedFile.Create(
                    protection: MapProtection.PageReadWrite,
                    maxSize: totalDataSize,
                    name: memoryMapName)
                : MemoryMappedFile.Open(
                    access: MapAccess.FileMapAllAccess,
                    name: memoryMapName);

            _headerAccessor = _memoryMappedFile.MapView(
                access: MapAccess.FileMapAllAccess,
                offset: 0,
                size: totalDataSize);
            _headerReader = new BinaryReader(_headerAccessor);
            _headerWriter = new BinaryWriter(_headerAccessor);

            LineSize = lineSize;
            TotalBufferSizeInLines = totalBufferSizeInLines;

            _mutex = new Mutex(false, _mutexName);
        }

        private string _mutexName = "wii_common";

        private readonly Stream _headerAccessor;
        private readonly BinaryReader _headerReader;
        private readonly BinaryWriter _headerWriter;

        // these are more like IDs. not reduced upon disconnection.
        private readonly int _numberOfClienRequestsAddress;
        private readonly int _numberOfAcceptedClientsAddress;
        private readonly int _lineSizeAddress;
        private readonly int _totalBufferSizeInLinesAddress;

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

        public int NumberOfClientRequests {
            get {
                return ReadIntFromHeader(_numberOfClienRequestsAddress);
            }
            set {
                WriteIntToHeader(_numberOfClienRequestsAddress, value);
            }
        }

        public int NumberOfAcceptedClients {
            get {
                return ReadIntFromHeader(_numberOfAcceptedClientsAddress);
            }
            set {
                WriteIntToHeader(_numberOfAcceptedClientsAddress, value);
            }
        }
        
        public int LineSize {
            get {
                return ReadIntFromHeader(_lineSizeAddress);
            }
            set {
                WriteIntToHeader(_lineSizeAddress, value);
            }
        }

        public int TotalBufferSizeInLines {
            get {
                return ReadIntFromHeader(_totalBufferSizeInLinesAddress);
            }
            set {
                WriteIntToHeader(_totalBufferSizeInLinesAddress, value);
            }
        }

        private Mutex _mutex;

        public void LockMutex() {
            _mutex.WaitOne();
        }

        public void ReleaseMutex() {
            _mutex.ReleaseMutex();
        }

        public int ConnectClientAndWaitForServer() {
            LockMutex();
            int id = NumberOfClientRequests;
            NumberOfClientRequests += 1;
            int cachedNumRequests = NumberOfClientRequests;
            ReleaseMutex();

            while (cachedNumRequests > NumberOfAcceptedClients) {
                Thread.Sleep(1);
            }
            return id;
        }
    }
}
